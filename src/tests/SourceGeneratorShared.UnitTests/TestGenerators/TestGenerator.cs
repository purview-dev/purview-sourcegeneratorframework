using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;

namespace Purview.SourceGeneratorFramework.TestGenerators;

public sealed class TestGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var isDisabled = IncrementalPipeline.IsDisabledValueProvider(context, "DisableTestGenerator");
		var targets = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			new TypeIdentity("TestAttribute", null),
			static (ctx, ct) =>
			{
				var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.TargetNode, ct);
				return new TargetInfo(symbol?.Name ?? "Unknown");
			},
			predicate: static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax
		);

		var inputs = isDisabled
			.CombineWith(
				context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName ?? "Unknown"),
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
