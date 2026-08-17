using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// Represents a simple type/value descriptor used during source generation.
/// </summary>
public readonly record struct TypeValueObject : IEquatable<ITypeSymbol>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct from a <see cref="Type"/>.
	/// </summary>
	/// <param name="type">The type to initialize the value object from.</param>
	/// <exception cref="ArgumentNullException">Thrown when the provided type is null.</exception>
	public TypeValueObject(Type type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		var knownType = KnownLangTypes.Get(type);
		if (knownType != TypeMapping.Empty)
		{
			TypeName = knownType.Type.Name;
			Namespace = knownType.Type.Namespace;
			Keyword = knownType.Keyword;
			SpecialType = knownType.SpecialType;
		}
		else
		{
			var metadataName = type.Name;
			var aritySeparator = metadataName.IndexOf('`');

			TypeName = aritySeparator < 0 ? metadataName : metadataName.Substring(0, aritySeparator);
			Namespace = type.Namespace;
			GenericArity = type.IsGenericType ? type.GetGenericArguments().Length : 0;
			TypeArguments =
				type.IsGenericType && !type.IsGenericTypeDefinition
					? [.. type.GetGenericArguments().Select(static argument => new TypeValueObject(argument))]
					: [];
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct.
	/// <para>
	/// <b>Note:</b> This constructor does not validate the provided type name or namespace. It is the caller's responsibility to ensure that the values are valid and represent a real type.
	/// If this is a known C# keyword type, consider using the <see cref="TypeValueObject(SpecialType)"/>, <see cref="TypeValueObject(ITypeSymbol)"/>, or <see cref="TypeValueObject(Type)"/> constructors instead.
	/// </para>
	/// <para>
	///	Also beware that this constructor does not handle generic types. If you need to represent a generic type, use the <see cref="TypeValueObject(ITypeSymbol)"/> or <see cref="TypeValueObject(Type)"/> constructors,
	///	or the <see cref="MakeGeneric(TypeValueObject[])"/> method after construction.
	/// </para>
	/// </summary>
	public TypeValueObject(string typeName, string? @namespace)
	{
		TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
		Namespace = @namespace;
		GenericArity = 0;
		TypeArguments = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct from a Roslyn type symbol.
	/// </summary>
	public TypeValueObject(ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		var knownType = KnownLangTypes.Get(typeSymbol.SpecialType);
		if (knownType != TypeMapping.Empty)
		{
			TypeName = knownType.Type.Name;
			Namespace = knownType.Type.Namespace;
			Keyword = knownType.Keyword;
			SpecialType = knownType.SpecialType;
		}
		else
		{
			TypeName = typeSymbol.Name;
			Namespace = typeSymbol.ContainingNamespace.IsGlobalNamespace
				? null
				: typeSymbol.ContainingNamespace.ToDisplayString();

			if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
			{
				GenericArity = namedType.Arity;
				TypeArguments = IsGenericDefinition(namedType)
					? []
					:
					[
						.. namedType.TypeArguments.Select(static argument =>
						{
							return TypeHelpers.IsKeywordType(argument)
								? new(argument.SpecialType)
								: new TypeValueObject(argument);
						}),
					];
			}
			else
			{
				GenericArity = 0;
				TypeArguments = [];
			}
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct from a recognized C# keyword special type.
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

		TypeName = knownType.Type.Name;
		Namespace = knownType.Type.Namespace;
		Keyword = knownType.Keyword;
		SpecialType = knownType.SpecialType;
	}

	/// <summary>
	/// Gets the recognized C# keyword special type, or <see cref="SpecialType.None"/> if the type is not a recognized keyword type.
	/// </summary>
	public SpecialType SpecialType { get; init; } = SpecialType.None;

	/// <summary>
	/// Gets the C# keyword for the type, or <see langword="null"/> if the type does not have a keyword representation.
	/// </summary>
	public string? Keyword { get; init; }

	/// <summary>
	/// Gets the type name without its namespace.
	/// </summary>
	public string TypeName { get; init; }

	/// <summary>
	/// Gets the namespace, or <see langword="null"/> if the type is in the global namespace.
	/// </summary>
	public string? Namespace { get; init; }

	/// <summary>
	/// Gets the number of generic parameters declared by the type.
	/// </summary>
	public int GenericArity { get; init; }

	/// <summary>
	/// Gets the concrete generic type arguments for a constructed type.
	/// </summary>
	/// <remarks>
	/// This collection is empty for a non-generic type and for an open generic type definition.
	/// Use <see cref="GenericArity"/> to distinguish those cases.
	/// </remarks>
	public ImmutableArray<TypeValueObject> TypeArguments { get; init; }

	/// <summary>
	/// Gets a value indicating whether this value represents an open generic type definition.
	/// </summary>
	public bool IsGenericTypeDefinition => GenericArity > 0 && TypeArguments.IsDefaultOrEmpty;

	/// <summary>
	/// Gets the CLR metadata name, including the generic arity suffix when required.
	/// </summary>
	public string MetadataName => GenericArity == 0 ? TypeName : $"{TypeName}`{GenericArity}";

	/// <summary>
	/// Gets the namespace-qualified CLR metadata name used by Roslyn type lookup.
	/// </summary>
	public string MetadataFullName => IsGlobalNamespace ? MetadataName : $"{Namespace}.{MetadataName}";

	/// <summary>
	/// Gets the fully-qualified global type name for use in generated code.
	/// </summary>
	public string RenderFullName
	{
		get
		{
			if (SpecialType != SpecialType.None)
				return Keyword!;

			// If the type is in the global namespace, we can render it without the "global::" prefix.
			return IsGlobalNamespace ? RenderTypeName : $"global::{Namespace}.{RenderTypeName}";
		}
	}

	/// <summary>
	/// Gets the type name suitable for use in generated code.
	/// </summary>
	public string RenderTypeName
	{
		get
		{
			if (SpecialType != SpecialType.None)
				return Keyword!;

			if (GenericArity == 0)
				return TypeName;

			if (TypeArguments.IsDefaultOrEmpty)
				return $"{TypeName}<{new string(',', GenericArity - 1)}>";

			// If the type has concrete type arguments, render them in angle brackets.
			return $"{TypeName}<{string.Join(", ", TypeArguments.Select(static argument => argument.RenderFullName))}>";
		}
	}

	/// <summary>
	/// Gets the fully-qualified name rendered as a C# attribute application, including brackets and
	/// the optional omission of the <c>Attribute</c> suffix.
	/// </summary>
	public string RenderAttributeName => $"[{TypeHelpers.GetTypeName(RenderFullName)}]";

	/// <summary>
	/// Gets a value indicating whether the type is in the global namespace.
	/// </summary>
	public bool IsGlobalNamespace => Namespace is null;

	/// <summary>
	/// Returns the rendered full name.
	/// </summary>
	public override string ToString() => RenderFullName;

	/// <summary>
	/// Determines whether the specified <see cref="ITypeSymbol"/> is equal to the current <see cref="TypeValueObject"/>.
	/// </summary>
	/// <param name="other">The type symbol to compare with the current type value object.</param>
	/// <returns><see langword="true"/> if the specified type symbol is equal to the current type value object; otherwise, <see langword="false"/>.</returns>
	public bool Equals(ITypeSymbol? other)
	{
		if (other is null)
			return false;

		var otherNamespace = other.ContainingNamespace.IsGlobalNamespace
			? null
			: other.ContainingNamespace.ToDisplayString();

		if (TypeName != other.Name || Namespace != otherNamespace || SpecialType != other.SpecialType)
			return false;

		if (other is not INamedTypeSymbol namedType)
			return GenericArity == 0;

		if (GenericArity != namedType.Arity)
			return false;

		// An open definition represents every constructed form of that definition.
		if (TypeArguments.IsDefaultOrEmpty)
			return true;

		// If the current value has concrete type arguments, ensure they match the other type's arguments.
		return TypeArguments.Length == namedType.TypeArguments.Length
			&& TypeArguments
				.Zip(namedType.TypeArguments, static (expected, actual) => expected.Equals(actual))
				.All(static equal => equal);
	}

	/// <summary>
	/// Determines whether the specified value represents the same type.
	/// </summary>
	public bool Equals(TypeValueObject other)
	{
		var typeArgumentCount = TypeArguments.IsDefaultOrEmpty ? 0 : TypeArguments.Length;
		var otherTypeArgumentCount = other.TypeArguments.IsDefaultOrEmpty ? 0 : other.TypeArguments.Length;

		if (
			TypeName != other.TypeName
			|| Namespace != other.Namespace
			|| SpecialType != other.SpecialType
			|| Keyword != other.Keyword
			|| GenericArity != other.GenericArity
			|| typeArgumentCount != otherTypeArgumentCount
		)
			return false;

		for (var index = 0; index < typeArgumentCount; index++)
		{
			if (!TypeArguments[index].Equals(other.TypeArguments[index]))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Returns a structural hash code for this type and its generic arguments.
	/// </summary>
	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = TypeName?.GetHashCode() ?? 0;
			hashCode = (hashCode * 397) ^ (Namespace?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ SpecialType.GetHashCode();
			hashCode = (hashCode * 397) ^ (Keyword?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ GenericArity;

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

	/// <summary>Creates structured declaration syntax for this type.</summary>
	public TypeReferenceOptions AsTypeReference() => new(this);

	/// <summary>Creates a nullable structured type reference.</summary>
	public TypeReferenceOptions MakeNullable() => AsTypeReference().Nullable();

	/// <summary>Creates an array structured type reference with the specified rank.</summary>
	public TypeReferenceOptions MakeArray(int rank = 1) => AsTypeReference().MakeArray(rank);

	/// <summary>Creates a pointer structured type reference.</summary>
	public TypeReferenceOptions MakePointer() => AsTypeReference().MakePointer();

	/// <summary>
	/// Creates a generic variant of this type using the standard angle-bracket syntax.
	/// </summary>
	public TypeValueObject MakeGeneric(params string[] typeArguments)
	{
		if (typeArguments == null)
			throw new ArgumentNullException(nameof(typeArguments));

		// If the type has no generic arity, we can treat the provided type arguments as concrete types.
		return MakeGeneric(typeArguments.Select(static argument => new TypeValueObject(argument, null)).ToArray());
	}

	/// <summary>
	/// Creates a constructed generic type using the specified type arguments.
	/// </summary>
	public TypeValueObject MakeGeneric(params TypeValueObject[] typeArguments)
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
		{
			throw new InvalidOperationException($"Cannot create a generic type from the special type '{SpecialType}'.");
		}

		// If the type has no generic arity, we can treat the provided type arguments as concrete types.
		return this with
		{
			GenericArity = GenericArity == 0 ? typeArguments.Length : GenericArity,
			TypeArguments = [.. typeArguments],
		};
	}

	/// <summary>
	/// Gets an empty <see cref="TypeValueObject"/>.
	/// </summary>
	public static readonly TypeValueObject Empty;

	/// <summary>
	/// Creates a <see cref="TypeValueObject"/> from a generic type parameter.
	/// </summary>
	/// <typeparam name="T">The type parameter.</typeparam>
	/// <returns>A <see cref="TypeValueObject"/> representing the type parameter.</returns>
	public static TypeValueObject Create<T>() => new(typeof(T));

	static bool IsGenericDefinition(INamedTypeSymbol typeSymbol) =>
		typeSymbol.IsUnboundGenericType
		|| SymbolEqualityComparer.Default.Equals(typeSymbol, typeSymbol.OriginalDefinition);
}
