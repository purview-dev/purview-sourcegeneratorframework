namespace Purview.SourceGeneratorFramework.Generators.Model;

sealed record AttributeDataModelTarget(
	string? Namespace,
	string StructName,
	TypeDeclarationAccessibility? Accessibility,
	bool IsRecord,
	bool IsReadOnly,
	TypeValueObject TargetAttribute,
	bool MatchByInheritance,
	bool AutoDiscover,
	EquatableArray<string> PrimaryConstructorArguments,
	EquatableArray<AttributeDataModelProperty> Properties,
	EquatableArray<DiagnosticInfo> Diagnostics
)
{
	public bool HasDiagnostics => !Diagnostics.IsEmpty;
}
