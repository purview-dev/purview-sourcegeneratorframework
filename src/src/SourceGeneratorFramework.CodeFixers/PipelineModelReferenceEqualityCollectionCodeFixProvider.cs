using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PipelineModelReferenceEqualityCollectionCodeFixProvider))]
public sealed class PipelineModelReferenceEqualityCollectionCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "UseEquatableArray";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR15"];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		var semanticModel = await context
			.Document.GetSemanticModelAsync(context.CancellationToken)
			.ConfigureAwait(false);
		if (semanticModel is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan);
			if (!AttributeDataModelSymbolPropertyCodeFixProvider.TryGetTypeSyntax(node, out var typeSyntax, out _))
				continue;

			var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
			if (typeInfo.Type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
				continue;

			var originalDefinition = namedType.OriginalDefinition;
			var immutableArrayType = semanticModel.Compilation.GetTypeByMetadataName(
				"System.Collections.Immutable.ImmutableArray`1"
			);
			if (immutableArrayType is null)
				continue;

			if (!SymbolEqualityComparer.Default.Equals(originalDefinition, immutableArrayType))
				continue;

			var elementType = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

			context.RegisterCodeFix(
				CodeAction.Create(
					"Use EquatableArray<T>",
					_ => ReplaceTypeAsync(context.Document, root, typeSyntax, elementType),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static Task<Document> ReplaceTypeAsync(
		Document document,
		SyntaxNode root,
		TypeSyntax typeSyntax,
		string elementType
	)
	{
		var newType = SyntaxFactory
			.ParseTypeName($"global::Purview.SourceGeneratorFramework.EquatableArray<{elementType}>")
			.WithTriviaFrom(typeSyntax)
			.WithLeadingTrivia(typeSyntax.GetLeadingTrivia())
			.WithTrailingTrivia(typeSyntax.GetTrailingTrivia());

		var newRoot = root.ReplaceNode(typeSyntax, newType);
		return Task.FromResult(document.WithSyntaxRoot(newRoot));
	}
}
