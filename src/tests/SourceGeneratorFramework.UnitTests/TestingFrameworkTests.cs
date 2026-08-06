using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework;

public class TestingFrameworkTests
{
	const string GenerateAttributeSource = """
		namespace Test
		{
			[System.AttributeUsage(System.AttributeTargets.Class)]
			public sealed class GenerateAttribute : System.Attribute { }
		}
		""";

	internal sealed class TestGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
				"Test.GenerateAttribute",
				(node, _) => node is ClassDeclarationSyntax,
				(ctx, _) => ctx.TargetSymbol.Name
			);

			context.RegisterSourceOutput(
				provider,
				static (spc, name) =>
					spc.AddSource(
						$"{name}.g.cs",
						$@"
namespace Test
{{
	public static class Generated_{name}
	{{
		public const string Name = ""{name}"";
	}}
}}
"
					)
			);
		}
	}

	[Test]
	public async Task RunAsync_MultipleSources_GeneratesForBoth()
	{
		var source1 =
			"""
				using Test;
				[Generate]
				public class A { }
				"""
			+ "\n"
			+ GenerateAttributeSource;
		var source2 =
			"""
				using Test;
				[Generate]
				public class B { }
				"""
			+ "\n"
			+ GenerateAttributeSource;

		var runner = new SourceGeneratorTestRunner<TestGenerator>();
		var result = await runner.RunAsync([source1, source2]);

		result.AssertGeneratedSourceCount(2);
	}

	[Test]
	public async Task AssertSingleGeneratedSource_ReturnsGeneratedSource()
	{
		var source =
			"""
				using Test;
				[Generate]
				public class A { }
				"""
			+ "\n"
			+ GenerateAttributeSource;

		var runner = new SourceGeneratorTestRunner<TestGenerator>();
		var result = await runner.RunAsync(source);

		var generated = result.AssertSingleGeneratedSource();

		await Assert.That(generated).Contains("public static class Generated_A");
	}

	[Test]
	public async Task AssertGeneratedSourceContains_MatchesGeneratedText()
	{
		var source =
			"""
				using Test;
				[Generate]
				public class A { }
				"""
			+ "\n"
			+ GenerateAttributeSource;

		var runner = new SourceGeneratorTestRunner<TestGenerator>();
		var result = await runner.RunAsync(source);

		result.AssertGeneratedSourceContains("public static class Generated_A");
	}

	[Test]
	public async Task AssertNoCompilationErrors_PassingRun_DoesNotThrow()
	{
		var source =
			"""
				using Test;
				[Generate]
				public class A { }
				"""
			+ "\n"
			+ GenerateAttributeSource;

		var runner = new SourceGeneratorTestRunner<TestGenerator>();
		var result = await runner.RunAsync(source);

		result.AssertNoCompilationErrors();
	}
}
