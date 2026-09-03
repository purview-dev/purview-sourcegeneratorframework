using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

/// <summary>
/// Flags raw <c>CodeWriter</c> text emission that starts with a C# declaration keyword, suggesting a
/// structured declaration API such as <c>WriteClass</c> or <c>WriteProperty</c> instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferStructuredCodeWriterApiAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR18";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Prefer a structured CodeWriter declaration API",
		"'{0}' with a declaration should use a structured API such as WriteClass, WriteMethod, WriteProperty, or WriteField",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: "Emitting declaration syntax through raw text bypasses the structured, deterministic declaration APIs on CodeWriter."
	);

	static readonly string[] DeclarationStarts =
	[
		"public ",
		"internal ",
		"private ",
		"protected ",
		"file ",
		"static ",
		"sealed ",
		"abstract ",
		"partial ",
		"readonly ",
		"ref ",
		"required ",
		"const ",
		"class ",
		"struct ",
		"interface ",
		"enum ",
		"record ",
		"delegate ",
		"namespace ",
		"global using ",
		"using ",
	];

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
		if (
			context.Node
			is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation
		)
			return;

		var name = member.Name.Identifier.Text;
		if (name is not ("Write" or "WriteLine" or "Append" or "AppendLine" or "MultiLine"))
			return;

		if (invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is not LiteralExpressionSyntax literal)
			return;

		if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
			return;

		if (
			context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
			is not IMethodSymbol method
		)
			return;

		if (method.ContainingType?.ToDisplayString() != "Purview.SourceGeneratorFramework.CodeWriter")
			return;

		var value = literal.Token.ValueText.TrimStart();
		if (StartsWithDeclaration(value))
			context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), name));
	}

	static bool StartsWithDeclaration(string value)
	{
		foreach (var prefix in DeclarationStarts)
		{
			if (!value.StartsWith(prefix, StringComparison.Ordinal))
				continue;

			// "using (var x = ...)" is a using statement inside a body, not a directive; don't flag it.
			if (prefix == "using " && value.StartsWith("using (", StringComparison.Ordinal))
				return false;

			// "namespace global::" is a valid namespace declaration, but "global::" is not a declaration keyword.
			return true;
		}

		return false;
	}
}
