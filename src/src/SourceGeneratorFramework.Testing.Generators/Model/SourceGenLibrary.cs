using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Testing.Abstractions;

namespace Purview.SourceGeneratorFramework.Testing.Generators.Model;

static class SourceGenLibrary
{
	public static IncrementalValueProvider<GenerationModel> GetGeneratorValueProviders(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		var isDisabled = IncrementalPipeline.IsDisabledValueProvider(
			context,
			PropertyLibrary.DisableSourceGenerator
		);
		var generationContext = IncrementalPipeline.DefaultGenerationContextValueProvider(
			context,
			logger
		);
		var sourceGenerators = context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (node, _) => node is ClassDeclarationSyntax,
			transform: static (ctx, ct) => GetGeneratorClassInfo(ctx, ct)
		);

		return isDisabled
			.CombineWith(
				generationContext,
				static (disabled, generationContext, _) =>
					new GenerationModel(disabled, generationContext),
				"CreateGenerationModel"
			)
			.CollectWith(
				sourceGenerators,
				(inputs, sourceGenerators, _) =>
				{
					inputs.SourceGenerators = [.. sourceGenerators.Where(m => m != default)];
					return inputs;
				},
				"GetSourceGenerators"
			);
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
			TypeLibrary.IIncrementalGenerator.Equals(i) || TypeLibrary.ISourceGenerator.Equals(i)
		);

		if (!isGenerator)
			return default;

		var hasLogSupport = interfaces.Any(static i => TypeLibrary.ILogSupport.Equals(i));
		if (hasLogSupport)
			return default;

		var hasPartial = classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
		if (!hasPartial)
		{
			return GeneratorResult<TypeValueObject>.Fail(
				DiagnosticInfo.Create(
					new DiagnosticDescriptor(
						"PSG0001",
						"Source generator must be partial",
						"Class '{0}' implements a source generator interface but is not marked partial. Logging support cannot be generated.",
						"Purview.SourceGenerators.Testing",
						DiagnosticSeverity.Warning,
						true
					),
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
