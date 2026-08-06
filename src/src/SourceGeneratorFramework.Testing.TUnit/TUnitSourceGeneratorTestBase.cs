using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Testing.TUnit;

/// <summary>
/// TUnit-specific base class for source generator tests.
/// </summary>
public abstract class TUnitSourceGeneratorTestBase<TGenerator> : SourceGeneratorTestBase<TGenerator>
	where TGenerator : class, IIncrementalGenerator, new()
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TUnitSourceGeneratorTestBase{TGenerator}"/> class.
	/// </summary>
	protected TUnitSourceGeneratorTestBase()
		: base(new TUnitTestOutput()) { }
}
