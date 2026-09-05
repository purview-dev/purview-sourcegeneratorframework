using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags Roslyn component types (source generators, diagnostic analyzers, code fix providers) that
/// are not effectively public, because the compiler host can only instantiate public types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonPublicRoslynComponentAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR27";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Roslyn component type must be public",
		"Type '{0}' is a {1} but is not public; Roslyn cannot instantiate non-public components, so it will never run",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "The Roslyn compiler host can only instantiate public source generator, diagnostic analyzer, and code fix provider types. Non-public component types are silently ignored."
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
			var diagnosticAnalyzerType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer"
			);
			var incrementalGeneratorType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.IIncrementalGenerator"
			);
			var legacyGeneratorType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.ISourceGenerator"
			);
			var exportAttributeType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.CodeFixes.ExportCodeFixProviderAttribute"
			);
			var diagnosticAnalyzerAttributeType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerAttribute"
			);
			var generatorAttributeType = context.Compilation.GetTypeByMetadataName(
				"Microsoft.CodeAnalysis.GeneratorAttribute"
			);

			context.RegisterSymbolAction(
				context =>
					AnalyzeNamedType(
						context,
						codeFixProviderType,
						diagnosticAnalyzerType,
						incrementalGeneratorType,
						legacyGeneratorType,
						exportAttributeType,
						diagnosticAnalyzerAttributeType,
						generatorAttributeType
					),
				SymbolKind.NamedType
			);
		});
	}

	static void AnalyzeNamedType(
		SymbolAnalysisContext context,
		INamedTypeSymbol? codeFixProviderType,
		INamedTypeSymbol? diagnosticAnalyzerType,
		INamedTypeSymbol? incrementalGeneratorType,
		INamedTypeSymbol? legacyGeneratorType,
		INamedTypeSymbol? exportAttributeType,
		INamedTypeSymbol? diagnosticAnalyzerAttributeType,
		INamedTypeSymbol? generatorAttributeType
	)
	{
		if (context.Symbol is not INamedTypeSymbol type)
			return;

		if (
			!RoslynComponentDiscovery.IsRoslynComponent(
				type,
				codeFixProviderType,
				diagnosticAnalyzerType,
				incrementalGeneratorType,
				legacyGeneratorType,
				exportAttributeType,
				diagnosticAnalyzerAttributeType,
				generatorAttributeType
			)
		)
			return;

		if (RoslynComponentDiscovery.IsEffectivelyPublic(type))
			return;

		var kind = RoslynComponentDiscovery.DescribeKind(
			type,
			codeFixProviderType,
			diagnosticAnalyzerType,
			incrementalGeneratorType,
			legacyGeneratorType,
			exportAttributeType,
			generatorAttributeType
		);

		context.ReportDiagnostic(
			Diagnostic.Create(
				Rule,
				type.Locations.FirstOrDefault(static loc => loc.IsInSource) ?? Location.None,
				type.Name,
				kind
			)
		);
	}
}
