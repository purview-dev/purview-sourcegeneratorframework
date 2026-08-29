using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PipelineModelReferenceEqualityCollectionAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSGFR15";

	public static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Pipeline model collection lacks sequence equality",
		"Pipeline model member '{0}' uses '{1}', which does not provide sequence equality for incremental caching. Use EquatableArray<T> or an equivalent value-equatable collection instead.",
		"Purview.SourceGeneratorFramework",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Incremental source generator pipeline models should use value-equatable collections for members so that value equality compares contents rather than references.",
		customTags: WellKnownDiagnosticTags.CompilationEnd
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationAction(static context =>
		{
			var compilation = context.Compilation;
			var pipelineTypes = ResolvePipelineTypes(compilation);
			var collectionTypes = ResolveCollectionTypes(compilation);
			var referenceEqualityCollectionTypes = ResolveReferenceEqualityCollectionTypes(compilation);

			if (pipelineTypes.IsEmpty || referenceEqualityCollectionTypes.IsEmpty)
				return;

			var modelTypes = CollectPipelineModelTypes(compilation, pipelineTypes, collectionTypes);

			foreach (var modelType in modelTypes)
			{
				AnalyzeModelType(context, modelType, referenceEqualityCollectionTypes);
			}
		});
	}

	static ImmutableArray<INamedTypeSymbol> ResolvePipelineTypes(Compilation compilation)
	{
		var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
		AddIfNotNull(builder, compilation.GetTypeByMetadataName("Purview.SourceGeneratorFramework.GeneratorResult`1"));
		AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.IncrementalValuesProvider`1"));
		AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.IncrementalValueProvider`1"));
		return builder.ToImmutable();
	}

	static ImmutableArray<INamedTypeSymbol> ResolveCollectionTypes(Compilation compilation)
	{
		var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
		AddIfNotNull(builder, compilation.GetTypeByMetadataName("Purview.SourceGeneratorFramework.EquatableArray`1"));
		AddIfNotNull(builder, compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1"));
		return builder.ToImmutable();
	}

	static ImmutableArray<INamedTypeSymbol> ResolveReferenceEqualityCollectionTypes(Compilation compilation)
	{
		var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
		AddIfNotNull(builder, compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1"));
		AddIfNotNull(builder, compilation.GetTypeByMetadataName("System.Collections.Generic.List`1"));
		return builder.ToImmutable();
	}

	static void AddIfNotNull(ImmutableArray<INamedTypeSymbol>.Builder builder, INamedTypeSymbol? type)
	{
		if (type is not null)
			builder.Add(type);
	}

	static HashSet<INamedTypeSymbol> CollectPipelineModelTypes(
		Compilation compilation,
		ImmutableArray<INamedTypeSymbol> pipelineTypes,
		ImmutableArray<INamedTypeSymbol> collectionTypes
	)
	{
		var directModels = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

		foreach (var typeSymbol in GetAllTypes(compilation.GlobalNamespace))
		{
			foreach (var member in typeSymbol.GetMembers())
			{
				var memberType = GetMemberType(member);
				if (memberType is null)
					continue;

				CollectPipelineTypeArguments(memberType, pipelineTypes, directModels);
			}
		}

		var expanded = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
		var worklist = new Queue<INamedTypeSymbol>(directModels);

		while (worklist.Count > 0)
		{
			var modelType = worklist.Dequeue();
			if (!ShouldExpand(modelType) || !expanded.Add(modelType))
				continue;

			foreach (var member in modelType.GetMembers())
			{
				var memberType = GetMemberType(member);
				if (memberType is not INamedTypeSymbol namedMemberType)
					continue;

				if (namedMemberType.IsGenericType)
				{
					var originalDefinition = namedMemberType.OriginalDefinition;
					if (collectionTypes.Any(c => SymbolEqualityComparer.Default.Equals(originalDefinition, c)))
					{
						foreach (var typeArgument in namedMemberType.TypeArguments)
						{
							if (typeArgument is INamedTypeSymbol namedTypeArgument && ShouldExpand(namedTypeArgument))
								worklist.Enqueue(namedTypeArgument);
						}

						continue;
					}
				}

				if (ShouldExpand(namedMemberType))
					worklist.Enqueue(namedMemberType);
			}
		}

		return expanded;
	}

	static bool ShouldExpand(INamedTypeSymbol typeSymbol) => typeSymbol.Locations.Any(static loc => loc.IsInSource);

	static void CollectPipelineTypeArguments(
		ITypeSymbol typeSymbol,
		ImmutableArray<INamedTypeSymbol> pipelineTypes,
		HashSet<INamedTypeSymbol> modelTypes
	)
	{
		if (typeSymbol is not INamedTypeSymbol namedType)
			return;

		if (namedType.IsGenericType)
		{
			var originalDefinition = namedType.OriginalDefinition;
			foreach (var pipelineType in pipelineTypes)
			{
				if (SymbolEqualityComparer.Default.Equals(originalDefinition, pipelineType))
				{
					foreach (var typeArgument in namedType.TypeArguments)
					{
						if (
							typeArgument is INamedTypeSymbol namedTypeArgument
							&& namedTypeArgument.Locations.Any(static loc => loc.IsInSource)
						)
							modelTypes.Add(namedTypeArgument);
					}
				}
			}

			foreach (var typeArgument in namedType.TypeArguments)
			{
				CollectPipelineTypeArguments(typeArgument, pipelineTypes, modelTypes);
			}
		}
	}

	static void AnalyzeModelType(
		CompilationAnalysisContext context,
		INamedTypeSymbol modelType,
		ImmutableArray<INamedTypeSymbol> referenceEqualityCollectionTypes
	)
	{
		foreach (var member in modelType.GetMembers())
		{
			if (member.IsImplicitlyDeclared)
				continue;

			var memberType = member switch
			{
				IFieldSymbol field => field.Type,
				IPropertySymbol property => property.Type,
				_ => null,
			};

			if (memberType is null)
				continue;

			if (
				memberType is IArrayTypeSymbol
				|| IsReferenceEqualityCollectionType(memberType, referenceEqualityCollectionTypes)
			)
			{
				ReportDiagnostic(context, member, memberType);
			}
		}
	}

	static bool IsReferenceEqualityCollectionType(
		ITypeSymbol typeSymbol,
		ImmutableArray<INamedTypeSymbol> referenceEqualityCollectionTypes
	)
	{
		if (typeSymbol is not INamedTypeSymbol namedType || !namedType.IsGenericType)
			return false;

		var originalDefinition = namedType.OriginalDefinition;
		foreach (var collectionType in referenceEqualityCollectionTypes)
		{
			if (SymbolEqualityComparer.Default.Equals(originalDefinition, collectionType))
				return true;
		}

		return false;
	}

	static void ReportDiagnostic(CompilationAnalysisContext context, ISymbol member, ITypeSymbol memberType)
	{
		var location = GetMemberLocation(member);
		if (location is null)
			return;

		context.ReportDiagnostic(
			Diagnostic.Create(
				Rule,
				location,
				member.Name,
				memberType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
			)
		);
	}

	static Location? GetMemberLocation(ISymbol member)
	{
		return member.Locations.FirstOrDefault(static loc => loc.IsInSource);
	}

	static ITypeSymbol? GetMemberType(ISymbol member)
	{
		return member switch
		{
			IFieldSymbol field => field.Type,
			IPropertySymbol property => property.Type,
			IParameterSymbol parameter => parameter.Type,
			IMethodSymbol method when method.AssociatedSymbol is null => method.ReturnType,
			IEventSymbol eventSymbol => eventSymbol.Type,
			_ => null,
		};
	}

	static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
	{
		foreach (var member in namespaceSymbol.GetMembers())
		{
			if (member is INamespaceSymbol nestedNamespace)
			{
				foreach (var type in GetAllTypes(nestedNamespace))
					yield return type;
			}
			else if (member is INamedTypeSymbol type)
			{
				yield return type;
				foreach (var nested in GetAllTypes(type))
					yield return nested;
			}
		}
	}

	static IEnumerable<INamedTypeSymbol> GetAllTypes(INamedTypeSymbol typeSymbol)
	{
		foreach (var nested in typeSymbol.GetTypeMembers())
		{
			yield return nested;
			foreach (var deeper in GetAllTypes(nested))
				yield return deeper;
		}
	}
}
