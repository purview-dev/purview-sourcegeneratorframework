using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Fixes a bare <c>Nullable()</c>/<c>MakeNullable()</c> call by passing the first in-scope
/// <c>CodeWriter</c> or <c>GenerationSettings</c>, so the annotation is emitted only when the target
/// compilation supports nullable.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferNullableContextOverloadCodeFixProvider))]
public sealed class PreferNullableContextOverloadCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "PassNullableContext";

	const string CodeWriterTypeName = "Purview.SourceGeneratorFramework.CodeWriter";
	const string GenerationSettingsTypeName = "Purview.SourceGeneratorFramework.GenerationSettings";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR16"];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		var semanticModel = await context
			.Document.GetSemanticModelAsync(context.CancellationToken)
			.ConfigureAwait(false);
		if (root is null || semanticModel is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan);
			if (node is not InvocationExpressionSyntax invocation)
				continue;

			if (!TryFindNullableContext(semanticModel, invocation, out var argumentName, context.CancellationToken))
				continue;

			var methodName = ((MemberAccessExpressionSyntax)invocation.Expression).Name.Identifier.Text;

			context.RegisterCodeFix(
				CodeAction.Create(
					$"Pass '{argumentName}' to {methodName}()",
					_ => AddArgumentAsync(context.Document, invocation, argumentName),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static async Task<Document> AddArgumentAsync(
		Document document,
		InvocationExpressionSyntax invocation,
		string argumentName
	)
	{
		var root = (await document.GetSyntaxRootAsync().ConfigureAwait(false))!;
		var newArgumentList = SyntaxFactory
			.ArgumentList(
				SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(argumentName)))
			)
			.WithTriviaFrom(invocation.ArgumentList);

		return document.WithSyntaxRoot(root.ReplaceNode(invocation.ArgumentList, newArgumentList));
	}

	static bool TryFindNullableContext(
		SemanticModel semanticModel,
		SyntaxNode invocation,
		out string argumentName,
		CancellationToken cancellationToken
	)
	{
		argumentName = string.Empty;

		// Parameters of the enclosing method are always in scope.
		if (semanticModel.GetEnclosingSymbol(invocation.SpanStart, cancellationToken) is IMethodSymbol method)
		{
			foreach (var parameter in method.Parameters)
			{
				if (IsNullableContextType(parameter.Type))
				{
					argumentName = parameter.Name;
					return true;
				}
			}
		}

		// Walk the enclosing blocks from the invocation outward, collecting variables declared before it.
		for (
			var block = invocation.FirstAncestorOrSelf<BlockSyntax>();
			block is not null;
			block = block.Parent?.FirstAncestorOrSelf<BlockSyntax>()
		)
		{
			foreach (var variable in block.DescendantNodes().OfType<VariableDeclaratorSyntax>())
			{
				if (variable.Span.End >= invocation.SpanStart)
					continue;

				if (
					semanticModel.GetDeclaredSymbol(variable, cancellationToken) is ILocalSymbol local
					&& IsNullableContextType(local.Type)
				)
				{
					argumentName = local.Name;
					return true;
				}
			}

			if (block.Parent is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax)
				break;
		}

		return false;
	}

	static bool IsNullableContextType(ITypeSymbol? type)
	{
		var name = type?.ToDisplayString();

		return name is CodeWriterTypeName or GenerationSettingsTypeName;
	}
}
