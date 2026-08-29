using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferForAttributeWithMetadataNameAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR11";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Prefer ForAttributeWithMetadataName over CreateSyntaxProvider",
		"Prefer SyntaxProvider.ForAttributeWithMetadataName for attribute-based detection; it is faster and more incremental-friendly than CreateSyntaxProvider",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Use IncrementalPipeline.ForAttributeWithMetadataName or SyntaxProvider.ForAttributeWithMetadataName when detecting symbols by attribute."
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;
		if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
			return;

		var name = memberAccess.Name.Identifier.Text;
		if (name != "CreateSyntaxProvider")
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
	}
}
