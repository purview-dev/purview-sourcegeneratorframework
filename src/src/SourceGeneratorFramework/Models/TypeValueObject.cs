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

		if (TypeHelpers.TryGetKeyword(type, out var keyword))
		{
			TypeName = keyword!;
			Namespace = null;
		}
		else
		{
			var metadataName = type.Name;
			var aritySeparator = metadataName.IndexOf('`');

			TypeName =
				aritySeparator < 0 ? metadataName : metadataName.Substring(0, aritySeparator);
			Namespace = type.Namespace;
			GenericArity = type.IsGenericType ? type.GetGenericArguments().Length : 0;
			TypeArguments =
				type.IsGenericType && !type.IsGenericTypeDefinition
					?
					[
						.. type.GetGenericArguments()
							.Select(static argument => new TypeValueObject(argument)),
					]
					: [];
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct.
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

		if (TypeHelpers.TryGetKeyword(typeSymbol.SpecialType, out var keyword))
		{
			TypeName = keyword!;
			Namespace = null;
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
		: this(specialType.ToString(), null)
	{
		if (!TypeHelpers.TryGetKeyword(specialType, out var keyword))
		{
			throw new ArgumentException(
				$"The provided special type '{specialType}' is not a recognized C# keyword type.",
				nameof(specialType)
			);
		}

		TypeName = keyword!;
	}

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
	public string MetadataFullName =>
		IsGlobalNamespace ? MetadataName : $"{Namespace}.{MetadataName}";

	/// <summary>
	/// Gets the full symbol name, including namespace when present.
	/// </summary>
	public string SymbolFullName => MetadataFullName;

	/// <summary>
	/// Gets the fully-qualified global name for use in generated code, rendered as an attribute when applicable.
	/// </summary>
	public string RenderFullName
	{
		get
		{
			var result = IsGlobalNamespace
				? RenderTypeName
				: $"global::{Namespace}.{RenderTypeName}";
			return TypeHelpers.IsAttribute(TypeName)
				? $"[{TypeHelpers.GetTypeName(result)}]"
				: result;
		}
	}

	/// <summary>
	/// Gets the type name suitable for use in generated code, trimming the 'Attribute' suffix when applicable.
	/// </summary>
	public string RenderTypeName
	{
		get
		{
			var typeName = TypeHelpers.IsAttribute(TypeName)
				? TypeHelpers.GetTypeName(TypeName)
				: TypeName;

			if (GenericArity == 0)
				return typeName;

			if (TypeArguments.IsDefaultOrEmpty)
				return $"{typeName}<{new string(',', GenericArity - 1)}>";

			// If the type has concrete type arguments, render them in angle brackets.
			return $"{typeName}<{string.Join(", ", TypeArguments.Select(static argument => argument.RenderFullName))}>";
		}
	}

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

		if (TypeName != other.Name || Namespace != otherNamespace)
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
		var otherTypeArgumentCount = other.TypeArguments.IsDefaultOrEmpty
			? 0
			: other.TypeArguments.Length;

		if (
			TypeName != other.TypeName
			|| Namespace != other.Namespace
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
	public static implicit operator string(TypeValueObject typeValueObject) =>
		typeValueObject.RenderFullName;

	/// <summary>
	/// Creates a generic variant of this type using the standard angle-bracket syntax.
	/// </summary>
	public TypeValueObject MakeGeneric(params string[] typeArguments)
	{
		if (typeArguments == null)
			throw new ArgumentNullException(nameof(typeArguments));

		// If the type has no generic arity, we can treat the provided type arguments as concrete types.
		return MakeGeneric(
			typeArguments.Select(static argument => new TypeValueObject(argument, null)).ToArray()
		);
	}

	/// <summary>
	/// Creates a constructed generic type using the specified type arguments.
	/// </summary>
	public TypeValueObject MakeGeneric(params TypeValueObject[] typeArguments)
	{
		if (typeArguments == null)
			throw new ArgumentNullException(nameof(typeArguments));

		if (typeArguments.Length == 0)
			throw new ArgumentException(
				"At least one type argument must be provided.",
				nameof(typeArguments)
			);

		if (GenericArity > 0 && typeArguments.Length != GenericArity)
		{
			throw new ArgumentException(
				$"Type '{MetadataFullName}' requires {GenericArity} type arguments, but {typeArguments.Length} were supplied.",
				nameof(typeArguments)
			);
		}

		// If the type has no generic arity, we can treat the provided type arguments as concrete types.
		return this with
		{
			GenericArity = GenericArity == 0 ? typeArguments.Length : GenericArity,
			TypeArguments = [.. typeArguments],
		};
	}

	/// <summary>
	/// Creates a generic variant of this type using curly-bracket syntax (for XML documentation).
	/// </summary>
	public TypeValueObject MakeGenericXml(params string[] typeArguments)
	{
		if (typeArguments == null)
			throw new ArgumentNullException(nameof(typeArguments));

		if (typeArguments.Length == 0)
			throw new ArgumentException(
				"At least one type argument must be provided.",
				nameof(typeArguments)
			);

		if (GenericArity > 0 && typeArguments.Length != GenericArity)
			throw new ArgumentException(
				$"Type '{MetadataFullName}' requires {GenericArity} type arguments, but {typeArguments.Length} were supplied.",
				nameof(typeArguments)
			);

		// If the type has no generic arity, we can treat the provided type arguments as concrete types.
		return new($"{TypeName}{{{string.Join(", ", typeArguments)}}}", Namespace);
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
