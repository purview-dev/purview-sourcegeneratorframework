using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;
using Purview.SourceGeneratorFramework.Models;
using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework;

public class IncrementalPipelineTests2
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

			var inputs = isDisabled
				.CombineWith(
					context.CompilationProvider.Select(
						static (compilation, _) => compilation.AssemblyName ?? "Unknown"
					),
					static (disabled, assemblyName, _) =>
						new GenerationInputs(disabled, assemblyName),
					"CreateGenerationInputs"
				)
				.CollectWith(
					targets,
					static (state, collectedTargets, _) =>
						state with
						{
							Targets = collectedTargets,
						},
					"AddGenerationTargets"
				);

			context.RegisterSourceOutput(
				inputs,
				static (spc, source) =>
				{
					if (source.IsDisabled)
						return;

					foreach (var target in source.Targets)
					{
						spc.AddSource($"{target.Name}.g.cs", $"partial class {target.Name} {{ }}");
					}
				}
			);
		}
	}

	readonly record struct TargetInfo(string Name, string FilePath);

	sealed record GenerationInputs(bool IsDisabled, string AssemblyName)
	{
		public ImmutableArray<TargetInfo> Targets { get; init; } = [];
	}
}
