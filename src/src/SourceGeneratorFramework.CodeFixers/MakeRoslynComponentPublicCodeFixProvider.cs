using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Makes a Roslyn component type <c>public</c> so the compiler host can instantiate it
/// (fixes <c>PSGFR27</c>).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeRoslynComponentPublicCodeFixProvider))]
public sealed class MakeRoslynComponentPublicCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "MakePublic";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR27"];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		foreach (var diagnostic in context.Diagnostics)
		{
			var node = root.FindNode(diagnostic.Location.SourceSpan);
			if (node.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { } typeDeclaration)
				continue;

			context.RegisterCodeFix(
				CodeAction.Create(
					"Make public",
					_ => MakePublicAsync(context.Document, typeDeclaration, context.CancellationToken),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static async Task<Document> MakePublicAsync(
		Document document,
		TypeDeclarationSyntax typeDeclaration,
		CancellationToken cancellationToken
	)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		var updated = RoslynComponentFixHelpers.MakePublic(typeDeclaration);

		return document.WithSyntaxRoot(root.ReplaceNode(typeDeclaration, updated));
	}
}
