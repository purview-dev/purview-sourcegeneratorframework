using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags <c>DiagnosticAnalyzer</c> subclasses that are not decorated with
/// <c>[DiagnosticAnalyzer]</c>, so the analyzer is never loaded and its diagnostics (and their
/// code fixes) never appear.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingDiagnosticAnalyzerAttributeAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR25";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"DiagnosticAnalyzer is not registered",
		"Type '{0}' derives from DiagnosticAnalyzer but is not marked [DiagnosticAnalyzer]; the analyzer will never run",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Diagnostic analyzers must be decorated with [DiagnosticAnalyzer] so the compiler host loads them. Without the attribute the analyzer is silently ignored."
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
			var diagnosticAnalyzerType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer"
			);
			var diagnosticAnalyzerAttributeType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerAttribute"
			);

			context.RegisterSymbolAction(
				context => AnalyzeNamedType(context, diagnosticAnalyzerType, diagnosticAnalyzerAttributeType),
				SymbolKind.NamedType
			);
		});
	}

	static void AnalyzeNamedType(
		SymbolAnalysisContext context,
		INamedTypeSymbol? diagnosticAnalyzerType,
		INamedTypeSymbol? diagnosticAnalyzerAttributeType
	)
	{
		if (context.Symbol is not INamedTypeSymbol type)
			return;

		if (!RoslynComponentDiscovery.IsDiagnosticAnalyzer(type, diagnosticAnalyzerType))
			return;

		if (RoslynComponentDiscovery.HasAttribute(type, diagnosticAnalyzerAttributeType))
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
