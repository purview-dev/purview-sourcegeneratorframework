using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Logging;
using Purview.SourceGeneratorFramework.Testing.Models;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// The result of a source generator test run.
/// </summary>
public sealed record class DriverRunResult(
	GeneratorDriverRunResult DriverResult,
	CompilationRunResult CompilationResult,
	ImmutableArray<SyntaxTree> AllSyntaxTrees,
	ImmutableArray<SyntaxTree> PrimarySyntaxTrees,
	ImmutableArray<LogEntry> LogEntries
)
{
	/// <summary>
	/// Throws <see cref="DriverRunValidationException"/> containing all generation exceptions,
	/// compilation errors, emit errors, and generator log errors found in the run.
	/// </summary>
	public void EnsureValid()
	{
		var generationExceptions = DriverResult
			.Results.Where(result => result.Exception is not null)
			.Select(result => new GeneratorFailure(
				result.Generator.GetType().FullName ?? result.Generator.GetType().Name,
				result.Exception!
			))
			.ToList();

		var compilationErrors = CompilationResult
			.Compilation.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		var logErrors = LogEntries.Where(e => e.Type == SourceGenLogLevel.Fatal).ToList();
		var compilationErrorKeys = compilationErrors.Select(GetDiagnosticKey).ToHashSet();
		var emitErrors = CompilationResult
			.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.Where(diagnostic => !compilationErrorKeys.Contains(GetDiagnosticKey(diagnostic)))
			.ToList();

		if (
			generationExceptions.Count == 0
			&& compilationErrors.Count == 0
			&& emitErrors.Count == 0
			&& logErrors.Count == 0
		)
			return;

		throw new DriverRunValidationException(
			this,
			generationExceptions,
			compilationErrors,
			emitErrors,
			logErrors,
			AllSyntaxTrees,
			CompilationResult.Compilation.SyntaxTrees
		);
	}

	static string GetDiagnosticKey(Diagnostic diagnostic) =>
		$"{diagnostic.Id}|{diagnostic.Location.SourceTree?.FilePath}|{diagnostic.Location.SourceSpan.Start}|{diagnostic.Location.SourceSpan.Length}|{diagnostic.GetMessage(CultureInfo.InvariantCulture)}";

	/// <summary>
	/// Gets the source text of a generated tree with the specified hint name.
	/// </summary>
	/// <param name="hintName">The hint name of the generated tree - this can be the whole of end of the hint name.</param>
	/// <param name="matchMode">The mode to use when matching the hint name.</param>
	/// <returns>The source text of the generated tree, or <see langword="null"/> if not found.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="hintName"/> is <see langword="null"/> or whitespace.</exception>
	/// <remarks>The <paramref name="hintName"/> is matched using <see cref="StringComparison.Ordinal"/>.</remarks>
	public string? GetSource(string hintName, HintNameMatchMode matchMode = HintNameMatchMode.Suffix)
	{
		if (string.IsNullOrWhiteSpace(hintName))
			throw new ArgumentException("Value cannot be null or whitespace.", nameof(hintName));

		var hasExtension = hintName.EndsWith(".cs", StringComparison.Ordinal);
		var predicate = matchMode switch
		{
			HintNameMatchMode.Suffix => new Func<string, bool>(s =>
			{
				if (!hasExtension)
				{
					var postSuffix = ".cs";
					if (
						s.EndsWith(hintName + postSuffix, StringComparison.Ordinal)
						|| s.EndsWith(hintName + ".g" + postSuffix, StringComparison.Ordinal)
					)
						return true;
				}

				return s.EndsWith(hintName, StringComparison.Ordinal);
			}),
			HintNameMatchMode.Partial => new Func<string, bool>(s => s.Contains(hintName, StringComparison.Ordinal)),
			HintNameMatchMode.Exact => new Func<string, bool>(s => s.Equals(hintName, StringComparison.Ordinal)),
			_ => throw new ArgumentOutOfRangeException(nameof(matchMode), matchMode, "Invalid match mode."),
		};

		// Find the generated source with the specified hint name
		return DriverResult
			.Results.SelectMany(static r => r.GeneratedSources)
			.Where(s => predicate(s.HintName))
			.Select(static s => s.SourceText.ToString())
			.SingleOrDefault();
	}

	/// <summary>
	/// Gets the source text of the first primary generated tree.
	/// </summary>
	public string GetSource()
	{
		var tree = PrimarySyntaxTrees.FirstOrDefault();
		return tree?.GetText().ToString() ?? string.Empty;
	}

	/// <summary>
	/// Finds a generated tree whose file path ends with the specified suffix.
	/// </summary>
	/// <param name="filePathSuffix">The suffix to match.</param>
	/// <returns>The matching syntax tree, or <see langword="null"/> if none is found.</returns>
	public SyntaxTree? GetGeneratedTree(string filePathSuffix) =>
		AllSyntaxTrees.FirstOrDefault(tree => tree.FilePath.EndsWith(filePathSuffix, StringComparison.Ordinal));
}

/// <summary>
/// The result of a compilation run, including the compilation, the resulting assembly (if successful), and any diagnostics produced during compilation.
/// </summary>
/// <param name="Compilation">The compilation that was run.</param>
/// <param name="Assembly">The resulting assembly, or <see langword="null"/> if the compilation failed.</param>
/// <param name="Diagnostics">The diagnostics produced during the compilation.</param>
public sealed record class CompilationRunResult(
	Compilation Compilation,
	Assembly? Assembly,
	ImmutableArray<Diagnostic> Diagnostics
);

/// <summary>
/// Specifies how to match hint names when retrieving generated source code from a <see cref="DriverRunResult"/>.
/// </summary>
public enum HintNameMatchMode
{
	/// <summary>
	/// Match the hint name by suffix.
	/// </summary>
	/// <remarks>Note this will automatically check for <c>.cs</c> if it's excluded.</remarks>
	Suffix,

	/// <summary>
	/// Match the hint name by partial match.
	/// </summary>
	Partial,

	/// <summary>
	/// Match the hint name exactly.
	/// </summary>
	Exact,
}
