namespace Purview.SourceGeneratorFramework;

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
