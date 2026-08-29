using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AttributeDataModelSymbolPropertyAnalyzer : DiagnosticAnalyzer
{
	public static readonly DiagnosticDescriptor Rule = new(
		"ADM0010",
		"Attribute data model property type is not cacheable",
		"Property '{0}' type '{1}' is not cacheable for attribute data extraction. Use Purview.SourceGeneratorFramework.TypeIdentity or a string/string? type to capture type identity in a cacheable form.",
		"Property",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(context =>
		{
			var symbolType = context.Compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.ISymbol");
			var systemType = context.Compilation.GetTypeByMetadataName("System.Type");
			var typeIdentityType = context.Compilation.GetTypeByMetadataName(
				"Purview.SourceGeneratorFramework.TypeIdentity"
			);

			context.RegisterSymbolAction(
				context => AnalyzeNamedType(context, symbolType, systemType, typeIdentityType),
				SymbolKind.NamedType
			);
		});
	}

	static void AnalyzeNamedType(
		SymbolAnalysisContext context,
		INamedTypeSymbol? symbolType,
		INamedTypeSymbol? systemType,
		INamedTypeSymbol? typeIdentityType
	)
	{
		if (context.Symbol is not INamedTypeSymbol typeSymbol)
			return;

		if (typeSymbol.TypeKind is not TypeKind.Struct and not TypeKind.Class)
			return;

		if (
			!typeSymbol
				.GetAttributes()
				.Any(a =>
					a.AttributeClass?.ToDisplayString()
					== "Purview.SourceGeneratorFramework.Generators.GenerateAttribute"
				)
		)
			return;

		foreach (var constructor in typeSymbol.InstanceConstructors)
		{
			foreach (var parameter in constructor.Parameters)
			{
				if (!parameter.Locations.Any(static loc => loc.IsInSource))
					continue;

				if (IsNonCacheableType(parameter.Type, symbolType, systemType, typeIdentityType))
				{
					context.ReportDiagnostic(
						Diagnostic.Create(
							Rule,
							parameter.Locations.FirstOrDefault(static loc => loc.IsInSource)
								?? typeSymbol.Locations.FirstOrDefault(),
							parameter.Name,
							parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
						)
					);
				}
			}
		}
	}

	static bool IsNonCacheableType(
		ITypeSymbol typeSymbol,
		INamedTypeSymbol? symbolType,
		INamedTypeSymbol? systemType,
		INamedTypeSymbol? typeIdentityType
	)
	{
		if (
			typeIdentityType is not null
			&& SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, typeIdentityType)
		)
			return false;

		if (typeSymbol.SpecialType == SpecialType.System_String)
			return false;

		if (systemType is not null && SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, systemType))
			return true;

		if (symbolType is not null)
		{
			if (SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, symbolType))
				return true;

			if (
				typeSymbol is INamedTypeSymbol namedType
				&& namedType.AllInterfaces.Contains(symbolType, SymbolEqualityComparer.Default)
			)
				return true;
		}

		return false;
	}
}
