using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Adds <c>[ExportCodeFixProvider]</c> to a <c>CodeFixProvider</c> subclass so Visual Studio can
/// discover it (fixes <c>PSGFR24</c>).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddExportCodeFixProviderAttributeCodeFixProvider))]
public sealed class AddExportCodeFixProviderAttributeCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "AddExportCodeFixProvider";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR24"];

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
					"Add [ExportCodeFixProvider]",
					_ => AddExportCodeFixProviderAsync(context.Document, typeDeclaration, context.CancellationToken),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static Task<Document> AddExportCodeFixProviderAsync(
		Document document,
		TypeDeclarationSyntax typeDeclaration,
		CancellationToken cancellationToken
	)
	{
		var attribute = RoslynComponentFixHelpers.CreateAttribute("ExportCodeFixProvider", "LanguageNames.CSharp");

		return RoslynComponentFixHelpers.AddAttributeAsync(
			document,
			typeDeclaration,
			attribute,
			["Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CodeFixes"],
			cancellationToken
		);
	}
}
