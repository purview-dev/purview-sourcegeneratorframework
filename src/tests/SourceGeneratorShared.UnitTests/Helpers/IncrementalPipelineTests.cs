using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Helpers;

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

		await Assert.That(result.AllSyntaxTrees.Any()).IsFalse();
	}

	internal sealed class TestGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var isDisabled = IncrementalPipeline.IsDisabledValueProvider(context, "DisableTestGenerator");
			var targets = IncrementalPipeline.ForAttributeWithMetadataName(
				context,
				new TypeValueObject("TestAttribute", "Test"),
				static (ctx, ct) =>
				{
					var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.TargetNode, ct);
					return new TargetInfo(symbol?.Name ?? "Unknown");
				},
				predicate: static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax
			);

			var inputs = isDisabled
				.CombineWith(
					context.CompilationProvider.Select(
						static (compilation, _) => compilation.AssemblyName ?? "Unknown"
					),
					static (disabled, assemblyName, _) => new GenerationInputs(disabled, assemblyName),
					"CreateGenerationInputs"
				)
				.CollectWith(
					targets,
					static (state, collectedTargets, _) => state with { Targets = collectedTargets },
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

	[Test]
	public async Task RegisterSourceOutput_ReportsDiagnosticsAndGeneratesForSuccessfulTargets()
	{
		var source =
			"""
				using Test;

				[TestAttribute]
				public partial class MyClass { }
				"""
			+ "\n"
			+ TestAttributeSource;

		var runner = new SourceGeneratorTestRunner<DiagnosticTestGenerator>();
		var result = await runner.RunAsync(source);

		var tree = result.GetGeneratedTree("MyClass.g.cs");
		await Assert.That(tree).IsNotNull();
		await Assert.That(tree!.ToString()).Contains("class MyClass");
		await Assert.That(result.DriverResult.Diagnostics).IsNotEmpty();
	}

	[Test]
	public async Task GenerationContextValueProvider_WithoutScopeParameter_DoesNotRecurse()
	{
		var runner = new SourceGeneratorTestRunner<GenerationContextTestGenerator>();
		var result = await runner.RunAsync("public class C { }");

		var tree = result.GetGeneratedTree("Context.g.cs");
		await Assert.That(tree).IsNotNull();
		await Assert.That(tree!.ToString()).Contains("TestAssembly");
	}

	readonly record struct TargetInfo(string Name);

	sealed record GenerationInputs(bool IsDisabled, string AssemblyName)
	{
		public ImmutableArray<TargetInfo> Targets { get; init; } = [];
	}

	internal sealed class DiagnosticTestGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var targets = IncrementalPipeline.ForAttributeWithMetadataName(
				context,
				new TypeValueObject("TestAttribute", "Test"),
				static (ctx, ct) =>
				{
					var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.TargetNode, ct);
					var name = symbol?.Name ?? "Unknown";
					var diagnostic = DiagnosticInfo.Create(
						new DiagnosticDescriptor(
							"TEST001",
							"Test diagnostic",
							$"Processed {name}",
							"Test",
							DiagnosticSeverity.Info,
							isEnabledByDefault: true
						),
						Location.None
					);
					return GeneratorResult<TargetInfo>.Ok(new TargetInfo(name), diagnostic);
				}
			);

			var generationContext = IncrementalPipeline.DefaultGenerationContextValueProvider(
				context,
				"DiagnosticTestGenerator",
				"1.0.0"
			);

			IncrementalPipeline.RegisterSourceOutput(
				context,
				targets,
				generationContext,
				static (spc, target, ctx) =>
				{
					var writer = ctx.CreateCodeWriter();
					writer.WriteLine($"partial class {target.Name} {{ }}");
					spc.AddSource($"{target.Name}.g.cs", writer.ToString());
				}
			);
		}
	}

	internal sealed class GenerationContextTestGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var generationContext = IncrementalPipeline.GenerationContextValueProvider(
				context,
				"GenerationContextTestGenerator",
				"1.0.0",
				static (compilation, settings, logger, _) => new GenerationContext(compilation, settings, logger)
			);

			context.RegisterSourceOutput(
				generationContext,
				static (spc, value) => spc.AddSource("Context.g.cs", $"// {value.AssemblyName}")
			);
		}
	}
}
