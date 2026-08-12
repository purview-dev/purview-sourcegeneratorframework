using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework.Testing.Generators.Model;

sealed record PropertySource(
	AttributePropertySource Source,
	string? MappedName,
	int ConstructorIndex
);

sealed record AttributeDataModelProperty(
	string PropertyName,
	string FullyQualifiedTypeName,
	ImmutableArray<PropertySource> Sources,
	string DefaultValueExpression,
	bool HasDefaultValue,
	bool IsExplicit,
	bool IsNonNullableReferenceType,
	bool IsNestedModel,
	string? NestedModelTypeName = null
)
{
	public bool IsNamedArgument =>
		Sources.Any(static s => s.Source == AttributePropertySource.NamedArgument);

	public bool IsConstructorIndex =>
		Sources.Any(static s => s.Source == AttributePropertySource.ConstructorIndex);

	public bool IsConstructorName =>
		Sources.Any(static s => s.Source == AttributePropertySource.ConstructorName);

	public bool IsNestedModelSource =>
		Sources.Any(static s => s.Source == AttributePropertySource.NestedModel);
}
