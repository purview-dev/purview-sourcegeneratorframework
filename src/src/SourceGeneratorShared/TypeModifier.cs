namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Identifies a single composition step applied to a type reference.
/// </summary>
public enum TypeModifierKind
{
	/// <summary>A nullable annotation, or a <see cref="Nullable{T}"/> wrapper for a value type.</summary>
	Nullable = 0,

	/// <summary>A pointer indirection.</summary>
	/// <remarks>
	/// Calling this field `Pointer` results in a CA1720 warning because it is a reserved keyword in C#. The name `PointerModifier` is used instead to avoid the warning.
	/// </remarks>
	PointerModifier = 1,

	/// <summary>An array of the given rank.</summary>
	Array = 2,
}

/// <summary>
/// Distinguishes how a <see cref="TypeModifierKind.Nullable"/> modifier was formed, which determines
/// whether the <c>?</c> may be elided when the target compilation does not support nullable annotations.
/// </summary>
/// <remarks>
/// A nullable <i>value</i> type (<c>int?</c>, <c>Nullable&lt;T&gt;</c>) is valid in any nullable context and is
/// never elided. A nullable <i>reference</i> annotation (<c>string?</c>) triggers CS8632 outside a nullable
/// context and is elided when unsupported. <see cref="Unknown"/> is used when the value-versus-reference
/// question cannot be answered without a compilation; such annotations are never elided so a genuine value
/// type cannot be silently changed.
/// </remarks>
public enum NullableModifierKind
{
	/// <summary>The value-versus-reference question is unknown.</summary>
	Unknown = 0,

	/// <summary>The modifier represents a nullable value type, such as <c>int?</c> or <c>Nullable&lt;T&gt;</c>.</summary>
	ValueType = 1,

	/// <summary>The modifier represents a nullable reference type annotation, such as <c>string?</c>.</summary>
	Reference = 2,
}

/// <summary>
/// A single composition step applied to a type reference.
/// </summary>
/// <remarks>
/// Modifiers are stored innermost-first, so <c>int?[]</c> is <c>[Nullable, Array(1)]</c> and <c>int[]?</c>
/// is <c>[Array(1), Nullable]</c>. Rendering appends each suffix in order; matching consumes them in reverse.
/// </remarks>
public readonly struct TypeModifier : IEquatable<TypeModifier>
{
	/// <summary>Gets the kind of composition step.</summary>
	public TypeModifierKind Kind { get; init; }

	/// <summary>Gets the array rank. Only meaningful when <see cref="Kind"/> is <see cref="TypeModifierKind.Array"/>.</summary>
	public int Rank { get; init; }

	/// <summary>
	/// Gets how a <see cref="TypeModifierKind.Nullable"/> modifier was formed. This is render-only metadata
	/// and is deliberately excluded from equality and hashing, so a symbol-sourced <c>string?</c> still
	/// compares equal to a composed <c>MakeNullable()</c> one.
	/// </summary>
	public NullableModifierKind NullableKind { get; init; }

	/// <summary>Gets a nullable modifier whose value-versus-reference classification is unknown.</summary>
	public static TypeModifier Nullable => new() { Kind = TypeModifierKind.Nullable, Rank = 0 };

	/// <summary>Gets a nullable modifier representing a nullable value type.</summary>
	public static TypeModifier NullableValueType =>
		new()
		{
			Kind = TypeModifierKind.Nullable,
			Rank = 0,
			NullableKind = NullableModifierKind.ValueType,
		};

	/// <summary>Gets a nullable modifier representing a nullable reference type annotation.</summary>
	public static TypeModifier NullableReference =>
		new()
		{
			Kind = TypeModifierKind.Nullable,
			Rank = 0,
			NullableKind = NullableModifierKind.Reference,
		};

	/// <summary>Gets a pointer modifier.</summary>
	public static TypeModifier PointerModifier => new() { Kind = TypeModifierKind.PointerModifier, Rank = 0 };

	/// <summary>Creates an array modifier of the given rank.</summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rank"/> is less than one.</exception>
	public static TypeModifier Array(int rank = 1)
	{
		if (rank < 1)
			throw new ArgumentOutOfRangeException(nameof(rank), rank, "An array rank must be at least one.");

		// The rank is stored in the Rank property, but the Kind is always Array.
		return new() { Kind = TypeModifierKind.Array, Rank = rank };
	}

	/// <summary>
	/// Gets the C# suffix for this modifier.
	/// </summary>
	public string Suffix =>
		Kind switch
		{
			TypeModifierKind.Nullable => "?",
			TypeModifierKind.PointerModifier => "*",
			TypeModifierKind.Array => Rank == 1 ? "[]" : $"[{new string(',', Rank - 1)}]",
			_ => string.Empty,
		};

	/// <inheritdoc />
	public override string ToString() => Suffix;

	/// <summary>
	/// Compares modifiers by their structural shape, ignoring the render-only
	/// <see cref="NullableKind"/> classification.
	/// </summary>
	public bool Equals(TypeModifier other) => Kind == other.Kind && Rank == other.Rank;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is TypeModifier other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode()
	{
		unchecked
		{
			return ((int)Kind * 397) ^ Rank;
		}
	}

	/// <summary>Compares modifiers by their structural shape, ignoring the render-only nullable classification.</summary>
	public static bool operator ==(TypeModifier left, TypeModifier right) => left.Equals(right);

	/// <summary>Compares modifiers by their structural shape, ignoring the render-only nullable classification.</summary>
	public static bool operator !=(TypeModifier left, TypeModifier right) => !left.Equals(right);
}
