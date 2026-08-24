using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>Describes a generated method, constructor, delegate, or primary-constructor parameter.</summary>
public readonly record struct ParameterDeclarationOptions
{
	/// <summary>Creates a parameter declaration.</summary>
	public ParameterDeclarationOptions(
		string name,
		TypeReferenceOptions reference,
		ParameterModifier modifier = ParameterModifier.None
	)
	{
		Name = name;
		Reference = reference;
		Modifier = modifier;
	}

	/// <summary>Gets the parameter name.</summary>
	public string Name { get; }

	/// <summary>Gets the parameter type.</summary>
	public TypeReferenceOptions Reference { get; }

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
