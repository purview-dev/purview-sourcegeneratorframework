using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>Describes C# type syntax without requiring callers to assemble nullable or generic text.</summary>
public readonly record struct TypeReferenceOptions
{
	/// <summary>Creates a named type reference.</summary>
	public TypeReferenceOptions(string name) =>
		Name = string.IsNullOrWhiteSpace(name)
			? throw new ArgumentException(
				"Type name cannot be null, empty, or whitespace.",
				nameof(name)
			)
			: name;

	/// <summary>Creates a type reference from a framework type value.</summary>
	public TypeReferenceOptions(TypeValueObject type)
	{
		if (type == TypeValueObject.Empty)
		{
			this = Empty;
			return;
		}

		var name = type.RenderFullName;
		Name = string.IsNullOrWhiteSpace(name)
			? throw new ArgumentException(
				"The type value must provide a non-empty rendered name.",
				nameof(type)
			)
			: name;
	}

	/// <summary>Creates a type reference from a runtime type.</summary>
	public TypeReferenceOptions(Type type)
		: this(new TypeValueObject(type)) { }

	/// <summary>Creates a type reference from a Roslyn symbol.</summary>
	public TypeReferenceOptions(ITypeSymbol type)
		: this(new TypeValueObject(type))
	{
		IsNullable = type.NullableAnnotation == NullableAnnotation.Annotated;
	}

	/// <summary>Gets the named type, predefined keyword, tuple element list, or generic parameter.</summary>
	public string Name { get; }

	/// <summary>Gets whether this value represents the absence of a type reference.</summary>
	public bool IsEmpty => this == Empty;

	/// <summary>Gets generic type arguments.</summary>
	public ImmutableArray<TypeReferenceOptions> GenericArguments { get; init; }

	/// <summary>Gets the generic arity for an open generic definition.</summary>
	public int GenericArity { get; init; }

	/// <summary>Gets array ranks, one entry per jagged-array layer; rank one represents <c>[]</c>.</summary>
	public ImmutableArray<int> ArrayRanks { get; init; }

	/// <summary>Gets whether a nullable annotation is appended to the complete type.</summary>
	public bool IsNullable { get; init; }

	/// <summary>Gets whether a pointer suffix is appended.</summary>
	public bool IsPointer { get; init; }

	/// <summary>Returns this type with a nullable annotation.</summary>
	public TypeReferenceOptions Nullable() => this with { IsNullable = true };

	/// <summary>Returns this type with concrete generic arguments.</summary>
	public TypeReferenceOptions MakeGeneric(params TypeReferenceOptions[] arguments) =>
		this with
		{
			GenericArguments = [.. arguments],
			GenericArity = 0,
		};

	/// <summary>Returns this type as an array of the specified rank.</summary>
	public TypeReferenceOptions MakeArray(int rank = 1)
	{
		return rank < 1
			? throw new ArgumentOutOfRangeException(nameof(rank))
			: (this with { ArrayRanks = ArrayRanks.IsDefault ? [rank] : [.. ArrayRanks, rank] });
	}

	public static implicit operator TypeReferenceOptions(TypeValueObject type) =>
		type == TypeValueObject.Empty ? Empty : new(type);

	public static implicit operator TypeReferenceOptions?(TypeValueObject? type) =>
		type == null || type == TypeValueObject.Empty ? null : new(type);

	public static implicit operator TypeReferenceOptions?(Type? type) =>
		type == null ? null : new(type);

	public static implicit operator TypeReferenceOptions(Type type) => new(type);

	public static implicit operator TypeReferenceOptions?(string? type) =>
		type == null ? null : new(type);

	public static implicit operator TypeReferenceOptions(string type) => new(type);

	/// <summary>
	/// Represents the absence of a type reference. Code renderers and emitters ignore this value.
	/// </summary>
	public static readonly TypeReferenceOptions Empty;
}
