using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.Generators.Model;

static class GeneratorTypeLibrary
{
	const string GeneratorsNamespace = "Purview.SourceGeneratorFramework.Generators";

	public static readonly TypeValueObject ILogSupport = TypeValueObject.Create<ILogSupport>();

	public static readonly TypeValueObject GenerationLogger = TypeValueObject.Create<GenerationLogger>();

	public static readonly TypeValueObject TypeValueObject = TypeValueObject.Create<TypeValueObject>();

	public static readonly TypeValueObject OutputType = TypeValueObject.Create<OutputType>();

	public static readonly TypeValueObject IIncrementalGenerator =
		TypeValueObject.Create<Microsoft.CodeAnalysis.IIncrementalGenerator>();

	public static readonly TypeValueObject ISourceGenerator =
		TypeValueObject.Create<Microsoft.CodeAnalysis.ISourceGenerator>();

	public static readonly TypeValueObject EmbeddedAttribute =
		TypeValueObject.Create<Microsoft.CodeAnalysis.EmbeddedAttribute>();

	public static readonly TypeValueObject GenerateAttribute = new(nameof(GenerateAttribute), GeneratorsNamespace);

	public static readonly TypeValueObject PropertyAttribute = new(nameof(PropertyAttribute), GeneratorsNamespace);

	public static readonly TypeValueObject ArgumentAttribute = new(nameof(ArgumentAttribute), GeneratorsNamespace);

	public static readonly TypeValueObject NestedModelAttribute = new(
		nameof(NestedModelAttribute),
		GeneratorsNamespace
	);

	public static readonly TypeValueObject ExcludeAttribute = new(nameof(ExcludeAttribute), GeneratorsNamespace);

	public static readonly TypeValueObject GenericTypeArgumentAttribute = new(
		nameof(GenericTypeArgumentAttribute),
		GeneratorsNamespace
	);
}
