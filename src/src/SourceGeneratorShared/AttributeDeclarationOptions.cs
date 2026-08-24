using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>Describes an attribute applied to a generated declaration.</summary>
public readonly record struct AttributeDeclarationOptions
{
	/// <summary>Creates an attribute declaration from a structured type reference.</summary>
	public AttributeDeclarationOptions(TypeReferenceOptions reference) => Reference = reference;

	/// <summary>Creates an attribute declaration from a structured type value.</summary>
	/// <remarks>
	/// <see cref="TypeValueObject.RenderAttributeName"/> includes surrounding square brackets. This
	/// constructor retains the rendered attribute name while removing those
	/// delimiters because the code writer supplies them.
	/// </remarks>
	public AttributeDeclarationOptions(TypeValueObject type)
		: this(type.AsTypeReference()) { }

	/// <summary>Gets the structured attribute type.</summary>
	public TypeReferenceOptions Reference { get; }

	/// <summary>Gets an optional target such as <c>return</c>, <c>field</c>, or <c>property</c>.</summary>
	public string? Target { get; init; }

	/// <summary>Gets structured attribute arguments.</summary>
	public ImmutableArray<AttributeArgumentOptions> Arguments { get; init; }
}
