using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.ExampleGenerator.CodeFixers;

/// <summary>
/// A sample refactoring that adds a <c>[Debug]</c> attribute (from the logging sample's
/// <c>DebugAttribute</c>) to the method the cursor is on.
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(LoggingRefactoringProvider))]
public sealed class LoggingRefactoringProvider : CodeRefactoringProvider
{
	/// <summary>The equivalence key of the registered code action.</summary>
	public const string EquivalenceKey = "AddDebug";

	public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
		var method = root?.FindNode(context.Span).FirstAncestorOrSelf<MethodDeclarationSyntax>();
		if (method is null)
			return;

		context.RegisterRefactoring(
			CodeAction.Create(
				"Add [Debug]",
				cancellationToken => AddDebugAttributeAsync(context.Document, method, cancellationToken),
				EquivalenceKey
			)
		);
	}

	static async Task<Document> AddDebugAttributeAsync(
		Document document,
		MethodDeclarationSyntax method,
		CancellationToken cancellationToken
	)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken);
		var attribute = SyntaxFactory.AttributeList(
			SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("Debug")))
		);

		return document.WithSyntaxRoot(root!.ReplaceNode(method, method.AddAttributeLists(attribute)));
	}
}
