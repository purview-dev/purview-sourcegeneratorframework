using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseIncrementalGeneratorAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR12";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Use IIncrementalGenerator instead of ISourceGenerator",
		"Source generators should implement IIncrementalGenerator and use RegisterSourceOutput for incremental, cache-friendly generation",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "ISourceGenerator is not incremental. Implement IIncrementalGenerator and use RegisterSourceOutput."
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration);
	}

	static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.Node;
		if (classDeclaration.BaseList is null)
			return;

		foreach (var baseType in classDeclaration.BaseList.Types)
		{
			if (baseType.Type is not IdentifierNameSyntax identifierName)
				continue;

			if (identifierName.Identifier.Text != "ISourceGenerator")
				continue;

			context.ReportDiagnostic(Diagnostic.Create(Rule, identifierName.GetLocation()));
		}
	}
}
