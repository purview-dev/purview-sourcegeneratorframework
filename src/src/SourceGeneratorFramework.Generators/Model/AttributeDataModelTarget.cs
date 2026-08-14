namespace Purview.SourceGeneratorFramework.Generators.Model;

sealed record AttributeDataModelTarget(
	string? Namespace,
	string StructName,
	TypeDeclarationAccessibility? Accessibility,
	TypeValueObject TargetAttribute,
	bool MatchByInheritance,
	bool AutoDiscover,
	EquatableArray<AttributeDataModelProperty> Properties,
	EquatableArray<DiagnosticInfo> Diagnostics
)
{
	public bool HasDiagnostics => !Diagnostics.IsEmpty;
}
