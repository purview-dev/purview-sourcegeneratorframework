using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Benchmarks;

public sealed class SimpleGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static ctx =>
			ctx.AddSource("Simple.g.cs", "namespace Benchmarks { public static class Simple { } }")
		);
	}
}
