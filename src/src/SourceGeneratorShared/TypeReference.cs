using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// A structured reference to a type as it appears at a use site: a named type, generic parameter or
/// <see langword="dynamic"/>, composed with nullable, pointer and array modifiers.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TypeIdentity"/> models type <i>identity</i>; this models a type <i>reference</i>. Anything
/// a use site can spell that identity cannot — <c>T</c>, <c>int[]</c>, <c>byte*</c>, <c>string?</c> — is
/// expressed here, which is why <see cref="TypeIdentity.TypeArguments"/> is a collection of these.
/// </para>
/// <para>
/// <b>This is a reference type by necessity, not by preference.</b> As a struct it would embed a
/// <see cref="TypeIdentity"/> by value while <see cref="TypeIdentity"/> holds an
/// <see cref="ImmutableArray{T}"/> of these — a mutual value-type layout cycle. The C# compiler accepts that
/// (no CS0523, because <see cref="ImmutableArray{T}"/>'s only field is an array reference) but the CLR type
/// loader rejects it at runtime with a <see cref="TypeLoadException"/>: see dotnet/runtime#11259. Making one
/// side of the cycle a reference type is the fix, and this is the cheaper side to convert — it is the larger
/// of the two and lives in arrays, so references cost less to copy than the embedded value did.
/// </para>
/// <para>
/// Nullable reference annotations are recorded but not enforced during matching: <c>string?</c> matches both
/// the annotated and unannotated symbol, because annotation is metadata rather than identity. Nullable
/// <i>value</i> types are enforced, because <c>int?</c> is a genuinely different type from <c>int</c>.
/// </para>
/// <para>
/// A named reference inherits its generic comparison behavior from <see cref="TypeIdentity"/>. A reference
/// wrapping an open generic definition matches constructed symbols of that definition; a reference wrapping a
/// constructed identity validates its arguments. Reference modifiers are always significant, so an open
/// <c>List&lt;&gt;</c> reference does not make <c>List&lt;int&gt;[]</c> match unless the reference also has the array
/// modifier.
/// </para>
/// </remarks>
public sealed record TypeReference
{
	/// <summary>
	/// Initializes a new, empty reference. Prefer the named factories.
	/// </summary>
	TypeReference()
	{
		Kind = TypeReferenceKind.None;
		Modifiers = [];
	}

	/// <summary>
	/// Initializes a new reference to the given named type.
	/// </summary>
	public TypeReference(TypeIdentity typeIdentity)
	{
		Kind = TypeReferenceKind.Named;
		Identity = typeIdentity;
		Modifiers = [];
	}

	/// <summary>Gets a value indicating whether this reference is empty.</summary>
	public bool IsEmpty => Kind == TypeReferenceKind.None;

	/// <summary>Gets what this reference refers to beneath its modifiers.</summary>
	public TypeReferenceKind Kind { get; init; }

	/// <summary>Gets the named type, when <see cref="Kind"/> is <see cref="TypeReferenceKind.Named"/>.</summary>
	public TypeIdentity Identity { get; init; }

	/// <summary>
	/// Gets the generic parameter name, when <see cref="Kind"/> is <see cref="TypeReferenceKind.TypeParameter"/>.
	/// </summary>
	public string? TypeParameterName { get; init; }

	/// <summary>
	/// Gets the composition modifiers, innermost-first.
	/// </summary>
	public ImmutableArray<TypeModifier> Modifiers { get; init; }

	/// <summary>
	/// Gets a value indicating whether this is an unmodified reference to a named type, and therefore
	/// interchangeable with a bare <see cref="TypeIdentity"/>.
	/// </summary>
	public bool IsPlainNamedType => Kind == TypeReferenceKind.Named && Modifiers.IsDefaultOrEmpty;

	/// <summary>Gets a value indicating whether the outermost modifier is an array.</summary>
	public bool IsArray => LastModifier?.Kind == TypeModifierKind.Array;

	/// <summary>Gets a value indicating whether the outermost modifier is a pointer.</summary>
	public bool IsPointer => LastModifier?.Kind == TypeModifierKind.PointerModifier;

	/// <summary>Gets a value indicating whether the outermost modifier is a nullable annotation.</summary>
	public bool IsNullable => LastModifier?.Kind == TypeModifierKind.Nullable;

	TypeModifier? LastModifier => Modifiers.IsDefaultOrEmpty ? null : Modifiers[Modifiers.Length - 1];

	/// <summary>
	/// Gets the fully-qualified reference as it should be rendered in generated code.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases")]
	public string RenderFullName
	{
		get
		{
			var core = Kind switch
			{
				TypeReferenceKind.Named => Identity.RenderFullName,
				TypeReferenceKind.TypeParameter => TypeParameterName ?? string.Empty,
				TypeReferenceKind.Dynamic => "dynamic",
				_ => string.Empty,
			};

			if (Modifiers.IsDefaultOrEmpty)
				return core;

			StringBuilder builder = new(core);

			// `?` and `*` read innermost-first, but a run of array declarators reads outermost-first:
			// `int[][,]` is a rank-1 array of rank-2 arrays. Each contiguous array run is therefore emitted
			// in reverse.
			var index = 0;
			while (index < Modifiers.Length)
			{
				if (Modifiers[index].Kind != TypeModifierKind.Array)
				{
					builder.Append(Modifiers[index].Suffix);
					index++;

					continue;
				}

				var start = index;
				while (index < Modifiers.Length && Modifiers[index].Kind == TypeModifierKind.Array)
					index++;

				for (var reverse = index - 1; reverse >= start; reverse--)
					builder.Append(Modifiers[reverse].Suffix);
			}

			return builder.ToString();
		}
	}

	/// <summary>
	/// Gets the fully-qualified type name for use in an attribute application.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown when this is not an unmodified named type. C# attributes cannot be arrays, pointers, nullable
	/// types, type parameters, or <see langword="dynamic"/>.
	/// </exception>
	public string RenderAttributeName
	{
		get
		{
			if (!IsPlainNamedType)
				throw new InvalidOperationException("An attribute type must be an unmodified named type.");

			// The attribute type name is the same as the named type's full name, but with the "Attribute" suffix
			return Identity.RenderAttributeName;
		}
	}

	/// <inheritdoc />
	public override string ToString() => RenderFullName;

	/// <summary>
	/// Implicitly converts a named type to an unmodified reference.
	/// </summary>
	public static implicit operator TypeReference(TypeIdentity type) => new(type);

	/// <summary>
	/// Implicitly converts a reference to its rendered name.
	/// </summary>
	public static implicit operator string(TypeReference? reference) => reference?.RenderFullName ?? string.Empty;

	/// <summary>
	/// Implicitly converts a reference to its underlying type value object, discarding any modifiers.
	/// </summary>
	public static implicit operator TypeIdentity(TypeReference? reference) => reference?.Identity ?? TypeIdentity.Empty;

	// ---------------------------------------------------------------------------------------------
	// Composition
	// ---------------------------------------------------------------------------------------------

	/// <summary>Appends a nullable annotation.</summary>
	public TypeReference Nullable() => Append(TypeModifier.Nullable);

	/// <summary>Appends an array of the given rank.</summary>
	public TypeReference MakeArray(int rank = 1) => Append(TypeModifier.Array(rank));

	/// <summary>Appends a pointer indirection.</summary>
	public TypeReference MakePointer() => Append(TypeModifier.PointerModifier);

	TypeReference Append(TypeModifier modifier)
	{
		if (Kind == TypeReferenceKind.None)
			throw new InvalidOperationException("Cannot compose modifiers onto an empty type reference.");

		var existing = Modifiers.IsDefaultOrEmpty ? 0 : Modifiers.Length;
		var builder = ImmutableArray.CreateBuilder<TypeModifier>(existing + 1);

		if (existing > 0)
			builder.AddRange(Modifiers);

		builder.Add(modifier);

		return this with
		{
			Modifiers = builder.MoveToImmutable(),
		};
	}

	// ---------------------------------------------------------------------------------------------
	// Matching
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Determines whether the given type symbol is this composed reference.
	/// </summary>
	/// <remarks>
	/// For named types, open-versus-constructed generic matching is performed by
	/// <see cref="TypeIdentity.Matches(ITypeSymbol?)"/> after this reference's modifiers have been consumed.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases")]
	public bool Matches(ITypeSymbol? other)
	{
		if (other is null || Kind == TypeReferenceKind.None)
			return false;

		var current = other;

		// Modifiers are stored innermost-first, so they are consumed in reverse against the symbol.
		if (!Modifiers.IsDefaultOrEmpty)
		{
			for (var index = Modifiers.Length - 1; index >= 0; index--)
			{
				var modifier = Modifiers[index];
				switch (modifier.Kind)
				{
					case TypeModifierKind.Array:
						if (current is not IArrayTypeSymbol array || array.Rank != modifier.Rank)
							return false;

						current = array.ElementType;

						break;

					case TypeModifierKind.PointerModifier:
						if (current is not IPointerTypeSymbol pointer)
							return false;

						current = pointer.PointedAtType;

						break;

					case TypeModifierKind.Nullable:
						// Nullable value types are a distinct type and must be unwrapped; nullable reference
						// annotations are metadata and are deliberately ignored.
						if (
							current is INamedTypeSymbol nullable
							&& nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
							&& nullable.TypeArguments.Length == 1
						)
							current = nullable.TypeArguments[0];
						else if (current.IsValueType)
							return current.NullableAnnotation == NullableAnnotation.Annotated;

						break;

					default:
						return false;
				}
			}
		}

		return Kind switch
		{
			TypeReferenceKind.Named => Identity.Matches(current),
			TypeReferenceKind.TypeParameter => current is ITypeParameterSymbol parameter
				&& string.Equals(TypeParameterName, parameter.Name, StringComparison.Ordinal),
			TypeReferenceKind.Dynamic => current is IDynamicTypeSymbol,
			_ => false,
		};
	}

	/// <summary>
	/// Determines whether the type of the given symbol is this composed reference.
	/// </summary>
	/// <remarks>
	/// Resolves fields, properties, events, parameters, locals and method return types to their type before
	/// matching. Type symbols are matched directly.
	/// </remarks>
	public bool Matches(ISymbol? other) => Matches(SymbolTypeResolver.Resolve(other));

	/// <summary>
	/// Determines whether this reference is an unmodified reference to the given named type.
	/// </summary>
	public bool Equals(TypeIdentity other) => IsPlainNamedType && Identity.Equals(other);

	/// <summary>
	/// Determines whether the specified reference describes the same composed type.
	/// </summary>
	/// <remarks>
	/// Declared explicitly because the synthesised record equality would compare
	/// <see cref="ImmutableArray{T}"/> by its default comparer, which is reference equality on the underlying
	/// array rather than structural equality of the modifiers.
	/// </remarks>
	public bool Equals(TypeReference? other)
	{
		if (ReferenceEquals(this, other))
			return true;

		if (other is null || Kind != other.Kind)
			return false;

		if (!string.Equals(TypeParameterName, other.TypeParameterName, StringComparison.Ordinal))
			return false;

		if (Kind == TypeReferenceKind.Named && !Identity.Equals(other.Identity))
			return false;

		var count = Modifiers.IsDefaultOrEmpty ? 0 : Modifiers.Length;
		var otherCount = other.Modifiers.IsDefaultOrEmpty ? 0 : other.Modifiers.Length;

		if (count != otherCount)
			return false;

		for (var index = 0; index < count; index++)
		{
			if (!Modifiers[index].Equals(other.Modifiers[index]))
				return false;
		}

		return true;
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = (int)Kind;
			hashCode =
				(hashCode * 397)
				^ (TypeParameterName is null ? 0 : StringComparer.Ordinal.GetHashCode(TypeParameterName));

			if (Kind == TypeReferenceKind.Named)
				hashCode = (hashCode * 397) ^ Identity.GetHashCode();

			if (!Modifiers.IsDefaultOrEmpty)
			{
				foreach (var modifier in Modifiers)
					hashCode = (hashCode * 397) ^ modifier.GetHashCode();
			}

			return hashCode;
		}
	}

	// ---------------------------------------------------------------------------------------------
	// Factories
	// ---------------------------------------------------------------------------------------------

	/// <summary>Gets the empty reference.</summary>
	public static readonly TypeReference Empty = new();

	/// <summary>Gets a reference to <see langword="dynamic"/>.</summary>
	public static TypeReference Dynamic { get; } = new() { Kind = TypeReferenceKind.Dynamic, Modifiers = [] };

	/// <summary>Creates a reference to an open generic parameter.</summary>
	public static TypeReference ForTypeParameter(string name)
	{
		if (name == null)
			throw new ArgumentNullException(nameof(name));

		// The name is stored in the reference, but it is not used for equality or hashing. This is because the name is not part of the C# type system; it is only used for rendering.
		return new()
		{
			Kind = TypeReferenceKind.TypeParameter,
			TypeParameterName = name,
			Modifiers = [],
		};
	}

	/// <summary>Creates a reference from a runtime type.</summary>
	public static TypeReference Create<T>() => Create(typeof(T));

	/// <summary>Creates a reference from a runtime type.</summary>
	/// <exception cref="ArgumentException">Thrown when the type cannot be represented.</exception>
	public static TypeReference Create(Type type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		if (!TryCreate(type, out var value))
			throw new ArgumentException($"The type '{type}' cannot be represented as a type reference.", nameof(type));

		// The type is known to be representable, so the out value is guaranteed to be valid.
		return value;
	}

	/// <summary>Creates a reference from a type symbol.</summary>
	/// <exception cref="ArgumentException">Thrown when the symbol cannot be represented.</exception>
	public static TypeReference Create(ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		if (!TryCreate(typeSymbol, out var value))
		{
			throw new ArgumentException(
				$"The symbol '{typeSymbol.ToDisplayString()}' cannot be represented as a type reference.",
				nameof(typeSymbol)
			);
		}

		// The symbol is known to be representable, so the out value is guaranteed to be valid.
		return value;
	}

	/// <summary>
	/// Attempts to create a reference from a type symbol, peeling arrays, pointers and nullability into
	/// modifiers.
	/// </summary>
	/// <returns><see langword="false"/> for unresolved, function-pointer and other unrepresentable symbols.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0010:Add missing cases")]
	public static bool TryCreate(ITypeSymbol? typeSymbol, out TypeReference value)
	{
		value = Empty;

		if (typeSymbol is null)
			return false;

		// Fast path: the overwhelmingly common case is an unmodified named type.
		if (
			typeSymbol is INamedTypeSymbol named
			&& named.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T
			&& named.NullableAnnotation != NullableAnnotation.Annotated
		)
		{
			if (!TypeIdentity.TryCreate(named, out var plain))
				return false;

			value = new TypeReference(plain);

			return true;
		}

		// Collected outermost-first, then reversed into innermost-first storage order.
		var modifiers = ImmutableArray.CreateBuilder<TypeModifier>();
		var current = typeSymbol;

		while (true)
		{
			if (current.IsReferenceType && current.NullableAnnotation == NullableAnnotation.Annotated)
				modifiers.Add(TypeModifier.Nullable);

			switch (current)
			{
				case IArrayTypeSymbol array:
					modifiers.Add(TypeModifier.Array(array.Rank));
					current = array.ElementType;

					continue;

				case IPointerTypeSymbol pointer:
					modifiers.Add(TypeModifier.PointerModifier);
					current = pointer.PointedAtType;

					continue;

				case INamedTypeSymbol nullable
					when nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
						&& nullable.TypeArguments.Length == 1:
					modifiers.Add(TypeModifier.Nullable);
					current = nullable.TypeArguments[0];

					continue;
			}

			break;
		}

		modifiers.Reverse();

		switch (current)
		{
			case IDynamicTypeSymbol:
				value = Dynamic with { Modifiers = modifiers.ToImmutable() };

				return true;

			case ITypeParameterSymbol parameter:
				value = ForTypeParameter(parameter.Name) with { Modifiers = modifiers.ToImmutable() };

				return true;

			default:
				if (!TypeIdentity.TryCreate(current, out var namedType))
					return false;

				value = new TypeReference(namedType) { Modifiers = modifiers.ToImmutable() };

				return true;
		}
	}

	/// <summary>
	/// Attempts to create a reference from a runtime type, peeling arrays, pointers and nullability into
	/// modifiers.
	/// </summary>
	public static bool TryCreate(Type? type, out TypeReference value)
	{
		value = Empty;

		if (type is null)
			return false;

		var modifiers = ImmutableArray.CreateBuilder<TypeModifier>();
		var current = type;

		if (current.IsByRef)
			current = current.GetElementType();

		while (true)
		{
			if (current.IsArray)
			{
				modifiers.Add(TypeModifier.Array(current.GetArrayRank()));
				current = current.GetElementType();

				continue;
			}

			if (current.IsPointer)
			{
				modifiers.Add(TypeModifier.PointerModifier);
				current = current.GetElementType();

				continue;
			}

			var underlying = System.Nullable.GetUnderlyingType(current);
			if (underlying is not null)
			{
				modifiers.Add(TypeModifier.Nullable);
				current = underlying;

				continue;
			}

			break;
		}

		modifiers.Reverse();

		if (current.IsGenericParameter)
		{
			value = ForTypeParameter(current.Name) with { Modifiers = modifiers.ToImmutable() };

			return true;
		}

		if (!TypeIdentity.TryCreate(current, out var namedType))
			return false;

		value = new TypeReference(namedType) { Modifiers = modifiers.ToImmutable() };

		return true;
	}
}
