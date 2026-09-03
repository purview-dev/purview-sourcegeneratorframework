using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags <c>CodeWriter</c> scope-returning methods whose returned scope is discarded, which skips the
/// block's closing token and can unbalance indentation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DiscardedCodeWriterScopeAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR17";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Consume CodeWriter scopes with a using statement",
		"The scope returned by {0} is discarded; assign it to a using statement so the closing token is written",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Scope-returning CodeWriter methods must be consumed by a using statement so the generated block is closed correctly."
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ExpressionStatement);
	}

	static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation })
			return;

		if (
			context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
			is not IMethodSymbol method
		)
			return;

		if (method.ContainingType?.ToDisplayString() != "Purview.SourceGeneratorFramework.CodeWriter")
			return;

		var returnType = method.ReturnType.ToDisplayString();
		if (
			returnType
			is not (
				"Purview.SourceGeneratorFramework.CodeWriter.BlockScope"
				or "Purview.SourceGeneratorFramework.CodeWriter.IndentScope"
			)
		)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), method.Name));
	}
}
