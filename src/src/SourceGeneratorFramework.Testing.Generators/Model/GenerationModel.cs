using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework.Testing.Generators.Model;

sealed record GenerationModel(bool IsDisabled, GenerationContext GenerationContext)
{
	public ImmutableArray<GeneratorResult<TypeValueObject>> SourceGenerators { get; set; } = [];

	public ImmutableArray<DiagnosticInfo> Diagnostics { get; set; } = [];
}
