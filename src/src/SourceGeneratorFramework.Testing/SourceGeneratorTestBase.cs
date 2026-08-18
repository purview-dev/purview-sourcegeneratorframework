using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Framework-agnostic base class for source generator tests.
/// </summary>
/// <typeparam name="TGenerator">The type of the source generator.</typeparam>
/// <param name="testOutput">The test output receiver.</param>
public abstract class SourceGeneratorTestBase<TGenerator>(ITestOutput testOutput)
	: SourceGeneratorTestBase<TGenerator, SourceGeneratorTestOptions>(testOutput)
	where TGenerator : class, IIncrementalGenerator, new();

/// <summary>
/// Framework-agnostic base class for source generator tests.
/// </summary>
/// <param name="testOutput">The test output receiver.</param>
/// <typeparam name="TGenerator">The type of the source generator.</typeparam>
/// <typeparam name="TOptions">The type of the test options.</typeparam>
public abstract class SourceGeneratorTestBase<TGenerator, TOptions>(ITestOutput testOutput)
	where TGenerator : class, IIncrementalGenerator, new()
	where TOptions : SourceGeneratorTestOptions, new()
{
	readonly SourceGeneratorTestRunner<TGenerator> _runner = new();

	/// <summary>
	/// Runs the generator against the supplied source and options.
	/// </summary>
	/// <param name="source">The source code to generate.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>The result of the generator run.</returns>
	protected Task<DriverRunResult> GenerateAsync(string source, CancellationToken cancellationToken = default) =>
		GenerateAsync(source, null!, cancellationToken);

	/// <summary>
	/// Runs the generator against the supplied sources and options.
	/// </summary>
	/// <param name="sources">The source code files to generate.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>The result of the generator run.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1849:Call async methods when in an async method"
	)]
	protected async Task<DriverRunResult> GenerateAsync(
		IEnumerable<string> sources,
		CancellationToken cancellationToken = default
	) => await GenerateAsync(sources, null!, cancellationToken);

	/// <summary>
	/// Runs the generator against the supplied source and options.
	/// </summary>
	/// <param name="source">The source code to generate.</param>
	/// <param name="options">The options to use for the generation.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>The result of the generator run.</returns>
	protected async Task<DriverRunResult> GenerateAsync(
		string source,
		TOptions options,
		CancellationToken cancellationToken = default
	) => await GenerateAsync([source], options, cancellationToken);

	/// <summary>
	/// Runs the generator against the supplied sources and options.
	/// </summary>
	/// <param name="sources">The source code files to generate.</param>
	/// <param name="options">The options to use for the generation.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>The result of the generator run.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1849:Call async methods when in an async method"
	)]
	protected async Task<DriverRunResult> GenerateAsync(
		IEnumerable<string> sources,
		TOptions options,
		CancellationToken cancellationToken = default
	)
	{
		if (sources is null)
			throw new ArgumentNullException(nameof(sources));

		options ??= new();
		options = options with { TestOutput = testOutput };

		OnBeforeRun(sources, options, cancellationToken);
		await OnBeforeRunAsync(sources, options, cancellationToken);

		var result = await _runner.RunAsync(sources, options, cancellationToken);
		if (options.ThrowOnGenerationException)
			result.EnsureValid();

		OnAfterRun(result, sources, options, cancellationToken);
		await OnAfterRunAsync(result, sources, options, cancellationToken);

		return result;
	}

	/// <summary>
	/// Called after the generator is run, allowing for inspection of the results.
	/// </summary>
	/// <param name="result">The result of the generator run.</param>
	/// <param name="sources">The source code files that were generated.</param>
	/// <param name="options">The options used for the generation.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	protected virtual void OnAfterRun(
		DriverRunResult result,
		IEnumerable<string> sources,
		TOptions options,
		CancellationToken cancellationToken
	)
	{
		//
	}

	/// <summary>
	/// Called after the generator is run, allowing for inspection of the results.
	/// </summary>
	/// <param name="result">The result of the generator run.</param>
	/// <param name="sources">The source code files that were generated.</param>
	/// <param name="options">The options used for the generation.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	protected virtual Task OnAfterRunAsync(
		DriverRunResult result,
		IEnumerable<string> sources,
		TOptions options,
		CancellationToken cancellationToken
	) => Task.CompletedTask;

	/// <summary>
	/// Called before the generator is run, allowing for customization of the sources and options.
	/// </summary>
	/// <param name="sources">The source code files to generate.</param>
	/// <param name="options">The options to use for the generation.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	protected virtual void OnBeforeRun(
		IEnumerable<string> sources,
		TOptions options,
		CancellationToken cancellationToken
	)
	{
		//
	}

	/// <summary>
	/// Called before the generator is run, allowing for customization of the sources and options.
	/// </summary>
	/// <param name="sources">The source code files to generate.</param>
	/// <param name="options">The options to use for the generation.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	protected virtual Task OnBeforeRunAsync(
		IEnumerable<string> sources,
		TOptions options,
		CancellationToken cancellationToken
	) => Task.CompletedTask;
}
