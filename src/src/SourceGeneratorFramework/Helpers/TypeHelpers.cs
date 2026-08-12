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

	static readonly (Type Type, SpecialType SpecialType)[] TypeMap =
	[
		(typeof(bool), SpecialType.System_Boolean),
		(typeof(byte), SpecialType.System_Byte),
		(typeof(sbyte), SpecialType.System_SByte),
		(typeof(char), SpecialType.System_Char),
		(typeof(decimal), SpecialType.System_Decimal),
		(typeof(double), SpecialType.System_Double),
		(typeof(float), SpecialType.System_Single),
		(typeof(int), SpecialType.System_Int32),
		(typeof(uint), SpecialType.System_UInt32),
		(typeof(long), SpecialType.System_Int64),
		(typeof(ulong), SpecialType.System_UInt64),
		(typeof(short), SpecialType.System_Int16),
		(typeof(ushort), SpecialType.System_UInt16),
		(typeof(string), SpecialType.System_String),
		(typeof(object), SpecialType.System_Object),
		(typeof(void), SpecialType.System_Void),
		(typeof(nint), SpecialType.System_IntPtr),
		(typeof(nuint), SpecialType.System_UIntPtr),
	];

	static readonly ImmutableDictionary<string, SpecialType> KeywordToSpecialType =
		Map.ToImmutableDictionary(m => m.Keyword, m => m.SpecialType, StringComparer.Ordinal);

	static readonly ImmutableDictionary<SpecialType, string> SpecialTypeToKeyword =
		Map.ToImmutableDictionary(m => m.SpecialType, m => m.Keyword);

	static readonly ImmutableDictionary<Type, SpecialType> SpecialTypeToType =
		TypeMap.ToImmutableDictionary(m => m.Type, m => m.SpecialType);

	/// <summary>
	/// Tries to map a C# keyword to its corresponding <see cref="SpecialType"/>.
	/// </summary>
	public static bool TryGetSpecialType(string keyword, out SpecialType specialType) =>
		KeywordToSpecialType.TryGetValue(keyword, out specialType);

	/// <summary>
	/// Tries to map a <see cref="Type"/> to its corresponding <see cref="SpecialType"/>.
	/// </summary>
	public static bool TryGetSpecialType(Type type, out SpecialType specialType) =>
		SpecialTypeToType.TryGetValue(type, out specialType);

	/// <summary>
	/// Tries to map a <see cref="Type"/> to its corresponding C# keyword.
	/// </summary>
	public static bool TryGetKeyword(Type type, out string? keyword)
	{
		keyword = null;
		return SpecialTypeToType.TryGetValue(type, out var specialType)
			&& SpecialTypeToKeyword.TryGetValue(specialType, out keyword);
	}

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
			if (IsCompatibleExpectedBase(descriptor.Symbol.BaseType, expectedBase))
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

	static bool IsCompatibleExpectedBase(INamedTypeSymbol actualBase, TypeValueObject expectedBase)
	{
		if (expectedBase.Equals(actualBase))
			return true;

		var actualDefinition = actualBase.OriginalDefinition;
		var actualNamespace = actualDefinition.ContainingNamespace.IsGlobalNamespace
			? null
			: actualDefinition.ContainingNamespace.ToDisplayString();
		if (
			actualDefinition.Name != expectedBase.TypeName
			|| actualNamespace != expectedBase.Namespace
		)
			return false;

		// A name-only TypeValueObject has no generic shape information. Treat it as the generic
		// definition identified by that name, allowing callers such as TypeLibrary.ResourceKitBase
		// to validate any constructed ResourceKitBase<T>.
		if (expectedBase.GenericArity == 0 && expectedBase.TypeArguments.IsDefaultOrEmpty)
			return true;

		if (
			actualDefinition.Arity != expectedBase.GenericArity
			|| expectedBase.TypeArguments.IsDefaultOrEmpty
			|| actualBase.TypeArguments.Length != expectedBase.TypeArguments.Length
		)
			return false;

		// This helper validates a generator contract rather than CLR generic assignability. For
		// example, ResourceKitBase<ConcreteResource> satisfies ResourceKitBase<IResourceKit> when
		// ConcreteResource implements IResourceKit, despite constructed generic invariance.
		for (var index = 0; index < expectedBase.TypeArguments.Length; index++)
		{
			var expectedArgument = expectedBase.TypeArguments[index];
			var actualArgument = actualBase.TypeArguments[index];
			if (
				!expectedArgument.Equals(actualArgument)
				&& !MatchesFullyQualifiedName(actualArgument, expectedArgument.SymbolFullName)
				&& !Implements(actualArgument, expectedArgument)
				&& !InheritsFrom(actualArgument, expectedArgument)
			)
				return false;
		}

		return true;
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
	/// Creates the declaration options required to reopen a containing type as a partial type.
	/// </summary>
	/// <param name="containingType">The containing type symbol to describe.</param>
	/// <returns>Options that reproduce the containing type declaration.</returns>
	/// <remarks>
	/// Base types, interfaces, attributes, and primary-constructor parameters are intentionally
	/// omitted because the returned declaration reopens an existing partial type.
	/// </remarks>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="containingType"/> is <see langword="null"/>.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// Thrown when the symbol is not a class, struct, record class, or record struct.
	/// </exception>
	public static TypeDeclarationOptions CreatePartialTypeDeclarationOptions(
		INamedTypeSymbol containingType
	) => CreatePartialTypeDeclarationOptions(containingType, includeOptionalParts: true);

	/// <summary>
	/// Creates the declaration options required to reopen a containing type as a partial type.
	/// </summary>
	/// <param name="containingType">The containing type symbol to describe.</param>
	/// <param name="includeOptionalParts">
	/// <see langword="true"/> to reproduce optional accessibility, sealed modifiers, and generic
	/// constraints; <see langword="false"/> to emit only the declaration parts required to reopen
	/// the partial type.
	/// </param>
	/// <returns>Options that reproduce the containing partial type.</returns>
	/// <remarks>
	/// The basic form still preserves the type kind, generic parameter names and order,
	/// <c>static</c>, and <c>readonly</c>, because those affect declaration compatibility.
	/// </remarks>
	public static TypeDeclarationOptions CreatePartialTypeDeclarationOptions(
		INamedTypeSymbol containingType,
		bool includeOptionalParts
	)
	{
		if (containingType == null)
			throw new ArgumentNullException(nameof(containingType));

		var kind = GetTypeDeclarationKind(containingType);
		var isStatic = containingType.IsStatic;

		return new(containingType.Name)
		{
			Kind = kind,
			Accessibility = includeOptionalParts
				? containingType.IsFileLocal
					? TypeDeclarationAccessibility.File
					: containingType.DeclaredAccessibility.ToTypeDeclarationAccessibility()
				: null,
			IsPartial = true,
			IsStatic = isStatic,
			IsSealed = includeOptionalParts && !isStatic && containingType.IsSealed,
			IsAbstract = includeOptionalParts && !isStatic && containingType.IsAbstract,
			IsReadOnly = containingType.IsReadOnly,
			GenericTypes =
			[
				.. containingType.TypeParameters.Select(typeParameter =>
					CreateGenericTypeParameterOptions(typeParameter, includeOptionalParts)
				),
			],
		};
	}

	static TypeDeclarationKind GetTypeDeclarationKind(INamedTypeSymbol typeSymbol) =>
		(typeSymbol.TypeKind, typeSymbol.IsRecord) switch
		{
			(TypeKind.Class, false) => TypeDeclarationKind.Class,
			(TypeKind.Class, true) => TypeDeclarationKind.RecordClass,
			(TypeKind.Struct, false) => TypeDeclarationKind.Struct,
			(TypeKind.Struct, true) => TypeDeclarationKind.RecordStruct,
			(TypeKind.Interface, _) => TypeDeclarationKind.Interface,
			_ => throw new ArgumentException(
				$"Type '{typeSymbol.ToDisplayString()}' is not a supported containing type.",
				nameof(typeSymbol)
			),
		};

	static GenericTypeParameterOptions CreateGenericTypeParameterOptions(
		ITypeParameterSymbol typeParameter,
		bool includeConstraints
	)
	{
		var constraints = ImmutableArray.CreateBuilder<string>();
		if (!includeConstraints)
			return new(typeParameter.Name) { Constraints = constraints.ToImmutable() };

		if (typeParameter.HasUnmanagedTypeConstraint)
			constraints.Add("unmanaged");
		else if (typeParameter.HasValueTypeConstraint)
			constraints.Add("struct");
		else if (typeParameter.HasReferenceTypeConstraint)
			constraints.Add(
				typeParameter.ReferenceTypeConstraintNullableAnnotation
				== NullableAnnotation.Annotated
					? "class?"
					: "class"
			);
		else if (typeParameter.HasNotNullConstraint)
			constraints.Add("notnull");

		constraints.AddRange(
			typeParameter.ConstraintTypes.Select(static constraint =>
				constraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
			)
		);

		if (
			typeParameter.HasConstructorConstraint
			&& !typeParameter.HasValueTypeConstraint
			&& !typeParameter.HasUnmanagedTypeConstraint
		)
			constraints.Add("new()");

		return new(typeParameter.Name) { Constraints = constraints.ToImmutable() };
	}

	/// <summary>
	/// Determines whether the symbol is accessible from public or internal scopes.
	/// </summary>
	public static bool IsAccessibleAsPublicOrInternal(ISymbol symbol) =>
		symbol == null
			? throw new ArgumentNullException(nameof(symbol))
			: symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
}
