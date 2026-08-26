using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GenerationCapabilitiesMustBeRecordAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGF001";

	public static readonly DiagnosticDescriptor Rule = new(
		id: DiagnosticId,
		title: "Generation capabilities must be a record",
		messageFormat: "Capabilities type '{0}' must be declared as a record",
		category: "Purview.SourceGeneratorFramework",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(static context =>
		{
			var generationContextType = context.Compilation.GetTypeByMetadataName(typeof(GenerationContext<>).FullName);
			if (generationContextType is null)
				return;

			context.RegisterSyntaxNodeAction(
				context => AnalyzeGenericName(context, generationContextType),
				SyntaxKind.GenericName
			);
		});
	}

	static void AnalyzeGenericName(SyntaxNodeAnalysisContext context, INamedTypeSymbol generationContextType)
	{
		var genericName = (GenericNameSyntax)context.Node;
		var typeInfo = context.SemanticModel.GetTypeInfo(genericName);
		if (typeInfo.Type is not INamedTypeSymbol constructedType)
			return;

		if (!SymbolEqualityComparer.Default.Equals(constructedType.OriginalDefinition, generationContextType))
			return;

		if (constructedType.TypeArguments[0] is not INamedTypeSymbol capabilitiesType)
			return;

		if (capabilitiesType.IsRecord)
			return;

		context.ReportDiagnostic(
			Diagnostic.Create(
				Rule,
				genericName.TypeArgumentList.Arguments[0].GetLocation(),
				capabilitiesType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
			)
		);
	}
}
