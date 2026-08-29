using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Provides diagnostic descriptors shared between the source generator and the analyzer for the
/// attribute-data model feature.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"MicrosoftCodeAnalysisReleaseTracking",
	"RS2008:Enable analyzer release tracking",
	Justification = "The descriptor is shared between the source generator and the analyzer project; release tracking is maintained in the consuming analyzer project."
)]
public static class AttributeDataModelDiagnosticRules
{
	/// <summary>
	/// Diagnostic raised when an attribute-data model property is declared with a non-cacheable
	/// <see cref="ISymbol"/> or <see cref="Type"/> type.
	/// </summary>
	public static readonly DiagnosticDescriptor SymbolPropertyNotCacheable = new(
		"ADM0010",
		"Attribute data model property type is not cacheable",
		"Property '{0}' type '{1}' is not cacheable for attribute data extraction. Use Purview.SourceGeneratorFramework.TypeIdentity or a string/string? type to capture type identity in a cacheable form.",
		"Property",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
}
