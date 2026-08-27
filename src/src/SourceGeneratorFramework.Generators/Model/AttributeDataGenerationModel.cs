namespace Purview.SourceGeneratorFramework.Generators.Model;

sealed record AttributeDataOutputContext(
	GenerationContext<EmptyCapabilities> Context,
	EquatableArray<GeneratorResult<AttributeDataModelTarget>> Targets
);
