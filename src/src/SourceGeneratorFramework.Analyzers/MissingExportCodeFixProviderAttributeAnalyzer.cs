using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags <c>CodeFixProvider</c> subclasses that are not decorated with
/// <c>[ExportCodeFixProvider]</c>, so Visual Studio can never discover their fixes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingExportCodeFixProviderAttributeAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR24";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"CodeFixProvider is not exported",
		"Type '{0}' derives from CodeFixProvider but is not marked [ExportCodeFixProvider]; Visual Studio will never discover its code fixes",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Code fix providers must be decorated with [ExportCodeFixProvider] so Visual Studio can discover them. Without the attribute the type is silently ignored and its fixes never appear."
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(context =>
		{
			var codeFixProviderType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider"
			);
			var exportAttributeType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.CodeFixes.ExportCodeFixProviderAttribute"
			);

			context.RegisterSymbolAction(
				context => AnalyzeNamedType(context, codeFixProviderType, exportAttributeType),
				SymbolKind.NamedType
			);
		});
	}

	static void AnalyzeNamedType(
		SymbolAnalysisContext context,
		INamedTypeSymbol? codeFixProviderType,
		INamedTypeSymbol? exportAttributeType
	)
	{
		if (context.Symbol is not INamedTypeSymbol type)
			return;

		if (!RoslynComponentDiscovery.IsCodeFixProvider(type, codeFixProviderType))
			return;

		if (RoslynComponentDiscovery.HasAttribute(type, exportAttributeType))
			return;

		context.ReportDiagnostic(
			Diagnostic.Create(
				Rule,
				type.Locations.FirstOrDefault(static loc => loc.IsInSource) ?? Location.None,
				type.Name
			)
		);
	}
}
