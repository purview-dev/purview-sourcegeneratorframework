using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Adds <c>[Generator]</c> to a type implementing a generator interface so it actually runs
/// (fixes <c>PSGFR26</c>).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddGeneratorAttributeCodeFixProvider))]
public sealed class AddGeneratorAttributeCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "AddGenerator";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR26"];

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
					"Add [Generator]",
					_ => AddGeneratorAsync(context.Document, typeDeclaration, context.CancellationToken),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static Task<Document> AddGeneratorAsync(
		Document document,
		TypeDeclarationSyntax typeDeclaration,
		CancellationToken cancellationToken
	)
	{
		var attribute = RoslynComponentFixHelpers.CreateAttribute("Generator");

		return RoslynComponentFixHelpers.AddAttributeAsync(
			document,
			typeDeclaration,
			attribute,
			["Microsoft.CodeAnalysis"],
			cancellationToken
		);
	}
}
