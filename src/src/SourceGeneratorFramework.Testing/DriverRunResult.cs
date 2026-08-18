using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Logging;
using Purview.SourceGeneratorFramework.Testing.Models;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// The result of a source generator test run.
/// </summary>
public record class DriverRunResult(
	GeneratorDriverRunResult Result,
	Compilation OutputCompilation,
	Assembly? Assembly,
	IEnumerable<Diagnostic> CompilationDiagnostics,
	IEnumerable<SyntaxTree> GeneratedTrees,
	IEnumerable<SyntaxTree> NonAttributeSyntaxTrees,
	IReadOnlyList<LogEntry> LogEntries
)
{
	/// <summary>
	/// Gets the generated syntax trees excluding configured attribute files.
	/// </summary>
	public IEnumerable<SyntaxTree> SyntaxTrees => NonAttributeSyntaxTrees;

	/// <summary>
	/// Throws <see cref="DriverRunValidationException"/> containing all generation exceptions,
	/// compilation errors, emit errors, and generator log errors found in the run.
	/// </summary>
	public void EnsureValid()
	{
		var generationExceptions = Result
			.Results.Where(result => result.Exception is not null)
			.Select(result => new GeneratorFailure(
				result.Generator.GetType().FullName ?? result.Generator.GetType().Name,
				result.Exception!
			))
			.ToList();

		var compilationErrors = OutputCompilation
			.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		var logErrors = LogEntries.Where(e => e.Type == SourceGenLogLevel.Fatal).ToList();
		var compilationErrorKeys = compilationErrors.Select(GetDiagnosticKey).ToHashSet();
		var emitErrors = CompilationDiagnostics
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
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
			generationExceptions,
			compilationErrors,
			emitErrors,
			logErrors,
			GeneratedTrees,
			OutputCompilation.SyntaxTrees
		);
	}

	static string GetDiagnosticKey(Diagnostic diagnostic) =>
		$"{diagnostic.Id}|{diagnostic.Location.SourceTree?.FilePath}|{diagnostic.Location.SourceSpan.Start}|{diagnostic.Location.SourceSpan.Length}|{diagnostic.GetMessage(CultureInfo.InvariantCulture)}";

	/// <summary>
	/// Gets the source text of a generated tree with the specified hint name.
	/// </summary>
	/// <param name="hintName">The hint name of the generated tree - this can be the whole of end of the hint name.</param>
	/// <returns>The source text of the generated tree, or <see langword="null"/> if not found.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="hintName"/> is <see langword="null"/> or whitespace.</exception>
	/// <remarks>The <paramref name="hintName"/> is matched using <see cref="StringComparison.Ordinal"/>.</remarks>
	public string? GetSource(string hintName)
	{
		if (string.IsNullOrWhiteSpace(hintName))
			throw new ArgumentException("Value cannot be null or whitespace.", nameof(hintName));

		// Find the generated source with the specified hint name
		return Result
			.Results.SelectMany(static r => r.GeneratedSources)
			.Where(s => s.HintName.EndsWith(hintName, StringComparison.Ordinal))
			.Select(static s => s.SourceText.ToString())
			.SingleOrDefault();
	}

	/// <summary>
	/// Gets the source text of the first non-attribute generated tree.
	/// </summary>
	public string GetSource()
	{
		var tree = NonAttributeSyntaxTrees.FirstOrDefault();
		return tree?.GetText().ToString() ?? string.Empty;
	}

	/// <summary>
	/// Finds a generated tree whose file path ends with the specified suffix.
	/// </summary>
	/// <param name="filePathSuffix">The suffix to match.</param>
	/// <returns>The matching syntax tree, or <see langword="null"/> if none is found.</returns>
	public SyntaxTree? GetGeneratedTree(string filePathSuffix) =>
		GeneratedTrees.FirstOrDefault(tree => tree.FilePath.EndsWith(filePathSuffix, StringComparison.Ordinal));
}
