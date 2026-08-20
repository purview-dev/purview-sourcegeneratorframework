using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Describes C# type syntax without requiring callers to assemble nullable or generic text.
/// This is the canonical type representation for source-generation pipelines.
/// </summary>
public readonly record struct TypeReferenceOptions
{
	/// <summary>Creates a type reference from a required framework type value.</summary>
	public TypeReferenceOptions(TypeValueObject type)
	{
		if (type == TypeValueObject.Empty)
		{
			this = Empty;
			return;
		}

		var name = RenderBaseTypeName(type);
		Name = string.IsNullOrWhiteSpace(name)
			? throw new ArgumentException("The type value must provide a non-empty rendered name.", nameof(type))
			: name;
		TypeValue = type;
		GenericArguments = type.TypeArguments.IsDefaultOrEmpty
			? []
			: [.. type.TypeArguments.Select(static argument => new TypeReferenceOptions(argument))];
		GenericArity = GenericArguments.IsDefaultOrEmpty ? type.GenericArity : 0;
	}

	/// <summary>Creates a type reference from a runtime type.</summary>
	public TypeReferenceOptions(Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		this = CreateFromRuntimeType(type);
	}

	/// <summary>Creates a type reference from a Roslyn symbol.</summary>
	public TypeReferenceOptions(ITypeSymbol type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		this = CreateFromSymbol(type);
	}

	/// <summary>Gets the named type, predefined keyword, tuple element list, or generic parameter.</summary>
	public string Name { get; }

	/// <summary>
	/// Gets the required semantic type value represented by this reference.
	/// </summary>
	/// <remarks>
	/// <see cref="TypeValueObject.Empty"/> is used only by <see cref="Empty"/>.
	/// </remarks>
	public TypeValueObject TypeValue { get; private init; }

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

	/// <summary>Returns this type as a pointer type.</summary>
	public TypeReferenceOptions MakePointer() => this with { IsPointer = true };

	/// <summary>
	/// Determines whether this unmodified reference represents the specified semantic type value.
	/// </summary>
	public bool Equals(TypeValueObject other) =>
		!IsEmpty
		&& !IsNullable
		&& !IsPointer
		&& ArrayRanks.IsDefaultOrEmpty
		&& GenericArguments.IsDefaultOrEmpty
		&& GenericArity == 0
		&& TypeValue.Equals(other);

	/// <summary>Gets the type rendered as valid C# type syntax.</summary>
	public string RenderTypeName
	{
		get
		{
			if (IsEmpty)
				return string.Empty;

			StringBuilder builder = new(Name);
			if (!GenericArguments.IsDefaultOrEmpty)
			{
				builder.Append('<');
				for (var index = 0; index < GenericArguments.Length; index++)
				{
					if (index > 0)
						builder.Append(", ");
					builder.Append(GenericArguments[index].RenderTypeName);
				}
				builder.Append('>');
			}
			else if (GenericArity > 0)
			{
				builder.Append('<').Append(',', GenericArity - 1).Append('>');
			}

			for (var index = 0; !ArrayRanks.IsDefaultOrEmpty && index < ArrayRanks.Length; index++)
				builder.Append('[').Append(',', ArrayRanks[index] - 1).Append(']');

			if (IsPointer)
				builder.Append('*');
			if (IsNullable)
				builder.Append('?');

			return builder.ToString();
		}
	}

	/// <summary>Gets this type rendered as C# attribute syntax without the optional <c>Attribute</c> suffix.</summary>
	public string RenderAttributeName
	{
		get
		{
			var rendered = RenderTypeName;
			var genericStart = rendered.IndexOf('<');
			var baseName = genericStart < 0 ? rendered : rendered.Substring(0, genericStart);
			var suffix = "Attribute";
			if (!baseName.EndsWith(suffix, StringComparison.Ordinal))
				return rendered;

			baseName = baseName.Substring(0, baseName.Length - suffix.Length);
			return genericStart < 0 ? baseName : baseName + rendered.Substring(genericStart);
		}
	}

	/// <summary>Returns the type rendered as valid C# type syntax.</summary>
	public override string ToString() => RenderTypeName;

	public static implicit operator TypeReferenceOptions(TypeValueObject type) =>
		type == TypeValueObject.Empty ? Empty : new(type);

	public static implicit operator TypeReferenceOptions?(Type? type) => type == null ? null : new(type);

	public static implicit operator TypeReferenceOptions(Type type) => new(type);

	/// <summary>Implicitly converts a structured type reference to rendered C# type syntax.</summary>
	public static implicit operator string(TypeReferenceOptions type) => type.RenderTypeName;

	/// <summary>
	/// Represents the absence of a type reference. Code renderers and emitters ignore this value.
	/// </summary>
	public static readonly TypeReferenceOptions Empty;

	static TypeReferenceOptions CreateFromRuntimeType(Type type)
	{
		if (type.IsByRef)
			return CreateFromRuntimeType(type.GetElementType());

		if (type.IsArray)
			return CreateFromRuntimeType(type.GetElementType()).MakeArray(type.GetArrayRank());

		if (type.IsPointer)
			return CreateFromRuntimeType(type.GetElementType()).MakePointer();

		return new TypeReferenceOptions(new TypeValueObject(type));
	}

	static TypeReferenceOptions CreateFromSymbol(ITypeSymbol type)
	{
		if (type is IArrayTypeSymbol array)
		{
			var arrayReference = CreateFromSymbol(array.ElementType).MakeArray(array.Rank);
			return type.NullableAnnotation == NullableAnnotation.Annotated ? arrayReference.Nullable() : arrayReference;
		}

		if (type is IPointerTypeSymbol pointer)
			return CreateFromSymbol(pointer.PointedAtType).MakePointer();

		var reference = new TypeReferenceOptions(new TypeValueObject(type));
		if (type is INamedTypeSymbol named && named.IsGenericType)
		{
			reference = reference with
			{
				GenericArguments = [.. named.TypeArguments.Select(CreateFromSymbol)],
				GenericArity = named.IsUnboundGenericType ? named.Arity : 0,
			};
		}
		return type.NullableAnnotation == NullableAnnotation.Annotated ? reference.Nullable() : reference;
	}

	static string RenderBaseTypeName(TypeValueObject type) =>
		type.SpecialType != SpecialType.None
			? type.Keyword!
			: type.IsGlobalNamespace
				? type.TypeName
				: $"global::{type.Namespace}.{type.TypeName}";
}
