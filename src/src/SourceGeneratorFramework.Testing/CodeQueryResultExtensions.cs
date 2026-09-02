using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Exposes <see cref="CodeQuery"/> instances over the code produced by each test result type. Queries default
/// to generated code first for source-generator runs (see <c>Generated</c>), with the full output available
/// via <c>Output</c>.
/// </summary>
public static class CodeQueryResultExtensions
{
	/// <summary>Gets a query over the generated trees of a source-generator run, with the output compilation.</summary>
	public static CodeQuery Generated(this DriverRunResult result)
	{
		if (result is null)
			throw new ArgumentNullException(nameof(result));

		var compilation = result.CompilationResult.Compilation;
		return new(result.AllSyntaxTrees, compilation, isGenerated: true);
	}

	/// <summary>Gets a query over the entire output compilation (user and generated trees) of a source-generator run.</summary>
	public static CodeQuery Output(this DriverRunResult result)
	{
		if (result is null)
			throw new ArgumentNullException(nameof(result));

		var compilation = result.CompilationResult.Compilation;
		return new([.. compilation.SyntaxTrees], compilation);
	}

	/// <summary>Gets a query over the trees of an analyzer test compilation.</summary>
	public static CodeQuery Code(this AnalyzerTestResult result)
	{
		if (result is null)
			throw new ArgumentNullException(nameof(result));

		// Analyzer tests do not produce a changed solution, so the compilation is always the input compilation.
		return new([.. result.Compilation.SyntaxTrees], result.Compilation);
	}

	/// <summary>Gets a query over the input compilation of a code-fix test.</summary>
	public static CodeQuery Code(this CodeFixTestResult result)
	{
		if (result is null)
			throw new ArgumentNullException(nameof(result));

		// Code-fix tests do not produce a changed solution, so the compilation is always the input compilation.
		return new([.. result.Compilation.SyntaxTrees], result.Compilation);
	}

	/// <summary>Gets a query over the fixed source produced by a code-fix test.</summary>
	public static CodeQuery FixedCode(this CodeFixTestResult result)
	{
		if (result is null)
			throw new ArgumentNullException(nameof(result));

		if (result.ChangedSolution is { } solution)
		{
			var compilation = GetCompilation(solution);

			return compilation is null ? new([], null) : new([.. compilation.SyntaxTrees], compilation);
		}

		return new(ParseSource(result.FixedSource), null);
	}

	/// <summary>Gets a query over the fixed sources produced by a fix-all code-fix test.</summary>
	public static CodeQuery FixedCode(this CodeFixFixAllResult result)
	{
		if (result is null)
			throw new ArgumentNullException(nameof(result));

		var compilation = GetCompilation(result.ChangedSolution);

		return compilation is null ? new([], null) : new([.. compilation.SyntaxTrees], compilation);
	}

	/// <summary>Gets a query over the refactored sources produced by a refactoring test.</summary>
	public static CodeQuery FixedCode(this RefactorTestResult result)
	{
		if (result is null)
			throw new ArgumentNullException(nameof(result));

		var compilation = GetCompilation(result.ChangedSolution);

		return compilation is null ? new([], null) : new([.. compilation.SyntaxTrees], compilation);
	}

	static ImmutableArray<SyntaxTree> ParseSource(string source) =>
		[CSharpSyntaxTree.ParseText(source ?? string.Empty)];

	static Compilation? GetCompilation(Solution solution)
	{
		var project = solution.Projects.FirstOrDefault();

		return project?.GetCompilationAsync().GetAwaiter().GetResult();
	}
}
