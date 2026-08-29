using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.TestGenerators;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Helpers;

public class IncrementalPipelineTests_TestGenerator : TUnitSourceGeneratorTestBase<TestGenerator>
{
	const string TestAttributeSource = """
[System.AttributeUsage(System.AttributeTargets.Class)]
public sealed class TestAttribute : System.Attribute { }
""";

	[Test]
	public async Task ForAttributeWithMetadataName_FindsAttributedClass(CancellationToken cancellationToken)
	{
		const string source = """
[TestAttribute]
public partial class MyClass { }
""";

		var result = await GenerateAsync(source, cancellationToken);
		var tree = result.GetSource();

		await Assert.That(tree).IsNotNull();
		await Assert.That(tree).ContainsGeneratedCode("class MyClass");
	}

	[Test]
	public async Task IsDisabledValueProvider_WhenDisabled_DoesNotGenerate(CancellationToken cancellationToken)
	{
		const string source = """
[TestAttribute]
public partial class MyClass { }
""";

		var result = await GenerateAsync(
			source,
			new() { DisableSourceGeneratorPropertyName = "DisableTestGenerator", DisableSourceGeneratorValue = true },
			cancellationToken
		);

		await Assert.That(result.AllSyntaxTrees.Any()).IsFalse();
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
