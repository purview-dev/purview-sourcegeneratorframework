using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;

namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// Represents a simple type/value descriptor used during source generation.
/// </summary>
public readonly record struct TypeValueObject
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct.
	/// </summary>
	public TypeValueObject(string typeName, string? @namespace)
	{
		TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
		Namespace = @namespace;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TypeValueObject"/> struct from a Roslyn type symbol.
	/// </summary>
	public TypeValueObject(ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		TypeName = typeSymbol.Name;
		Namespace = typeSymbol.ContainingNamespace.IsGlobalNamespace
			? null
			: typeSymbol.ContainingNamespace.ToDisplayString();
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
	/// Gets the full symbol name, including namespace when present.
	/// </summary>
	public string SymbolFullName => IsGlobalNamespace ? TypeName : $"{Namespace}.{TypeName}";

	/// <summary>
	/// Gets the fully-qualified global name for use in generated code, rendered as an attribute when applicable.
	/// </summary>
	public string RenderFullName
	{
		get
		{
			var result = IsGlobalNamespace ? TypeName : $"global::{Namespace}.{RenderTypeName}";
			return TypeHelpers.IsAttribute(TypeName)
				? $"[{TypeHelpers.GetTypeName(result)}]"
				: result;
		}
	}

	/// <summary>
	/// Gets the type name suitable for use in generated code, trimming the 'Attribute' suffix when applicable.
	/// </summary>
	public string RenderTypeName =>
		TypeHelpers.IsAttribute(TypeName) ? TypeHelpers.GetTypeName(TypeName) : TypeName;

	/// <summary>
	/// Gets a value indicating whether the type is in the global namespace.
	/// </summary>
	public bool IsGlobalNamespace => Namespace is null;

	/// <summary>
	/// Returns the rendered full name.
	/// </summary>
	public override string ToString() => RenderFullName;

	/// <summary>
	/// Implicitly converts a <see cref="TypeValueObject"/> to its rendered full name.
	/// </summary>
	public static implicit operator string(TypeValueObject typeValueObject) =>
		typeValueObject.RenderFullName;

	/// <summary>
	/// Creates a generic variant of this type using the standard angle-bracket syntax.
	/// </summary>
	public TypeValueObject MakeGeneric(params string[] typeArguments) =>
		typeArguments == null
			? throw new ArgumentNullException(nameof(typeArguments))
			: MakeGeneric('<', '>', typeArguments);

	/// <summary>
	/// Creates a generic variant of this type using curly-bracket syntax (for XML documentation).
	/// </summary>
	public TypeValueObject MakeGenericXml(params string[] typeArguments) =>
		typeArguments == null
			? throw new ArgumentNullException(nameof(typeArguments))
			: MakeGeneric('{', '}', typeArguments);

	TypeValueObject MakeGeneric(char start, char end, string[] typeArguments)
	{
		if (typeArguments.Length == 0)
			throw new ArgumentException(
				"At least one type argument must be provided.",
				nameof(typeArguments)
			);

		var typeArgs = string.Join(", ", typeArguments.Select(arg => arg));
		var fullTypeName = $"{TypeName}{start}{typeArgs}{end}";
		return new(fullTypeName, Namespace);
	}

	/// <summary>
	/// Gets an empty <see cref="TypeValueObject"/>.
	/// </summary>
	public static readonly TypeValueObject Empty;
}
