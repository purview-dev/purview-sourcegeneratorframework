namespace Purview.SourceGeneratorFramework.Testing.Generators.Model;

sealed record AttributeDataModelProperty(
	string PropertyName,
	string FullyQualifiedTypeName,
	AttributePropertySource Source,
	string? MappedName,
	int ConstructorIndex,
	string DefaultValueExpression,
	bool IsExplicit,
	bool IsNonNullableReferenceType,
	bool IsNestedModel,
	string? NestedModelTypeName = null
)
{
	public bool IsNamedArgument => Source == AttributePropertySource.NamedArgument;

	public bool IsConstructorIndex => Source == AttributePropertySource.ConstructorIndex;

	public bool IsConstructorName => Source == AttributePropertySource.ConstructorName;
}
