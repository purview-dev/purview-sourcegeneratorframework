using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>Describes an attribute applied to a generated declaration.</summary>
public readonly record struct AttributeDeclarationOptions
{
	/// <summary>Creates an attribute declaration from a structured type reference.</summary>
	public AttributeDeclarationOptions(TypeReference reference) => Reference = reference;

	/// <summary>Creates an attribute declaration from a structured type value.</summary>
	public AttributeDeclarationOptions(TypeIdentity type)
		: this(type.AsTypeReference()) { }

	/// <summary>Gets the structured attribute type.</summary>
	public TypeReference Reference { get; }

	/// <summary>Gets an optional target such as <c>return</c>, <c>field</c>, or <c>property</c>.</summary>
	public string? Target { get; init; }

	/// <summary>Gets structured attribute arguments.</summary>
	public ImmutableArray<AttributeArgumentOptions> Arguments { get; init; }
}
