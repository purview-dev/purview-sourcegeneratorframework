using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// A structured reference to a type as it appears at a use site: a named type, generic parameter or
/// <see langword="dynamic"/>, composed with nullable, pointer and array modifiers.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TypeValueObject"/> models type <i>identity</i>; this models a type <i>reference</i>. Anything
/// a use site can spell that identity cannot — <c>T</c>, <c>int[]</c>, <c>byte*</c>, <c>string?</c> — is
/// expressed here, which is why <see cref="TypeValueObject.TypeArguments"/> is a collection of these.
/// </para>
/// <para>
/// <b>This is a reference type by necessity, not by preference.</b> As a struct it would embed a
/// <see cref="TypeValueObject"/> by value while <see cref="TypeValueObject"/> holds an
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
/// </remarks>
public sealed record TypeReferenceOptions
{
	/// <summary>
	/// Initializes a new, empty reference. Prefer the named factories.
	/// </summary>
	TypeReferenceOptions()
	{
		Kind = TypeReferenceKind.None;
		Modifiers = [];
	}

	/// <summary>
	/// Initializes a new reference to the given named type.
	/// </summary>
	public TypeReferenceOptions(TypeValueObject type)
	{
		Kind = TypeReferenceKind.Named;
		Type = type;
		Modifiers = [];
	}

	/// <summary>Gets a value indicating whether this reference is empty.</summary>
	public bool IsEmpty => Kind == TypeReferenceKind.None;

	/// <summary>Gets what this reference refers to beneath its modifiers.</summary>
	public TypeReferenceKind Kind { get; init; }

	/// <summary>Gets the named type, when <see cref="Kind"/> is <see cref="TypeReferenceKind.Named"/>.</summary>
	public TypeValueObject Type { get; init; }

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
	/// interchangeable with a bare <see cref="TypeValueObject"/>.
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
				TypeReferenceKind.Named => Type.RenderFullName,
				TypeReferenceKind.TypeParameter => TypeParameterName ?? string.Empty,
				TypeReferenceKind.Dynamic => "dynamic",
				_ => string.Empty,
			};

			if (Modifiers.IsDefaultOrEmpty)
				return core;

			var builder = new StringBuilder(core);

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

	/// <inheritdoc />
	public override string ToString() => RenderFullName;

	/// <summary>
	/// Implicitly converts a named type to an unmodified reference.
	/// </summary>
	public static implicit operator TypeReferenceOptions(TypeValueObject type) => new(type);

	/// <summary>
	/// Implicitly converts a reference to its rendered name.
	/// </summary>
	public static implicit operator string(TypeReferenceOptions? reference) =>
		reference?.RenderFullName ?? string.Empty;

	/// <summary>
	/// Implicitly converts a reference to its underlying type value object, discarding any modifiers.
	/// </summary>
	public static implicit operator TypeValueObject(TypeReferenceOptions? reference) =>
		reference?.Type ?? TypeValueObject.Empty;

	// ---------------------------------------------------------------------------------------------
	// Composition
	// ---------------------------------------------------------------------------------------------

	/// <summary>Appends a nullable annotation.</summary>
	public TypeReferenceOptions Nullable() => Append(TypeModifier.Nullable);

	/// <summary>Appends an array of the given rank.</summary>
	public TypeReferenceOptions MakeArray(int rank = 1) => Append(TypeModifier.Array(rank));

	/// <summary>Appends a pointer indirection.</summary>
	public TypeReferenceOptions MakePointer() => Append(TypeModifier.PointerModifier);

	TypeReferenceOptions Append(TypeModifier modifier)
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
							current is INamedTypeSymbol { SpecialType: SpecialType.System_Nullable_T } nullable
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
			TypeReferenceKind.Named => Type.Matches(current),
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
	public bool Equals(TypeValueObject other) => IsPlainNamedType && Type.Equals(other);

	/// <summary>
	/// Determines whether the specified reference describes the same composed type.
	/// </summary>
	/// <remarks>
	/// Declared explicitly because the synthesised record equality would compare
	/// <see cref="ImmutableArray{T}"/> by its default comparer, which is reference equality on the underlying
	/// array rather than structural equality of the modifiers.
	/// </remarks>
	public bool Equals(TypeReferenceOptions? other)
	{
		if (ReferenceEquals(this, other))
			return true;

		if (other is null || Kind != other.Kind)
			return false;

		if (!string.Equals(TypeParameterName, other.TypeParameterName, StringComparison.Ordinal))
			return false;

		if (Kind == TypeReferenceKind.Named && !Type.Equals(other.Type))
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
				hashCode = (hashCode * 397) ^ Type.GetHashCode();

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
	public static readonly TypeReferenceOptions Empty = new();

	/// <summary>Gets a reference to <see langword="dynamic"/>.</summary>
	public static TypeReferenceOptions Dynamic { get; } = new() { Kind = TypeReferenceKind.Dynamic, Modifiers = [] };

	/// <summary>Creates a reference to an open generic parameter.</summary>
	public static TypeReferenceOptions ForTypeParameter(string name)
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
	public static TypeReferenceOptions Create<T>() => Create(typeof(T));

	/// <summary>Creates a reference from a runtime type.</summary>
	/// <exception cref="ArgumentException">Thrown when the type cannot be represented.</exception>
	public static TypeReferenceOptions Create(Type type)
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
	public static TypeReferenceOptions Create(ITypeSymbol typeSymbol)
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
	public static bool TryCreate(ITypeSymbol? typeSymbol, out TypeReferenceOptions value)
	{
		value = Empty;

		if (typeSymbol is null)
			return false;

		// Fast path: the overwhelmingly common case is an unmodified named type.
		if (
			typeSymbol is INamedTypeSymbol { SpecialType: not SpecialType.System_Nullable_T } named
			&& named.NullableAnnotation != NullableAnnotation.Annotated
		)
		{
			if (!TypeValueObject.TryCreate(named, out var plain))
				return false;

			value = new TypeReferenceOptions(plain);

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

				case INamedTypeSymbol { SpecialType: SpecialType.System_Nullable_T } nullable
					when nullable.TypeArguments.Length == 1:
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
				if (!TypeValueObject.TryCreate(current, out var namedType))
					return false;

				value = new TypeReferenceOptions(namedType) { Modifiers = modifiers.ToImmutable() };

				return true;
		}
	}

	/// <summary>
	/// Attempts to create a reference from a runtime type, peeling arrays, pointers and nullability into
	/// modifiers.
	/// </summary>
	public static bool TryCreate(Type? type, out TypeReferenceOptions value)
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

		if (!TypeValueObject.TryCreate(current, out var namedType))
			return false;

		value = new TypeReferenceOptions(namedType) { Modifiers = modifiers.ToImmutable() };

		return true;
	}
}
