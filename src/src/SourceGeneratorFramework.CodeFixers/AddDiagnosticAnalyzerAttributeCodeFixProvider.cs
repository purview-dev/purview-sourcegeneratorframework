using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.CodeFixers;

/// <summary>
/// Adds <c>[DiagnosticAnalyzer]</c> to a <c>DiagnosticAnalyzer</c> subclass so the compiler host
/// loads it (fixes <c>PSGFR25</c>).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddDiagnosticAnalyzerAttributeCodeFixProvider))]
public sealed class AddDiagnosticAnalyzerAttributeCodeFixProvider : CodeFixProvider
{
	internal const string EquivalenceKey = "AddDiagnosticAnalyzer";

	public override ImmutableArray<string> FixableDiagnosticIds => ["PSGFR25"];

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
					"Add [DiagnosticAnalyzer]",
					_ => AddDiagnosticAnalyzerAsync(context.Document, typeDeclaration, context.CancellationToken),
					EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static Task<Document> AddDiagnosticAnalyzerAsync(
		Document document,
		TypeDeclarationSyntax typeDeclaration,
		CancellationToken cancellationToken
	)
	{
		var attribute = RoslynComponentFixHelpers.CreateAttribute("DiagnosticAnalyzer", "LanguageNames.CSharp");

		return RoslynComponentFixHelpers.AddAttributeAsync(
			document,
			typeDeclaration,
			attribute,
			["Microsoft.CodeAnalysis"],
			cancellationToken
		);
	}
}
