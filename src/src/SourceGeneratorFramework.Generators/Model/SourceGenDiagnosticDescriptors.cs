using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Generators.Model;

static class SourceGenDiagnosticDescriptors
{
	public static readonly DiagnosticDescriptor SourceGeneratorMustBePartial = new(
		"PSG0001",
		"Source generator must be partial",
		"Class '{0}' implements a source generator interface but is not marked partial. Logging support cannot be generated.",
		"LoggingSupport",
		DiagnosticSeverity.Warning,
		true
	);
}
