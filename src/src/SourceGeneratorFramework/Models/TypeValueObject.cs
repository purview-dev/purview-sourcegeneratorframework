using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// Represents a semantic reference to a named type, independent of any single compilation.
/// </summary>
/// <remarks>
/// <para>
/// This value object models <b>named type identity</b>. Arrays, pointers, function pointers,
/// <see langword="dynamic"/>, generic parameters and error types are not identities and are modelled by
/// <see cref="TypeReferenceOptions"/>, which is also the element type of <see cref="TypeArguments"/>.
/// </para>
/// <para>
/// Matching against an <see cref="ITypeSymbol"/> is <i>structural</i> — name, namespace, containing-type
/// chain and generic shape — rather than symbolic, so a value created against one compilation can be matched
/// against another.
/// </para>
/// </remarks>
public readonly record struct TypeValueObject
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct from a <see cref="Type"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when the provided type is null.</exception>
	/// <exception cref="ArgumentException">
	/// Thrown when the type is not a named type. Use <see cref="TryCreate(Type, out TypeValueObject)"/> when the
	/// input may not be representable, or <see cref="TypeReferenceOptions.TryCreate(Type, out TypeReferenceOptions)"/>
	/// to capture array, pointer and nullable composition.
	/// </exception>
	public TypeValueObject(Type type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		if (!IsRepresentable(type))
		{
			throw new ArgumentException(
				$"The type '{type}' is not a named type and cannot be represented by {nameof(TypeValueObject)}. Use {nameof(TypeReferenceOptions)} for composed references.",
				nameof(type)
			);
		}

		var knownType = KnownLangTypes.Get(type);
		if (knownType != TypeMapping.Empty)
		{
			Name = knownType.Type.Name;
			Namespace = knownType.Type.Namespace;
			Keyword = knownType.Keyword;
			SpecialType = knownType.SpecialType;
			ContainingTypes = [];
			TypeArguments = [];

			return;
		}

		var allArguments = type.IsGenericType ? type.GetGenericArguments() : [];

		Name = StripArity(type.Name);
		Namespace = string.IsNullOrEmpty(type.Namespace) ? null : type.Namespace;
		ContainingTypes = BuildContainingTypes(type, allArguments, out var consumed);
		GenericArity = GetOwnArity(type);
		TypeArguments = type.IsGenericTypeDefinition ? [] : BuildArguments(allArguments, consumed, GenericArity);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct from a type name and namespace.
	/// <para>
	/// <b>Note:</b> This constructor does not validate the provided type name or namespace. It is the caller's
	/// responsibility to ensure the values represent a real type. For keyword types prefer
	/// <see cref="TypeValueObject(SpecialType)"/>, <see cref="TypeValueObject(ITypeSymbol)"/> or
	/// <see cref="TypeValueObject(Type)"/>.
	/// </para>
	/// <para>
	/// This produces a top-level, non-generic type. Use <see cref="Nested(string, int)"/> for nested types and
	/// <see cref="MakeGeneric(TypeReferenceOptions[])"/> for constructed generics.
	/// </para>
	/// </summary>
	public TypeValueObject(string typeName, string? @namespace)
	{
		Name = typeName ?? throw new ArgumentNullException(nameof(typeName));
		Namespace = string.IsNullOrEmpty(@namespace) ? null : @namespace;
		GenericArity = 0;
		ContainingTypes = [];
		TypeArguments = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct from a Roslyn type symbol.
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when the provided symbol is null.</exception>
	/// <exception cref="ArgumentException">
	/// Thrown when the symbol is not a resolvable named type. Use
	/// <see cref="TryCreate(ITypeSymbol, out TypeValueObject)"/> inside generator pipelines, where unresolved and
	/// composed symbols are routine.
	/// </exception>
	public TypeValueObject(ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		if (typeSymbol is not INamedTypeSymbol namedType || !IsRepresentable(namedType))
		{
			throw new ArgumentException(
				$"The symbol '{typeSymbol.ToDisplayString()}' is not a resolvable named type and cannot be represented by {nameof(TypeValueObject)}. Use {nameof(TryCreate)} for a non-throwing conversion.",
				nameof(typeSymbol)
			);
		}

		var knownType = KnownLangTypes.Get(namedType.SpecialType);
		if (knownType != TypeMapping.Empty)
		{
			Name = knownType.Type.Name;
			Namespace = knownType.Type.Namespace;
			Keyword = knownType.Keyword;
			SpecialType = knownType.SpecialType;
			ContainingTypes = [];
			TypeArguments = [];

			return;
		}

		Name = namedType.Name;
		Namespace = namedType.ContainingNamespace is null or { IsGlobalNamespace: true }
			? null
			: namedType.ContainingNamespace.ToDisplayString();

		ContainingTypes = BuildContainingTypes(namedType);
		GenericArity = namedType.Arity;
		TypeArguments = BuildArguments(namedType);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct from a recognized C# keyword
	/// special type.
	/// </summary>
	public TypeValueObject(SpecialType specialType)
	{
		var knownType = KnownLangTypes.Get(specialType);
		if (knownType == TypeMapping.Empty)
		{
			throw new ArgumentException(
				$"The provided special type '{specialType}' is not a recognized C# keyword type.",
				nameof(specialType)
			);
		}

		Name = knownType.Type.Name;
		Namespace = knownType.Type.Namespace;
		Keyword = knownType.Keyword;
		SpecialType = knownType.SpecialType;
		ContainingTypes = [];
		TypeArguments = [];
	}

	/// <summary>
	/// Gets the recognized C# keyword special type, or <see cref="SpecialType.None"/> when the type is not a
	/// recognized keyword type.
	/// </summary>
	/// <remarks>
	/// Populated <b>only</b> for C# keyword types. Roslyn populates <see cref="ITypeSymbol.SpecialType"/> for many
	/// non-keyword types as well — <c>System.DateTime</c>, <c>System.IDisposable</c>, <c>System.Nullable&lt;T&gt;</c>
	/// and others — so a special type mismatch is never on its own grounds for rejecting a match.
	/// </remarks>
	public SpecialType SpecialType { get; init; } = SpecialType.None;

	/// <summary>
	/// Gets the C# keyword for the type, or <see langword="null"/> when the type has no keyword representation.
	/// </summary>
	public string? Keyword { get; init; }

	/// <summary>
	/// Gets the type name without its namespace, containing types or generic arity suffix.
	/// </summary>
	public string Name { get; init; }

	/// <summary>
	/// Gets the namespace, or <see langword="null"/> when the type is in the global namespace.
	/// </summary>
	public string? Namespace { get; init; }

	/// <summary>
	/// Gets the chain of containing types, outermost first, for a nested type.
	/// </summary>
	/// <remarks>
	/// Each entry carries only a name, arity and — where known — type arguments. Its own
	/// <see cref="Namespace"/> is always <see langword="null"/>, because the namespace belongs to the chain
	/// as a whole.
	/// </remarks>
	public ImmutableArray<TypeValueObject> ContainingTypes { get; init; }

	/// <summary>
	/// Gets the number of generic parameters declared by the type itself, excluding those inherited from
	/// containing types.
	/// </summary>
	public int GenericArity { get; init; }

	/// <summary>
	/// Gets the generic type arguments for a constructed type.
	/// </summary>
	/// <remarks>
	/// Empty for a non-generic type and for an open generic definition; use <see cref="GenericArity"/> to
	/// distinguish those cases. Arguments are <see cref="TypeReferenceOptions"/> so that composed arguments —
	/// <c>List&lt;int[]&gt;</c>, <c>List&lt;T&gt;</c>, <c>List&lt;byte*&gt;</c>, <c>List&lt;string?&gt;</c> — are
	/// represented exactly rather than widened to the open definition.
	/// </remarks>
	public ImmutableArray<TypeReferenceOptions> TypeArguments { get; init; }

	/// <summary>
	/// Gets a value indicating whether this value represents an open generic type definition.
	/// </summary>
	public bool IsGenericTypeDefinition => GenericArity > 0 && TypeArguments.IsDefaultOrEmpty;

	/// <summary>Gets a value indicating whether the type is nested inside another type.</summary>
	public bool IsNested => !ContainingTypes.IsDefaultOrEmpty;

	/// <summary>Gets a value indicating whether the type is in the global namespace.</summary>
	public bool IsGlobalNamespace => Namespace is null;

	/// <summary>
	/// Gets the CLR metadata name, including the generic arity suffix when required.
	/// </summary>
	public string MetadataName => GenericArity == 0 ? Name : $"{Name}`{GenericArity}";

	/// <summary>
	/// Gets the fully-qualified CLR metadata name used by Roslyn type lookup, using <c>+</c> to separate nested
	/// types as required by <c>Compilation.GetTypeByMetadataName</c> and <c>ForAttributeWithMetadataName</c>.
	/// </summary>
	public string MetadataFullName
	{
		get
		{
			var name = MetadataName;
			if (IsNested)
				name = $"{string.Join("+", ContainingTypes.Select(static type => type.MetadataName))}+{name}";

			return IsGlobalNamespace ? name : $"{Namespace}.{name}";
		}
	}

	/// <summary>
	/// Gets the fully-qualified global type name for use in generated code.
	/// </summary>
	public string RenderFullName
	{
		get
		{
			if (SpecialType != SpecialType.None)
				return Keyword!;

			var name = RenderTypeName;
			if (IsNested)
				name = $"{string.Join(".", ContainingTypes.Select(static type => type.RenderTypeName))}.{name}";

			return IsGlobalNamespace ? name : $"global::{Namespace}.{name}";
		}
	}

	/// <summary>
	/// Gets the type name suitable for use in generated code, without namespace or containing types.
	/// </summary>
	public string RenderTypeName
	{
		get
		{
			if (SpecialType != SpecialType.None)
				return Keyword!;

			if (GenericArity == 0)
				return Name;

			return TypeArguments.IsDefaultOrEmpty
				? $"{Name}<{new string(',', GenericArity - 1)}>"
				: $"{Name}<{string.Join(", ", TypeArguments.Select(static argument => argument.RenderFullName))}>";
		}
	}

	/// <summary>
	/// Gets the fully-qualified name rendered as a C# attribute application, including brackets and the
	/// optional omission of the <c>Attribute</c> suffix.
	/// </summary>
	public string RenderAttributeName => $"[{TypeHelpers.GetTypeName(RenderFullName)}]";

	/// <summary>
	/// Returns the rendered full name.
	/// </summary>
	public override string ToString() => RenderFullName;

	// ---------------------------------------------------------------------------------------------
	// Matching
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Determines whether the specified <see cref="ITypeSymbol"/> represents this type.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> when the symbol represents this type; otherwise <see langword="false"/>. An open
	/// generic definition matches every constructed form of that definition.
	/// </returns>
	public bool Matches(ITypeSymbol? other)
	{
		if (other is null)
			return false;

		// Special types are unique, so a positive match here is conclusive and cheap.
		// A mismatch is NOT conclusive: this value only stamps SpecialType for C# keyword types, whereas
		// Roslyn stamps it for DateTime, IDisposable, Nullable<T>, IEnumerable<T> and more.
		if (SpecialType != SpecialType.None && SpecialType == other.SpecialType)
			return true;

		// Arrays, pointers, function pointers and dynamic are modelled by TypeReferenceOptions. Their symbols
		// also expose a null ContainingNamespace, so they must be excluded before it is read.
		if (other is not INamedTypeSymbol namedType || !IsRepresentable(namedType))
			return false;

		if (!string.Equals(Name, namedType.Name, StringComparison.Ordinal))
			return false;

		if (GenericArity != namedType.Arity)
			return false;

		if (!ContainingTypesMatch(namedType.ContainingType))
			return false;

		if (!NamespaceMatches(Namespace, namedType.ContainingNamespace))
			return false;

		// An open definition represents every constructed form of that definition.
		if (TypeArguments.IsDefaultOrEmpty)
			return true;

		var otherArguments = namedType.TypeArguments;
		if (TypeArguments.Length != otherArguments.Length)
			return false;

		for (var index = 0; index < TypeArguments.Length; index++)
		{
			if (!TypeArguments[index].Matches(otherArguments[index]))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Determines whether the type of the specified symbol is this type.
	/// </summary>
	/// <remarks>
	/// Fields, properties, events, parameters and locals resolve to their declared type; methods resolve to
	/// their return type; aliases resolve to their target. Type symbols are matched directly. This makes
	/// member-level checks a single call:
	/// <code>
	/// if (eventStore.Matches(propertySymbol)) { ... }
	/// </code>
	/// </remarks>
	public bool Matches(ISymbol? other) => Matches(SymbolTypeResolver.Resolve(other));

	/// <summary>
	/// Determines whether the specified <see cref="ITypeSymbol"/> represents this type.
	/// </summary>
	/// <remarks>
	/// An alias for <see cref="Matches(ITypeSymbol?)"/> retained for call-site ergonomics. It is deliberately
	/// not surfaced through <see cref="IEquatable{T}"/>: the relation is asymmetric — an open definition
	/// matches its constructions — and cannot be made consistent with a <see cref="ISymbol" /> instance's
	/// <see cref="object.GetHashCode"/>.
	/// </remarks>
	public bool Equals(ITypeSymbol? other) => Matches(other);

	/// <summary>Determines whether the specified runtime type represents the same semantic type.</summary>
	public bool Equals(Type? other) => other is not null && TryCreate(other, out var value) && Equals(value);

	/// <summary>Determines whether the specified structured reference is an unmodified reference to this type.</summary>
	public bool Equals(TypeReferenceOptions other) => other.Equals(this);

	/// <summary>
	/// Determines whether the specified value represents the same type.
	/// </summary>
	public bool Equals(TypeValueObject other) =>
		string.Equals(Name, other.Name, StringComparison.Ordinal)
		&& string.Equals(Namespace, other.Namespace, StringComparison.Ordinal)
		&& string.Equals(Keyword, other.Keyword, StringComparison.Ordinal)
		&& SpecialType == other.SpecialType
		&& GenericArity == other.GenericArity
		&& ContainingTypesEqual(ContainingTypes, other.ContainingTypes)
		&& TypeArgumentsEqual(TypeArguments, other.TypeArguments);

	/// <summary>
	/// Returns a structural hash code for this type, its containing types and its generic arguments.
	/// </summary>
	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = Name is null ? 0 : StringComparer.Ordinal.GetHashCode(Name);
			hashCode = (hashCode * 397) ^ (Namespace is null ? 0 : StringComparer.Ordinal.GetHashCode(Namespace));
			hashCode = (hashCode * 397) ^ (Keyword is null ? 0 : StringComparer.Ordinal.GetHashCode(Keyword));
			hashCode = (hashCode * 397) ^ (int)SpecialType;
			hashCode = (hashCode * 397) ^ GenericArity;

			if (!ContainingTypes.IsDefaultOrEmpty)
			{
				foreach (var containingType in ContainingTypes)
					hashCode = (hashCode * 397) ^ containingType.GetHashCode();
			}

			if (!TypeArguments.IsDefaultOrEmpty)
			{
				foreach (var argument in TypeArguments)
					hashCode = (hashCode * 397) ^ argument.GetHashCode();
			}

			return hashCode;
		}
	}

	/// <summary>
	/// Implicitly converts a <see cref="TypeValueObject"/> to its rendered full name.
	/// </summary>
	public static implicit operator string(TypeValueObject typeValueObject) => typeValueObject.RenderFullName;

	// ---------------------------------------------------------------------------------------------
	// Composition
	// ---------------------------------------------------------------------------------------------

	/// <summary>Creates the canonical source-generation type reference for this type.</summary>
	public TypeReferenceOptions AsTypeReference() => new(this);

	/// <summary>Creates a nullable structured type reference.</summary>
	public TypeReferenceOptions MakeNullable() => AsTypeReference().Nullable();

	/// <summary>Creates an array structured type reference with the specified rank.</summary>
	public TypeReferenceOptions MakeArray(int rank = 1) => AsTypeReference().MakeArray(rank);

	/// <summary>Creates a pointer structured type reference.</summary>
	public TypeReferenceOptions MakePointer() => AsTypeReference().MakePointer();

	/// <summary>
	/// Creates a value describing a type nested inside this one.
	/// </summary>
	/// <param name="typeName">The nested type's simple name.</param>
	/// <param name="genericArity">The nested type's own generic arity.</param>
	public TypeValueObject Nested(string typeName, int genericArity = 0)
	{
		if (typeName == null)
			throw new ArgumentNullException(nameof(typeName));

		if (SpecialType != SpecialType.None)
			throw new InvalidOperationException($"Cannot nest a type inside the special type '{SpecialType}'.");

		var existing = ContainingTypes.IsDefaultOrEmpty ? 0 : ContainingTypes.Length;
		var chain = ImmutableArray.CreateBuilder<TypeValueObject>(existing + 1);

		if (existing > 0)
			chain.AddRange(ContainingTypes);

		chain.Add(this with { Namespace = null, ContainingTypes = [] });

		return new TypeValueObject
		{
			Name = typeName,
			Namespace = Namespace,
			ContainingTypes = chain.MoveToImmutable(),
			GenericArity = genericArity,
			TypeArguments = [],
		};
	}

	/// <summary>
	/// Creates a generic variant of this type from type argument names.
	/// </summary>
	public TypeValueObject MakeGeneric(params string[] typeArguments)
	{
		if (typeArguments == null)
			throw new ArgumentNullException(nameof(typeArguments));

		return MakeGeneric(
			typeArguments
				.Select(static argument => new TypeReferenceOptions(new TypeValueObject(argument, null)))
				.ToArray()
		);
	}

	/// <summary>
	/// Creates a constructed generic type using the specified named type arguments.
	/// </summary>
	public TypeValueObject MakeGeneric(params TypeValueObject[] typeArguments)
	{
		if (typeArguments == null)
			throw new ArgumentNullException(nameof(typeArguments));

		return MakeGeneric(typeArguments.Select(static argument => argument.AsTypeReference()).ToArray());
	}

	/// <summary>
	/// Creates a constructed generic type using the specified composed type arguments.
	/// </summary>
	public TypeValueObject MakeGeneric(params TypeReferenceOptions[] typeArguments)
	{
		if (typeArguments == null)
			throw new ArgumentNullException(nameof(typeArguments));

		if (typeArguments.Length == 0)
			throw new ArgumentException("At least one type argument must be provided.", nameof(typeArguments));

		if (GenericArity > 0 && typeArguments.Length != GenericArity)
		{
			throw new ArgumentException(
				$"Type '{MetadataFullName}' requires {GenericArity} type arguments, but {typeArguments.Length} were supplied.",
				nameof(typeArguments)
			);
		}

		if (SpecialType != SpecialType.None)
			throw new InvalidOperationException($"Cannot create a generic type from the special type '{SpecialType}'.");

		return this with
		{
			GenericArity = GenericArity == 0 ? typeArguments.Length : GenericArity,
			TypeArguments = [.. typeArguments],
		};
	}

	// ---------------------------------------------------------------------------------------------
	// Factories
	// ---------------------------------------------------------------------------------------------

	/// <summary>Gets an empty <see cref="TypeValueObject"/>.</summary>
	public static readonly TypeValueObject Empty;

	/// <summary>Creates a <see cref="TypeValueObject"/> from a generic type parameter.</summary>
	public static TypeValueObject Create<T>() => new(typeof(T));

	/// <summary>
	/// Attempts to create a <see cref="TypeValueObject"/> from a type symbol, returning <see langword="false"/>
	/// for symbols that cannot be represented.
	/// </summary>
	/// <remarks>
	/// Prefer this inside generator and analyzer pipelines, where array, pointer, type-parameter and unresolved
	/// error symbols are routine and must not throw.
	/// </remarks>
	public static bool TryCreate(ITypeSymbol? typeSymbol, out TypeValueObject value)
	{
		if (typeSymbol is INamedTypeSymbol namedType && IsRepresentable(namedType))
		{
			value = new TypeValueObject(namedType);

			return true;
		}

		value = Empty;

		return false;
	}

	/// <summary>
	/// Attempts to create a <see cref="TypeValueObject"/> from a runtime type, returning
	/// <see langword="false"/> for types that cannot be represented.
	/// </summary>
	public static bool TryCreate(Type? type, out TypeValueObject value)
	{
		if (type is not null && IsRepresentable(type))
		{
			value = new TypeValueObject(type);

			return true;
		}

		value = Empty;

		return false;
	}

	// ---------------------------------------------------------------------------------------------
	// Internals
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Determines whether a symbol is a resolvable named type this value object can describe.
	/// </summary>
	internal static bool IsRepresentable(ITypeSymbol? typeSymbol) =>
		typeSymbol is INamedTypeSymbol
		&& typeSymbol.TypeKind
			is not (
				TypeKind.Error
				or TypeKind.Dynamic
				or TypeKind.Pointer
				or TypeKind.FunctionPointer
				or TypeKind.Array
				or TypeKind.TypeParameter
				or TypeKind.Submission
			);

	static bool IsRepresentable(Type type) =>
		!type.IsArray && !type.IsPointer && !type.IsByRef && !type.IsGenericParameter;

	/// <summary>
	/// Compares a dotted namespace string against a namespace symbol chain without allocating.
	/// </summary>
	internal static bool NamespaceMatches(string? expected, INamespaceSymbol? actual)
	{
		if (actual is null || actual.IsGlobalNamespace)
			return expected is null;

		if (expected is null)
			return false;

		var end = expected.Length;
		for (
			var segment = actual;
			segment is not null && !segment.IsGlobalNamespace;
			segment = segment.ContainingNamespace
		)
		{
			var name = segment.Name;
			var start = end - name.Length;

			if (start < 0 || string.CompareOrdinal(expected, start, name, 0, name.Length) != 0)
				return false;

			if (start == 0)
				return segment.ContainingNamespace is null or { IsGlobalNamespace: true };

			if (expected[start - 1] != '.')
				return false;

			end = start - 1;
		}

		return false;
	}

	bool ContainingTypesMatch(INamedTypeSymbol? containingType)
	{
		var expectedCount = ContainingTypes.IsDefaultOrEmpty ? 0 : ContainingTypes.Length;

		// Walk the symbol chain innermost-first against the expected chain in reverse.
		var index = expectedCount - 1;
		for (var symbol = containingType; symbol is not null; symbol = symbol.ContainingType, index--)
		{
			if (index < 0)
				return false;

			var expected = ContainingTypes[index];
			if (
				!string.Equals(expected.Name, symbol.Name, StringComparison.Ordinal)
				|| expected.GenericArity != symbol.Arity
			)
				return false;
		}

		return index == -1;
	}

	static bool ContainingTypesEqual(ImmutableArray<TypeValueObject> left, ImmutableArray<TypeValueObject> right)
	{
		var leftCount = left.IsDefaultOrEmpty ? 0 : left.Length;
		var rightCount = right.IsDefaultOrEmpty ? 0 : right.Length;

		if (leftCount != rightCount)
			return false;

		for (var index = 0; index < leftCount; index++)
		{
			if (!left[index].Equals(right[index]))
				return false;
		}

		return true;
	}

	static bool TypeArgumentsEqual(
		ImmutableArray<TypeReferenceOptions> left,
		ImmutableArray<TypeReferenceOptions> right
	)
	{
		var leftCount = left.IsDefaultOrEmpty ? 0 : left.Length;
		var rightCount = right.IsDefaultOrEmpty ? 0 : right.Length;

		if (leftCount != rightCount)
			return false;

		for (var index = 0; index < leftCount; index++)
		{
			if (!left[index].Equals(right[index]))
				return false;
		}

		return true;
	}

	static ImmutableArray<TypeValueObject> BuildContainingTypes(INamedTypeSymbol typeSymbol)
	{
		if (typeSymbol.ContainingType is null)
			return [];

		var chain = ImmutableArray.CreateBuilder<TypeValueObject>();
		for (var containing = typeSymbol.ContainingType; containing is not null; containing = containing.ContainingType)
		{
			chain.Add(
				new TypeValueObject
				{
					Name = containing.Name,
					Namespace = null,
					ContainingTypes = [],
					GenericArity = containing.Arity,
					TypeArguments = BuildArguments(containing),
				}
			);
		}

		chain.Reverse();

		return chain.ToImmutable();
	}

	static ImmutableArray<TypeReferenceOptions> BuildArguments(INamedTypeSymbol typeSymbol)
	{
		if (typeSymbol.TypeArguments.Length == 0)
			return [];

		// An unbound or original definition carries its own type parameters as arguments; treat it as open.
		if (
			typeSymbol.IsUnboundGenericType
			|| SymbolEqualityComparer.Default.Equals(typeSymbol, typeSymbol.OriginalDefinition)
		)
			return [];

		var builder = ImmutableArray.CreateBuilder<TypeReferenceOptions>(typeSymbol.TypeArguments.Length);
		foreach (var argument in typeSymbol.TypeArguments)
		{
			// Only genuinely unrepresentable arguments (function pointers, error types) widen to the definition.
			if (!TypeReferenceOptions.TryCreate(argument, out var value))
				return [];

			builder.Add(value);
		}

		return builder.MoveToImmutable();
	}

	static ImmutableArray<TypeValueObject> BuildContainingTypes(Type type, Type[] allArguments, out int consumed)
	{
		consumed = 0;

		if (type.DeclaringType is null)
			return [];

		return [];

		var chain = new List<Type>();
		for (var declaring = type.DeclaringType; declaring is not null; declaring = declaring.DeclaringType)
			chain.Add(declaring);

		chain.Reverse();

		var builder = ImmutableArray.CreateBuilder<TypeValueObject>(chain.Count);
		foreach (var link in chain)
		{
			var arity = GetOwnArity(link);
			builder.Add(
				new TypeValueObject
				{
					Name = StripArity(link.Name),
					Namespace = null,
					ContainingTypes = [],
					GenericArity = arity,
					TypeArguments = type.IsGenericTypeDefinition ? [] : BuildArguments(allArguments, consumed, arity),
				}
			);

			consumed += arity;
		}

		return builder.MoveToImmutable();
	}

	static ImmutableArray<TypeReferenceOptions> BuildArguments(Type[] allArguments, int offset, int count)
	{
		if (count == 0 || allArguments.Length < offset + count)
			return [];

		var builder = ImmutableArray.CreateBuilder<TypeReferenceOptions>(count);
		for (var index = offset; index < offset + count; index++)
		{
			if (!TypeReferenceOptions.TryCreate(allArguments[index], out var value))
				return [];

			builder.Add(value);
		}

		return builder.MoveToImmutable();
	}

	static int GetOwnArity(Type type)
	{
		if (!type.IsGenericType && !type.IsGenericTypeDefinition)
			return 0;

		var total = type.GetGenericArguments().Length;
		var declaring = type.DeclaringType;
		var outer =
			declaring is not null && (declaring.IsGenericType || declaring.IsGenericTypeDefinition)
				? declaring.GetGenericArguments().Length
				: 0;

		return total - outer;
	}

	static string StripArity(string metadataName)
	{
		var aritySeparator = metadataName.IndexOf('`');

		return aritySeparator < 0 ? metadataName : metadataName.Substring(0, aritySeparator);
	}
}
