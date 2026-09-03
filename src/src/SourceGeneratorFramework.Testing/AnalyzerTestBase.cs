using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Framework-agnostic base class for diagnostic analyzer tests.
/// </summary>
/// <typeparam name="TAnalyzer">The type of diagnostic analyzer.</typeparam>
public abstract class AnalyzerTestBase<TAnalyzer> : AnalyzerTestBase<TAnalyzer, AnalyzerTestOptions>
	where TAnalyzer : DiagnosticAnalyzer, new();

/// <summary>
/// Framework-agnostic base class for diagnostic analyzer tests.
/// </summary>
/// <typeparam name="TAnalyzer">The type of diagnostic analyzer.</typeparam>
/// <typeparam name="TOptions">The type of test options.</typeparam>
public abstract class AnalyzerTestBase<TAnalyzer, TOptions>
	where TAnalyzer : DiagnosticAnalyzer, new()
	where TOptions : AnalyzerTestOptions, new()
{
	readonly DiagnosticAnalyzerTestRunner<TAnalyzer> _runner = new();

	/// <summary>
	/// Runs the analyzer against the supplied source.
	/// </summary>
	protected Task<AnalyzerTestResult> AnalyzeAsync(string source, CancellationToken cancellationToken = default) =>
		AnalyzeAsync(source, null!, cancellationToken);

	/// <summary>
	/// Runs the analyzer against the supplied sources.
	/// </summary>
	protected Task<AnalyzerTestResult> AnalyzeAsync(
		IEnumerable<string> sources,
		CancellationToken cancellationToken = default
	) => AnalyzeAsync(sources, null!, cancellationToken);

	/// <summary>
	/// Runs the analyzer against the supplied source and options.
	/// </summary>
	protected Task<AnalyzerTestResult> AnalyzeAsync(
		string source,
		TOptions options,
		CancellationToken cancellationToken = default
	) => AnalyzeAsync([source], options, cancellationToken);

	/// <summary>
	/// Runs the analyzer against the supplied sources and options.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1849:Call async methods when in an async method"
	)]
	protected async Task<AnalyzerTestResult> AnalyzeAsync(
		IEnumerable<string> sources,
		TOptions options,
		CancellationToken cancellationToken = default
	)
	{
		if (sources is null)
			throw new ArgumentNullException(nameof(sources));

		options ??= new();
		options = OnBeforeRun(sources, options, cancellationToken);
		options = await OnBeforeRunAsync(sources, options, cancellationToken);

		var result = await _runner.RunAsync(sources, options, cancellationToken);

		OnAfterRun(result, sources, options, cancellationToken);
		await OnAfterRunAsync(result, sources, options, cancellationToken);

		return result;
	}

	/// <summary>
	/// Called before the analyzer is run.
	/// </summary>
	protected virtual TOptions OnBeforeRun(
		IEnumerable<string> sources,
		TOptions options,
		CancellationToken cancellationToken
	) => options;

	/// <summary>
	/// Called asynchronously before the analyzer is run.
	/// </summary>
	protected virtual Task<TOptions> OnBeforeRunAsync(
		IEnumerable<string> sources,
		TOptions options,
		CancellationToken cancellationToken
	) => Task.FromResult(options);

	/// <summary>
	/// Called after the analyzer is run.
	/// </summary>
	protected virtual void OnAfterRun(
		AnalyzerTestResult result,
		IEnumerable<string> sources,
		TOptions options,
		CancellationToken cancellationToken
	)
	{
		// No-op by default.
	}

	/// <summary>
	/// Called asynchronously after the analyzer is run.
	/// </summary>
	protected virtual Task OnAfterRunAsync(
		AnalyzerTestResult result,
		IEnumerable<string> sources,
		TOptions options,
		CancellationToken cancellationToken
	) => Task.CompletedTask;
}
