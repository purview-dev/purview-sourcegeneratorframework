using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework.Generators.Model;

sealed record LogGenerationModel(bool IsDisabled, GenerationContext GenerationContext)
{
	public ImmutableArray<GeneratorResult<TypeValueObject>> SourceGenerators { get; set; } = [];

	public ImmutableArray<DiagnosticInfo> Diagnostics { get; set; } = [];
}
