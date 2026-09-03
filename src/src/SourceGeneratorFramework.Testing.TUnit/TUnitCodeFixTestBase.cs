using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Testing.TUnit;

/// <summary>
/// TUnit-specific base class for analyzer and code fix tests.
/// </summary>
public abstract class TUnitCodeFixTestBase<TAnalyzer, TCodeFix>
	: TUnitCodeFixTestBase<TAnalyzer, TCodeFix, CodeFixTestOptions>
	where TAnalyzer : DiagnosticAnalyzer, new()
	where TCodeFix : CodeFixProvider, new();

/// <summary>
/// TUnit-specific base class for analyzer and code fix tests.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1005:Avoid excessive parameters on generic types",
	Justification = "The analyzer, code fix, and options types independently define the test fixture."
)]
public abstract class TUnitCodeFixTestBase<TAnalyzer, TCodeFix, TOptions>
	where TAnalyzer : DiagnosticAnalyzer, new()
	where TCodeFix : CodeFixProvider, new()
	where TOptions : CodeFixTestOptions, new()
{
	readonly CodeFixTestRunner<TAnalyzer, TCodeFix> _runner = new();

	/// <summary>
	/// Runs the analyzer and applies the selected code fix.
	/// </summary>
	protected Task<CodeFixTestResult> ApplyCodeFixAsync(string source, CancellationToken cancellationToken = default) =>
		ApplyCodeFixAsync(source, null!, cancellationToken);

	/// <summary>
	/// Runs the analyzer and applies the selected code fix using the supplied options.
	/// </summary>
	protected Task<CodeFixTestResult> ApplyCodeFixAsync(
		string source,
		TOptions options,
		CancellationToken cancellationToken = default
	) => _runner.RunAsync(source, options ?? new(), cancellationToken);

	/// <summary>
	/// Runs the analyzer and applies the code fix to every diagnostic in the project.
	/// </summary>
	protected Task<CodeFixFixAllResult> ApplyFixAllAsync(
		IEnumerable<string> sources,
		TOptions? options = null,
		CancellationToken cancellationToken = default
	) => _runner.RunFixAllAsync(sources, options ?? new(), cancellationToken);

	/// <summary>
	/// Runs the analyzer and applies the code fix to every diagnostic in the project.
	/// </summary>
	protected Task<CodeFixFixAllResult> ApplyFixAllAsync(
		string source,
		TOptions? options = null,
		CancellationToken cancellationToken = default
	) => _runner.RunFixAllAsync([source], options ?? new(), cancellationToken);
}
