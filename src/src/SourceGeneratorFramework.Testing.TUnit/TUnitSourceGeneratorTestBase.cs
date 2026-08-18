using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Testing.TUnit;

/// <summary>
/// TUnit-specific base class for source generator tests.
/// </summary>
/// <typeparam name="TGenerator">The type of the source generator.</typeparam>
public abstract class TUnitSourceGeneratorTestBase<TGenerator>
	: TUnitSourceGeneratorTestBase<TGenerator, SourceGeneratorTestOptions>
	where TGenerator : class, IIncrementalGenerator, new();

/// <summary>
/// TUnit-specific base class for source generator tests.
/// </summary>
/// <typeparam name="TGenerator">The type of the source generator.</typeparam>
/// <typeparam name="TContext">The type of the test context.</typeparam>
public abstract class TUnitSourceGeneratorTestBase<TGenerator, TContext> : SourceGeneratorTestBase<TGenerator, TContext>
	where TGenerator : class, IIncrementalGenerator, new()
	where TContext : SourceGeneratorTestOptions, new()
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TUnitSourceGeneratorTestBase{TGenerator, TContext}"/> class.
	/// </summary>
	protected TUnitSourceGeneratorTestBase()
		: base(new TUnitTestOutput()) { }
}
