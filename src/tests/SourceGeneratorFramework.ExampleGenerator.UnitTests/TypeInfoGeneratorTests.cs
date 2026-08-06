using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

public class TypeInfoGeneratorTests
{
	[Test]
	public async Task GenerateTypeInfo_GeneratesTypeInfoClass()
	{
		var source = """
			using Purview.SourceGeneratorFramework.Examples;

			namespace Test
			{
				[GenerateTypeInfo]
				public partial class MyClass { }
			}
			""";

		var runner = new SourceGeneratorTestRunner<TypeInfoGenerator>();
		var result = await runner.RunAsync(source);

		var tree = result.GetGeneratedTree(".TypeInfo.g.cs");
		var generated = tree is null ? null : (await tree.GetTextAsync()).ToString();

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static class TypeInfo");
		await Assert.That(generated).Contains("public const string Name = \"MyClass\";");
		await Assert
			.That(generated)
			.Contains("public const string FullName = \"global::Test.MyClass\";");
		await Assert.That(generated).Contains("public const string Namespace = \"Test\";");
	}

	[Test]
	public async Task GenerateTypeInfo_NonPartial_ReportsDiagnostic()
	{
		var source = """
			using Purview.SourceGeneratorFramework.Examples;

			namespace Test
			{
				[GenerateTypeInfo]
				public class NonPartialClass { }
			}
			""";

		var runner = new SourceGeneratorTestRunner<TypeInfoGenerator>();
		var result = await runner.RunAsync(source);

		await Assert.That(result.Result.Diagnostics).Contains(d => d.Id == "EXG0001");
	}
}
