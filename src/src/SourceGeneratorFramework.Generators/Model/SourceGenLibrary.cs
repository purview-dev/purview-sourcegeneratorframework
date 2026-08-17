using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Generators.Model;

static class SourceGenLibrary
{
	public static IncrementalValuesProvider<GeneratorResult<TypeValueObject>> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context
	)
	{
#pragma warning disable format
		return context
			.SyntaxProvider.CreateSyntaxProvider(
				predicate: static (node, _) =>
					node is ClassDeclarationSyntax { BaseList.Types.Count: > 0, Modifiers: var modifiers }
					&& modifiers.Any(SyntaxKind.PublicKeyword),
				transform: static (ctx, ct) => GetGeneratorClassInfo(ctx, ct)
			)
			.Where(static result => !result.IsEmpty)
			.WithTrackingName("GetSourceGenerators");
#pragma warning restore format
	}

	static GeneratorResult<TypeValueObject> GetGeneratorClassInfo(
		GeneratorSyntaxContext context,
		CancellationToken cancellationToken
	)
	{
		var classSyntax = (ClassDeclarationSyntax)context.Node;
		var symbol = context.SemanticModel.GetDeclaredSymbol(classSyntax, cancellationToken);

		if (symbol is not INamedTypeSymbol { IsAbstract: false, IsStatic: false } typeSymbol)
			return default;
		if (typeSymbol.DeclaredAccessibility is not Accessibility.Public)
			return default;

		var interfaces = typeSymbol.AllInterfaces;
		var isGenerator = interfaces.Any(static i =>
			GeneratorTypeLibrary.CodeAnalysis.IIncrementalGenerator.Equals(i)
			|| GeneratorTypeLibrary.CodeAnalysis.ISourceGenerator.Equals(i)
		);

		if (!isGenerator)
			return default;

		var hasLogSupport = interfaces.Any(static i =>
			GeneratorTypeLibrary.Logging.ISupportsSourceGenLogging.Equals(i)
		);
		if (hasLogSupport)
			return default;

		var hasPartial = classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
		if (!hasPartial)
		{
			return GeneratorResult<TypeValueObject>.Fail(
				DiagnosticInfo.Create(
					SourceGenDiagnosticDescriptors.SourceGeneratorMustBePartial,
					classSyntax.Identifier.GetLocation(),
					classSyntax.Identifier.Text
				)
			);
		}

		// If we reach this point, the class is a non-abstract, non-static class that implements
		// IIncrementalGenerator or ISourceGenerator but does not implement ILogSupport. We can generate the logging support for it.
		return GeneratorResult<TypeValueObject>.Ok(new(typeSymbol));
	}
}
