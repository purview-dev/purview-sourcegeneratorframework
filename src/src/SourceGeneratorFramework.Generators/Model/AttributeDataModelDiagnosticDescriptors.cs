using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Generators.Model;

static class AttributeDataModelDiagnosticDescriptors
{
	const string Category = "AttributeDataModelGenerator";

	public static readonly DiagnosticDescriptor TargetAttributeNotResolved = new(
		"ADM0001",
		"Target attribute type cannot be resolved",
		"Target attribute type for '{0}' cannot be resolved",
		Category,
		DiagnosticSeverity.Error,
		true
	);

	public static readonly DiagnosticDescriptor PropertyTypeNotSupported = new(
		"ADM0002",
		"Property type is not supported for attribute extraction",
		"Property '{0}' type '{1}' is not supported for attribute extraction",
		Category,
		DiagnosticSeverity.Error,
		true
	);

	public static readonly DiagnosticDescriptor ConstructorMemberNotFound = new(
		"ADM0003",
		"Specified constructor index/name does not exist on the target attribute",
		"Constructor argument '{0}' does not exist on target attribute '{1}'",
		Category,
		DiagnosticSeverity.Error,
		true
	);

	public static readonly DiagnosticDescriptor NestedModelNotGenerated = new(
		"ADM0004",
		"Nested model type is not annotated with GenerateAttributeDataModel",
		"Nested model type '{0}' is not annotated with GenerateAttributeDataModel",
		Category,
		DiagnosticSeverity.Error,
		true
	);

	public static readonly DiagnosticDescriptor DefaultValueNotSupported = new(
		"ADM0005",
		"Default value cannot be emitted for the property type",
		"Default value '{0}' cannot be emitted for property type '{1}'",
		Category,
		DiagnosticSeverity.Error,
		true
	);

	public static readonly DiagnosticDescriptor NonNullableReferenceTypeRequiresDefault = new(
		"ADM0006",
		"Non-nullable reference type property requires a default value",
		"Non-nullable reference type property '{0}' requires an explicit or inferred default value",
		Category,
		DiagnosticSeverity.Error,
		true
	);

	public static readonly DiagnosticDescriptor AutoDiscoverRequiresType = new(
		"ADM0007",
		"Auto-discovery requires a target attribute type",
		"Auto-discovery requires a target attribute type; use the Type constructor overload instead of the string overload",
		Category,
		DiagnosticSeverity.Error,
		true
	);

	public static readonly DiagnosticDescriptor TypeArgumentPropertyTypeInvalid = new(
		"ADM0008",
		"Type argument property type must be a symbol type",
		"Type argument property '{0}' type '{1}' must be ITypeSymbol or INamedTypeSymbol",
		Category,
		DiagnosticSeverity.Error,
		true
	);

	public static readonly DiagnosticDescriptor IsEnumRequiresStringType = new(
		"ADM0009",
		"IsEnum property must be a string type",
		"Property '{0}' is marked with IsEnum but its type '{1}' is not a string; IsEnum requires a string or string? property type",
		Category,
		DiagnosticSeverity.Error,
		true
	);
}
