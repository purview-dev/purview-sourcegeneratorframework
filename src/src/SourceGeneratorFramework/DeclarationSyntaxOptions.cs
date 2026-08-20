using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>Describes an attribute applied to a generated declaration.</summary>
public readonly record struct AttributeDeclarationOptions
{
	/// <summary>Creates an attribute declaration from a structured type reference.</summary>
	public AttributeDeclarationOptions(TypeReferenceOptions type) => Type = type;

	/// <summary>Creates an attribute declaration from a structured type value.</summary>
	/// <remarks>
	/// <see cref="TypeValueObject.RenderAttributeName"/> includes surrounding square brackets. This
	/// constructor retains the rendered attribute name while removing those
	/// delimiters because the code writer supplies them.
	/// </remarks>
	public AttributeDeclarationOptions(TypeValueObject type)
		: this(type.AsTypeReference()) { }

	/// <summary>Gets the structured attribute type.</summary>
	public TypeReferenceOptions Type { get; }

	/// <summary>Gets an optional target such as <c>return</c>, <c>field</c>, or <c>property</c>.</summary>
	public string? Target { get; init; }

	/// <summary>Gets structured attribute arguments.</summary>
	public ImmutableArray<AttributeArgumentOptions> Arguments { get; init; }
}

/// <summary>Describes one positional or named attribute argument.</summary>
public readonly record struct AttributeArgumentOptions
{
	/// <summary>Creates a positional attribute argument.</summary>
	public AttributeArgumentOptions(string value, string? name = null, bool isPropertyAssignment = false) =>
		(Value, Name, IsPropertyAssignment) = (value, name, isPropertyAssignment);

	/// <summary>Creates a positional Boolean attribute argument using a valid C# literal.</summary>
	public AttributeArgumentOptions(bool value, string? name = null, bool isPropertyAssignment = false) =>
		(Value, Name, IsPropertyAssignment) = (value ? "true" : "false", name, isPropertyAssignment);

	/// <summary>Gets the argument expression.</summary>
	public string Value { get; }

	/// <summary>Gets an optional constructor parameter or property name.</summary>
	public string? Name { get; init; }

	/// <summary>Gets whether a named argument uses property assignment (<c>=</c>) instead of constructor naming (<c>:</c>).</summary>
	public bool IsPropertyAssignment { get; init; }
}

/// <summary>Identifies a generated parameter modifier.</summary>
public enum ParameterModifier
{
	/// <summary>No modifier.</summary>
	None,

	/// <summary>The <c>ref</c> modifier.</summary>
	Ref,

	/// <summary>The <c>out</c> modifier.</summary>
	Out,

	/// <summary>The <c>in</c> modifier.</summary>
	In,

	/// <summary>The <c>ref readonly</c> modifier.</summary>
	RefReadOnly,
}

/// <summary>Describes one argument supplied to a generated method call.</summary>
public readonly record struct MethodCallArgumentOptions
{
	/// <summary>Creates a method-call argument from its value expression.</summary>
	/// <param name="value">The argument expression or variable name.</param>
	public MethodCallArgumentOptions(string value) =>
		Value = string.IsNullOrWhiteSpace(value)
			? throw new ArgumentException("Argument value cannot be null or whitespace.", nameof(value))
			: value;

	/// <summary>Gets the argument expression or variable name.</summary>
	public string Value { get; }

	/// <summary>Gets an optional named-argument label.</summary>
	public string? Name { get; init; }

	/// <summary>Gets the argument passing modifier.</summary>
	public ParameterModifier Modifier { get; init; }
}

/// <summary>Describes a generated method, constructor, delegate, or primary-constructor parameter.</summary>
public readonly record struct ParameterDeclarationOptions
{
	/// <summary>Creates a parameter declaration.</summary>
	public ParameterDeclarationOptions(string name, TypeReferenceOptions type)
	{
		Name = name;
		Type = type;
	}

	/// <summary>Gets the parameter name.</summary>
	public string Name { get; }

	/// <summary>Gets the parameter type.</summary>
	public TypeReferenceOptions Type { get; }

	/// <summary>Gets the parameter passing modifier.</summary>
	public ParameterModifier Modifier { get; init; }

	/// <summary>Gets whether <c>this</c> is emitted for an extension receiver.</summary>
	public bool IsThis { get; init; }

	/// <summary>Gets whether <c>params</c> is emitted.</summary>
	public bool IsParams { get; init; }

	/// <summary>Gets whether <c>scoped</c> is emitted.</summary>
	public bool IsScoped { get; init; }

	/// <summary>Gets an optional default-value expression.</summary>
	public string? DefaultValue { get; init; }

	/// <summary>Gets attributes applied to the parameter.</summary>
	public ImmutableArray<AttributeDeclarationOptions> Attributes { get; init; }
}
