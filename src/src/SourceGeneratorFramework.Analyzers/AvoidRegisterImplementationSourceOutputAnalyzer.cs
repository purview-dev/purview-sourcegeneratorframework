using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidRegisterImplementationSourceOutputAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR14";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Avoid RegisterImplementationSourceOutput",
		"Avoid RegisterImplementationSourceOutput unless the generator explicitly produces implementation-only sources; prefer RegisterSourceOutput",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "RegisterImplementationSourceOutput bypasses public API surface checks. Use RegisterSourceOutput unless you specifically need implementation-only output."
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

		if (memberAccess.Name.Identifier.Text != "RegisterImplementationSourceOutput")
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
	}
}
