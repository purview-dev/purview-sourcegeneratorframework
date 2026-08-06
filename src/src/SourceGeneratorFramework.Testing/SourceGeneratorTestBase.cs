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
	protected Task<DriverRunResult> GenerateAsync(
		string source,
		SourceGeneratorTestOptions? options = null,
		CancellationToken cancellationToken = default
	)
	{
		options ??= new SourceGeneratorTestOptions();
		options = options with { TestOutput = testOutput };
		return _runner.RunAsync(source, options, cancellationToken);
	}
}
