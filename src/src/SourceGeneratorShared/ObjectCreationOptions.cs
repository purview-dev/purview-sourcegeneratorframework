using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>Describes a generated object-creation expression.</summary>
public readonly record struct ObjectCreationOptions
{
	/// <summary>Creates an object-creation expression.</summary>
	/// <param name="reference">The type to instantiate.</param>
	/// <param name="arguments">The constructor arguments; strings are implicitly supported.</param>
	public ObjectCreationOptions(TypeReference reference, params MethodCallArgumentOptions[] arguments)
	{
		if (reference.IsNullOrEmpty())
			throw new ArgumentException("Object-creation type cannot be empty.", nameof(reference));

		Reference = reference;
		Arguments = arguments is null ? [] : [.. arguments];
	}

	/// <summary>Gets the type to instantiate.</summary>
	public TypeReference Reference { get; }

	/// <summary>Gets the constructor arguments.</summary>
	public ImmutableArray<MethodCallArgumentOptions> Arguments { get; }

	/// <summary>Gets whether constructor arguments are written one per line.</summary>
	public bool WriteArgumentsOnSeparateLines { get; init; }
}
