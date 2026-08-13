namespace Purview.SourceGeneratorFramework.Testing.Generators.Model;

static class TypeLibrary
{
	const string GeneratorsNamespace = "Purview.SourceGeneratorFramework.Testing.Generators";

	public static readonly TypeValueObject ILogSupport =
		TypeValueObject.Create<Abstractions.ILogSupport>();

	public static readonly TypeValueObject GenerationLogger =
		TypeValueObject.Create<Abstractions.GenerationLogger>();

	public static readonly TypeValueObject OutputType =
		TypeValueObject.Create<Abstractions.OutputType>();

	public static readonly TypeValueObject IIncrementalGenerator =
		TypeValueObject.Create<Microsoft.CodeAnalysis.IIncrementalGenerator>();

	public static readonly TypeValueObject ISourceGenerator =
		TypeValueObject.Create<Microsoft.CodeAnalysis.ISourceGenerator>();

	public static readonly TypeValueObject EmbeddedAttribute =
		TypeValueObject.Create<Microsoft.CodeAnalysis.EmbeddedAttribute>();

	public static readonly TypeValueObject GenerateAttributeDataModelAttribute = new(
		nameof(GenerateAttributeDataModelAttribute),
		GeneratorsNamespace
	);

	public static readonly TypeValueObject AttributeNamedPropertyAttribute = new(
		nameof(AttributeNamedPropertyAttribute),
		GeneratorsNamespace
	);

	public static readonly TypeValueObject AttributeCtorPropertyAttribute = new(
		nameof(AttributeCtorPropertyAttribute),
		GeneratorsNamespace
	);

	public static readonly TypeValueObject AttributeNestedModelPropertyAttribute = new(
		nameof(AttributeNestedModelPropertyAttribute),
		GeneratorsNamespace
	);

	public static readonly TypeValueObject AttributeExcludePropertyAttribute = new(
		nameof(AttributeExcludePropertyAttribute),
		GeneratorsNamespace
	);

	public static readonly TypeValueObject AttributeGenericTypeArgumentPropertyAttribute = new(
		nameof(AttributeGenericTypeArgumentPropertyAttribute),
		GeneratorsNamespace
	);
}
