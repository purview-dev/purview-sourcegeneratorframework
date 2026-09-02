using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Test refactoring that adds a <c>[System.Obsolete]</c> attribute to the method the cursor is on.
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AddObsoleteRefactoringProvider))]
public sealed class AddObsoleteRefactoringProvider : CodeRefactoringProvider
{
	public const string EquivalenceKey = "AddObsolete";

	public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
		var method = root?.FindNode(context.Span).FirstAncestorOrSelf<MethodDeclarationSyntax>();
		if (method is null)
			return;

		context.RegisterRefactoring(
			CodeAction.Create(
				"Add [Obsolete]",
				cancellationToken => AddObsoleteAsync(context.Document, method, cancellationToken),
				EquivalenceKey
			)
		);
	}

	static async Task<Document> AddObsoleteAsync(
		Document document,
		MethodDeclarationSyntax method,
		CancellationToken cancellationToken
	)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken);
		var attribute = SyntaxFactory.AttributeList(
			SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.Obsolete")))
		);

		return document.WithSyntaxRoot(root!.ReplaceNode(method, method.AddAttributeLists(attribute)));
	}
}
