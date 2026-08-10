using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Framework-agnostic base class for source generator tests.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SourceGeneratorTestBase{TGenerator}"/> class.
/// </remarks>
/// <param name="testOutput">The test output receiver.</param>
public abstract class SourceGeneratorTestBase<TGenerator>(ITestOutput testOutput)
	where TGenerator : class, IIncrementalGenerator, new()
{
	readonly SourceGeneratorTestRunner<TGenerator> _runner = new();

	/// <summary>
	/// Runs the generator against the supplied source and options.
	/// </summary>
	/// <param name="source">The source code to generate.</param>
	/// <param name="options">The options to use for the generation.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>The result of the generator run.</returns>
	protected Task<DriverRunResult> GenerateAsync(
		string source,
		SourceGeneratorTestOptions? options = null,
		CancellationToken cancellationToken = default
	) => GenerateAsync([source], options, cancellationToken);

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
		SourceGeneratorTestOptions? options = null,
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
		SourceGeneratorTestOptions options,
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
		SourceGeneratorTestOptions options,
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
		SourceGeneratorTestOptions options,
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
		SourceGeneratorTestOptions options,
		CancellationToken cancellationToken
	) => Task.CompletedTask;
}
