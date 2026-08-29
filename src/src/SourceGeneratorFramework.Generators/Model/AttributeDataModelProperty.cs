namespace Purview.SourceGeneratorFramework.Generators.Model;

sealed record PropertySource(AttributePropertySource Source, string? MappedName, int ConstructorIndex);

sealed record AttributeDataModelProperty(
	string PropertyName,
	string FullyQualifiedTypeName,
	EquatableArray<PropertySource> Sources,
	string DefaultValueExpression,
	bool HasDefaultValue,
	bool IsExplicit,
	bool IsNonNullableReferenceType,
	bool IsNestedModel,
	bool IsEnum,
	bool IsTypeIdentity,
	bool IsNullableValueType,
	string? NestedModelTypeName = null
)
{
	public bool IsNamedArgument => Sources.Any(static s => s.Source == AttributePropertySource.NamedArgument);

	public bool IsConstructorIndex => Sources.Any(static s => s.Source == AttributePropertySource.ConstructorIndex);

	public bool IsConstructorName => Sources.Any(static s => s.Source == AttributePropertySource.ConstructorName);

	public bool IsNestedModelSource => Sources.Any(static s => s.Source == AttributePropertySource.NestedModel);
}
