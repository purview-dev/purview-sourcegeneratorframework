using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Describes a generated indexer declaration.
/// </summary>
public readonly record struct IndexerDeclarationOptions
{
	/// <summary>
	/// Creates an indexer declaration.
	/// </summary>
	/// <param name="type">The indexer element type.</param>
	/// <param name="parameters">The indexer parameters.</param>
	public IndexerDeclarationOptions(TypeReference type, params ParameterDeclarationOptions[] parameters)
	{
		if (type.IsNullOrEmpty())
			throw new ArgumentException("Indexer type cannot be empty.", nameof(type));

		Type = type;
		Parameters = parameters is null ? [] : [.. parameters];
	}

	/// <summary>
	/// Gets the indexer element type.
	/// </summary>
	public TypeReference Type { get; }

	/// <summary>
	/// Gets the indexer parameters.
	/// </summary>
	public ImmutableArray<ParameterDeclarationOptions> Parameters { get; }

	/// <summary>
	/// Gets the optional accessibility modifier.
	/// </summary>
	public TypeDeclarationAccessibility? Accessibility { get; init; }

	/// <summary>
	/// Gets whether the indexer is static.
	/// </summary>
	public bool IsStatic { get; init; }

	/// <summary>
	/// Gets whether the indexer is abstract.
	/// </summary>
	public bool IsAbstract { get; init; }

	/// <summary>
	/// Gets whether the indexer is virtual.
	/// </summary>
	public bool IsVirtual { get; init; }

	/// <summary>
	/// Gets whether the indexer is an override.
	/// </summary>
	public bool IsOverride { get; init; }

	/// <summary>
	/// Gets whether the indexer is sealed.
	/// </summary>
	public bool IsSealed { get; init; }

	/// <summary>
	/// Gets whether a getter is emitted. The default is <see langword="true"/>.
	/// </summary>
	public bool HasGetter { get; init; } = true;

	/// <summary>
	/// Gets whether a setter or init accessor is emitted.
	/// </summary>
	public bool HasSetter { get; init; }

	/// <summary>
	/// Gets whether the setter is emitted as an init accessor.
	/// </summary>
	public bool IsInitOnly { get; init; }

	/// <summary>
	/// Gets optional getter accessibility.
	/// </summary>
	public TypeDeclarationAccessibility? GetterAccessibility { get; init; }

	/// <summary>
	/// Gets optional setter accessibility.
	/// </summary>
	public TypeDeclarationAccessibility? SetterAccessibility { get; init; }

	/// <summary>
	/// Gets an optional expression body without the leading <c>=&gt;</c>.
	/// </summary>
	public string? ExpressionBody { get; init; }

	/// <summary>
	/// Gets attributes applied to the indexer.
	/// </summary>
	public ImmutableArray<AttributeDeclarationOptions> Attributes { get; init; }

	/// <summary>
	/// Gets whether to emit generated attributes. When <see langword="null"/>, the value is inherited from
	/// <see cref="CodeWriter.DefaultIncludeGeneratedAttributes"/>.
	/// </summary>
	public bool? IncludeGeneratedAttributes { get; init; }
}
