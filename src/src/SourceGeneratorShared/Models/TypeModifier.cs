namespace Purview.SourceGeneratorFramework.Models;

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
/// A single composition step applied to a type reference.
/// </summary>
/// <remarks>
/// Modifiers are stored innermost-first, so <c>int?[]</c> is <c>[Nullable, Array(1)]</c> and <c>int[]?</c>
/// is <c>[Array(1), Nullable]</c>. Rendering appends each suffix in order; matching consumes them in reverse.
/// </remarks>
public readonly record struct TypeModifier
{
	/// <summary>Gets the kind of composition step.</summary>
	public TypeModifierKind Kind { get; init; }

	/// <summary>Gets the array rank. Only meaningful when <see cref="Kind"/> is <see cref="TypeModifierKind.Array"/>.</summary>
	public int Rank { get; init; }

	/// <summary>Gets a nullable modifier.</summary>
	public static TypeModifier Nullable => new() { Kind = TypeModifierKind.Nullable, Rank = 0 };

	/// <summary>Gets a pointer modifier.</summary>
	public static TypeModifier PointerModifier => new() { Kind = TypeModifierKind.PointerModifier, Rank = 0 };

	/// <summary>Creates an array modifier of the given rank.</summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rank"/> is less than one.</exception>
	public static TypeModifier Array(int rank = 1)
	{
		if (rank < 1)
			throw new ArgumentOutOfRangeException(nameof(rank), rank, "An array rank must be at least one.");

		// The rank is stored in the modifier, but it is not used for equality or hashing. This is because the rank is not part of the C# type system; it is only used for rendering.
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
}
