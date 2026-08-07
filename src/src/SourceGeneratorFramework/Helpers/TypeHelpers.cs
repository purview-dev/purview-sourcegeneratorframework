using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Helpers;

/// <summary>
/// Provides helpers for type analysis and identifier generation during source generation.
/// </summary>
public static class TypeHelpers
{
	/// <summary>
	/// The suffix used to identify attribute types.
	/// </summary>
	public const string AttributeSuffix = nameof(Attribute);

	static readonly (string Keyword, SpecialType SpecialType)[] Map =
	[
		("bool", SpecialType.System_Boolean),
		("byte", SpecialType.System_Byte),
		("sbyte", SpecialType.System_SByte),
		("char", SpecialType.System_Char),
		("decimal", SpecialType.System_Decimal),
		("double", SpecialType.System_Double),
		("float", SpecialType.System_Single),
		("int", SpecialType.System_Int32),
		("uint", SpecialType.System_UInt32),
		("long", SpecialType.System_Int64),
		("ulong", SpecialType.System_UInt64),
		("short", SpecialType.System_Int16),
		("ushort", SpecialType.System_UInt16),
		("string", SpecialType.System_String),
		("object", SpecialType.System_Object),
		("void", SpecialType.System_Void),
		("nint", SpecialType.System_IntPtr),
		("nuint", SpecialType.System_UIntPtr),
	];

	static readonly ImmutableDictionary<string, SpecialType> KeywordToSpecialType =
		Map.ToImmutableDictionary(m => m.Keyword, m => m.SpecialType, StringComparer.Ordinal);

	static readonly ImmutableDictionary<SpecialType, string> SpecialTypeToKeyword =
		Map.ToImmutableDictionary(m => m.SpecialType, m => m.Keyword);

	/// <summary>
	/// Tries to map a C# keyword to its corresponding <see cref="SpecialType"/>.
	/// </summary>
	public static bool TryGetSpecialType(string keyword, out SpecialType specialType) =>
		KeywordToSpecialType.TryGetValue(keyword, out specialType);

	/// <summary>
	/// Tries to map a <see cref="SpecialType"/> to its corresponding C# keyword.
	/// </summary>
	public static bool TryGetKeyword(SpecialType specialType, out string? keyword) =>
		SpecialTypeToKeyword.TryGetValue(specialType, out keyword);

	/// <summary>
	/// Determines whether the specified type is a C# keyword type.
	/// </summary>
	public static bool IsKeywordType(ITypeSymbol type) =>
		type == null
			? throw new ArgumentNullException(nameof(type))
			: SpecialTypeToKeyword.ContainsKey(type.SpecialType);

	/// <summary>
	/// Determines whether the specified keyword is a recognized C# keyword type.
	/// </summary>
	public static bool IsKeywordType(string keyword) => KeywordToSpecialType.ContainsKey(keyword);

	/// <summary>
	/// Determines whether the supplied type name ends with the 'Attribute' suffix.
	/// </summary>
	public static bool IsAttribute(string typeName)
	{
		if (typeName == null)
			throw new ArgumentNullException(nameof(typeName));

		var idx = typeName.IndexOf('`');
		if (idx >= 0)
			typeName = typeName.Substring(0, idx);

		return typeName.Length > AttributeSuffix.Length
			&& typeName.EndsWith(AttributeSuffix, StringComparison.Ordinal);
	}

	/// <summary>
	/// Gets the type name without the 'Attribute' suffix, if present.
	/// </summary>
	public static string GetTypeName(string typeName)
	{
		if (typeName == null)
			throw new ArgumentNullException(nameof(typeName));

		var idx = typeName.IndexOf('`');
		if (idx >= 0)
			typeName = typeName.Substring(0, idx);

		if (IsAttribute(typeName))
			typeName = typeName.Substring(0, typeName.Length - AttributeSuffix.Length);

		return typeName;
	}

	/// <summary>
	/// Determines whether the target symbol has an explicit base type declaration.
	/// </summary>
	public static bool HasExplicitBaseType(TargetSymbolDescriptor descriptor)
	{
		if (descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));
		if (descriptor.Declaration == null)
			return false;

		// Check if the declaration has a base list with at least one type specified
		return descriptor.Declaration.BaseList is { Types.Count: > 0 };
	}

	/// <summary>
	/// Determines whether the target symbol is derived from the expected base type.
	/// </summary>
	public static bool IsDerivedFromExpectedBase(
		TargetSymbolDescriptor descriptor,
		TypeValueObject expectedBase
	)
	{
		if (descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));
		if (descriptor.Symbol.BaseType is not null)
		{
			TypeValueObject baseType = new(descriptor.Symbol.BaseType);
			if (baseType == expectedBase)
				return true;
		}

		var declaredBaseTypes = descriptor.Declaration?.BaseList?.Types;
		if (declaredBaseTypes is null)
			return false;

		foreach (var baseType in declaredBaseTypes)
		{
			if (
				string.Equals(
					GetUnqualifiedTypeName(baseType.Type),
					expectedBase.SymbolFullName,
					StringComparison.Ordinal
				)
			)
				return true;
		}

		return false;
	}

	/// <summary>
	/// Gets the unqualified type name from a <see cref="TypeSyntax"/>.
	/// </summary>
	public static string GetUnqualifiedTypeName(TypeSyntax typeSyntax) =>
		typeSyntax switch
		{
			IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
			GenericNameSyntax genericName => genericName.Identifier.ValueText,
			QualifiedNameSyntax qualifiedName => GetUnqualifiedTypeName(qualifiedName.Right),
			AliasQualifiedNameSyntax aliasQualifiedName => GetUnqualifiedTypeName(
				aliasQualifiedName.Name
			),
			NullableTypeSyntax nullableType => GetUnqualifiedTypeName(nullableType.ElementType),
			_ => typeSyntax.ToString(),
		};

	/// <summary>
	/// Determines whether the supplied name is a valid C# identifier.
	/// </summary>
	public static bool IsValidIdentifier(string? name)
	{
		if (string.IsNullOrEmpty(name))
			return false;
		if (!char.IsLetter(name![0]) && name[0] != '_')
			return false;
		for (var i = 1; i < name.Length; i++)
		{
			if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
				return false;
		}

		return true;
	}

	/// <summary>
	/// Creates a <see cref="TypeValueObject"/> for the embedded compiler attribute used by source generators.
	/// </summary>
	public static readonly TypeValueObject EmbeddedAttribute = new(
		nameof(EmbeddedAttribute),
		"Microsoft.CodeAnalysis"
	);

	/// <summary>
	/// Determines whether the type declaration is marked <see langword="partial"/>.
	/// </summary>
	public static bool IsPartial(TypeDeclarationSyntax declaration) =>
		declaration == null
			? throw new ArgumentNullException(nameof(declaration))
			: declaration.Modifiers.Any(SyntaxKind.PartialKeyword);

	/// <summary>
	/// Determines whether the type declaration has non-empty constructors.
	/// </summary>
	public static bool HasNonEmptyConstructors(TypeDeclarationSyntax declaration, string className)
	{
		if (declaration == null)
			throw new ArgumentNullException(nameof(declaration));

		if (declaration.ParameterList is { Parameters.Count: > 0 })
			return true;

		foreach (
			var constructor in declaration
				.Members.OfType<ConstructorDeclarationSyntax>()
				.Where(c =>
					string.Equals(c.Identifier.ValueText, className, StringComparison.Ordinal)
				)
		)
		{
			if (constructor.ParameterList.Parameters.Count > 0)
				return true;

			if (constructor.ExpressionBody is not null || constructor.Initializer is not null)
				return true;

			if (constructor.Body is not null && constructor.Body.Statements.Count > 0)
				return true;
		}

		return false;
	}

	static bool MatchesFullyQualifiedName(ISymbol symbol, string fullyQualifiedName)
	{
		if (
			string.Equals(
				symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				$"global::{fullyQualifiedName}",
				StringComparison.Ordinal
			)
		)
			return true;

		if (symbol is INamedTypeSymbol namedType)
		{
			var original = namedType.OriginalDefinition;
			var metadataName = original.MetadataName;
			var namespaceName = original.ContainingNamespace?.ToDisplayString();
			var originalFullName = string.IsNullOrEmpty(namespaceName)
				? metadataName
				: $"{namespaceName}.{metadataName}";

			if (string.Equals(originalFullName, fullyQualifiedName, StringComparison.Ordinal))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Determines whether the symbol has an attribute with the specified metadata name.
	/// </summary>
	public static bool HasAttribute(ISymbol symbol, string fullyQualifiedName)
	{
		if (symbol == null)
			throw new ArgumentNullException(nameof(symbol));
		if (string.IsNullOrWhiteSpace(fullyQualifiedName))
		{
			throw new ArgumentException(
				"Fully qualified name cannot be null or whitespace.",
				nameof(fullyQualifiedName)
			);
		}

		// All valid...
		return symbol
			.GetAttributes()
			.Any(attr =>
				attr.AttributeClass is not null
				&& MatchesFullyQualifiedName(attr.AttributeClass, fullyQualifiedName)
			);
	}

	/// <summary>
	/// Determines whether the symbol has the specified attribute.
	/// </summary>
	public static bool HasAttribute(ISymbol symbol, TypeValueObject attributeType) =>
		HasAttribute(symbol, attributeType.SymbolFullName);

	/// <summary>
	/// Determines whether the type inherits from the specified base type.
	/// </summary>
	public static bool InheritsFrom(ITypeSymbol typeSymbol, string fullyQualifiedName)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		var baseType = typeSymbol.BaseType;
		while (baseType is not null)
		{
			if (MatchesFullyQualifiedName(baseType, fullyQualifiedName))
				return true;

			baseType = baseType.BaseType;
		}

		return false;
	}

	/// <summary>
	/// Determines whether the type inherits from the specified base type.
	/// </summary>
	public static bool InheritsFrom(ITypeSymbol typeSymbol, TypeValueObject baseType) =>
		InheritsFrom(typeSymbol, baseType.SymbolFullName);

	/// <summary>
	/// Determines whether the type implements the specified interface.
	/// </summary>
	public static bool Implements(ITypeSymbol typeSymbol, string fullyQualifiedName) =>
		typeSymbol == null
			? throw new ArgumentNullException(nameof(typeSymbol))
			: typeSymbol.AllInterfaces.Any(i => MatchesFullyQualifiedName(i, fullyQualifiedName));

	/// <summary>
	/// Determines whether the type implements the specified interface.
	/// </summary>
	public static bool Implements(ITypeSymbol typeSymbol, TypeValueObject interfaceType) =>
		Implements(typeSymbol, interfaceType.SymbolFullName);

	/// <summary>
	/// Returns the fully qualified display string for a type symbol, optionally including nullable annotations.
	/// </summary>
	public static string ToFullyQualifiedDisplayString(
		ITypeSymbol typeSymbol,
		bool includeNullable = true
	)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		var format = includeNullable
			? SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
				SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
			)
			: SymbolDisplayFormat.FullyQualifiedFormat;

		return typeSymbol.ToDisplayString(format);
	}

	/// <summary>
	/// Returns the fully qualified display string for a symbol.
	/// </summary>
	public static string ToFullyQualifiedDisplayString(ISymbol symbol) =>
		symbol == null
			? throw new ArgumentNullException(nameof(symbol))
			: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

	const string GenericIEnumerableName = "global::System.Collections.Generic.IEnumerable<T>";

	/// <summary>
	/// Determines whether the type is a collection-like type (implements <see cref="System.Collections.IEnumerable"/> or <see cref="IEnumerable{T}"/>).
	/// </summary>
	public static bool IsCollectionLike(ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		if (typeSymbol.SpecialType == SpecialType.System_Collections_IEnumerable)
			return true;

		// Check if the type implements IEnumerable<T>
		return typeSymbol.AllInterfaces.Any(i =>
			string.Equals(
				i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				GenericIEnumerableName,
				StringComparison.Ordinal
			)
		);
	}

	/// <summary>
	/// Tries to get the element type of a collection-like type.
	/// </summary>
	public static bool TryGetElementType(ITypeSymbol typeSymbol, out ITypeSymbol? elementType)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		var enumerableInterface = typeSymbol.AllInterfaces.FirstOrDefault(i =>
			string.Equals(
				i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				GenericIEnumerableName,
				StringComparison.Ordinal
			)
		);

		if (enumerableInterface is not null && enumerableInterface.TypeArguments.Length > 0)
		{
			elementType = enumerableInterface.TypeArguments[0];
			return true;
		}

		elementType = null;
		return false;
	}

	/// <summary>
	/// Derives a name by removing the specified suffix from the type name.
	/// </summary>
	public static string DeriveName(string typeName, string suffix)
	{
		if (string.IsNullOrEmpty(typeName))
			throw new ArgumentException("Type name cannot be null or empty.", nameof(typeName));
		if (string.IsNullOrEmpty(suffix))
			return typeName;

		var idx = typeName.IndexOf('`');
		if (idx >= 0)
			typeName = typeName.Substring(0, idx);

		if (typeName.EndsWith(suffix, StringComparison.Ordinal) && typeName.Length > suffix.Length)
			return typeName.Substring(0, typeName.Length - suffix.Length);

		// If the suffix is not present, return the original type name
		return typeName;
	}

	/// <summary>
	/// Gets the C# accessibility keyword for the specified accessibility.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases")]
	public static string GetAccessibilityKeyword(Accessibility accessibility) =>
		accessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Internal => "internal",
			Accessibility.Protected => "protected",
			Accessibility.Private => "private",
			Accessibility.ProtectedOrInternal => "protected internal",
			Accessibility.ProtectedAndInternal => "private protected",
			_ => string.Empty,
		};

	/// <summary>
	/// Determines whether the symbol is accessible from public or internal scopes.
	/// </summary>
	public static bool IsAccessibleAsPublicOrInternal(ISymbol symbol) =>
		symbol == null
			? throw new ArgumentNullException(nameof(symbol))
			: symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
}
