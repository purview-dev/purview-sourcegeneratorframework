using Purview.SourceGeneratorFramework.TestGenerators;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Helpers;

public class IncrementalPipelineTests_GenerationContextTestGenerator
	: TUnitSourceGeneratorTestBase<GenerationContextTestGenerator>
{
	const string TestAttributeSource = """
[System.AttributeUsage(System.AttributeTargets.Class)]
public sealed class TestAttribute : System.Attribute { }
""";

	[Test]
	public async Task GenerationContextValueProvider_WithoutScopeParameter_DoesNotRecurse(
		CancellationToken cancellationToken
	)
	{
		const string source = """
[TestAttribute]
public partial class MyClass;

""";

		var result = await GenerateAsync(source, cancellationToken);
		var tree = result.GetSource();

		await Assert.That(tree).IsNotNull();
	}

	protected override SourceGeneratorTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		SourceGeneratorTestOptions options,
		CancellationToken cancellationToken
	)
	{
		return base.OnBeforeRun(
			sources,
			options.WithAdditionalSources(TestAttributeSource).WithExcludeGeneratedSourceHintNames("TestAttribute"),
			cancellationToken
		);
	}
}
