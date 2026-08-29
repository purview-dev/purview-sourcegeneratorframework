using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;

namespace Purview.SourceGeneratorFramework.TestGenerators;

public sealed class DiagnosticTestGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var targets = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			new TypeIdentity("TestAttribute", null),
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
				return GeneratorResult<TargetInfo>.Create(new TargetInfo(name), diagnostic);
			}
		);

		var generationContext = IncrementalPipeline.DefaultGenerationContextValueProvider(
			context,
			new GenerationSettings("DiagnosticTestGenerator", "1.0.0")
		);

		context.RegisterSourceOutput(
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
