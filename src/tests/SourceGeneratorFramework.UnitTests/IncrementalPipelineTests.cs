using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;
using Purview.SourceGeneratorFramework.Models;
using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework;

public class IncrementalPipelineTests
{
	const string TestAttributeSource = """
		namespace Test
		{
			[System.AttributeUsage(System.AttributeTargets.Class)]
			public sealed class TestAttribute : System.Attribute { }
		}
		""";

	[Test]
	public async Task ForAttributeWithMetadataName_FindsAttributedClass()
	{
		var source =
			"""
				using Test;

				[TestAttribute]
				public partial class MyClass { }
				"""
			+ "\n"
			+ TestAttributeSource;

		var runner = new SourceGeneratorTestRunner<TestGenerator>();
		var result = await runner.RunAsync(source);

		var tree = result.GetGeneratedTree("MyClass.g.cs");
		await Assert.That(tree).IsNotNull();
		await Assert.That(tree!.FilePath).EndsWith("MyClass.g.cs");
		await Assert.That(tree.ToString()).Contains("class MyClass");
	}

	[Test]
	public async Task IsDisabledValueProvider_WhenDisabled_DoesNotGenerate()
	{
		var source =
			"""
				using Test;

				[TestAttribute]
				public partial class MyClass { }
				"""
			+ "\n"
			+ TestAttributeSource;

		var runner = new SourceGeneratorTestRunner<TestGenerator>();
		var result = await runner.RunAsync(
			source,
			new SourceGeneratorTestOptions
			{
				DisableSourceGeneratorPropertyName = "DisableTestGenerator",
				DisableSourceGeneratorValue = true,
			}
		);

		await Assert.That(result.GeneratedTrees.Any()).IsFalse();
	}

	internal sealed class TestGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var isDisabled = IncrementalPipeline.IsDisabledValueProvider(
				context,
				"DisableTestGenerator"
			);
			var targets = IncrementalPipeline.ForAttributeWithMetadataName(
				context,
				new TypeValueObject("TestAttribute", "Test"),
				static (ctx, ct) =>
				{
					var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.TargetNode, ct);
					return new TargetInfo(
						symbol?.Name ?? "Unknown",
						ctx.TargetNode.SyntaxTree.FilePath
					);
				},
				predicate: static (node, _) =>
					node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax
			);

			var combined = targets.Collect().Combine(isDisabled);

			context.RegisterSourceOutput(
				combined,
				static (spc, source) =>
				{
					if (source.Right)
						return;

					foreach (var target in source.Left)
					{
						spc.AddSource($"{target.Name}.g.cs", $"partial class {target.Name} {{ }}");
					}
				}
			);
		}
	}

	readonly record struct TargetInfo(string Name, string FilePath);
}
