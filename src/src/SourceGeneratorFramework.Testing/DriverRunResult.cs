using System.Reflection;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing.Abstractions;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// The result of a source generator test run.
/// </summary>
public record class DriverRunResult(
	GeneratorDriverRunResult Result,
	Compilation OutputCompilation,
	Assembly? Assembly,
	IEnumerable<SyntaxTree> GeneratedTrees,
	IEnumerable<SyntaxTree> NonAttributeSyntaxTrees,
	IReadOnlyList<(string Message, OutputType Type)> LogEntries
)
{
	/// <summary>
	/// Gets the generated syntax trees excluding configured attribute files.
	/// </summary>
	public IEnumerable<SyntaxTree> SyntaxTrees => NonAttributeSyntaxTrees;

	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> if the run contains generation exceptions,
	/// compilation errors, or generator log errors (depending on configured options).
	/// </summary>
	public void EnsureValid()
	{
		var generationExceptions = Result
			.Results.Select(r => r.Exception)
			.Where(e => e != null)
			.ToList();
		if (generationExceptions.Count > 0)
		{
			throw new InvalidOperationException(
				"Generator threw exceptions:\n"
					+ string.Join("\n", generationExceptions.Select(e => e!.ToString()))
			);
		}

		var compilationErrors = OutputCompilation
			.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		if (compilationErrors.Count > 0)
		{
			throw new InvalidOperationException(
				"Compilation errors:\n"
					+ string.Join("\n", compilationErrors.Select(d => d.ToString()))
			);
		}

		var logErrors = LogEntries
			.Where(e => e.Type == OutputType.Error)
			.Select(e => e.Message)
			.ToList();
		if (logErrors.Count > 0)
		{
			throw new InvalidOperationException(
				"Generator logged errors:\n" + string.Join("\n", logErrors)
			);
		}
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
		GeneratedTrees.FirstOrDefault(tree =>
			tree.FilePath.EndsWith(filePathSuffix, StringComparison.Ordinal)
		);
}
