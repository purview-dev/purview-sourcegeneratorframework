using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework.Generators.Model;

sealed record AttributeDataGenerationModel(bool IsDisabled, GenerationContext GenerationContext)
{
	public ImmutableArray<GeneratorResult<AttributeDataModelTarget>
#pragma warning disable format
	> AttributeDataTargets { get; set; } = [];
#pragma warning restore format

	public ImmutableArray<DiagnosticInfo> Diagnostics { get; set; } = [];
}
