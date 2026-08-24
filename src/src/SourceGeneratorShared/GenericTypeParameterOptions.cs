using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Describes one generic type parameter and its optional C# constraints.
/// </summary>
public sealed record GenericTypeParameterOptions
{
	/// <summary>
	/// Initializes a generic type parameter description.
	/// </summary>
	/// <param name="name">The type parameter name.</param>
	public GenericTypeParameterOptions(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Type parameter name cannot be null or whitespace.", nameof(name));

		Name = name;
	}

	/// <summary>
	/// Gets the type parameter name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the ordered constraint expressions written after <c>where T :</c>.
	/// </summary>
	/// <remarks>
	/// Entries are emitted verbatim and may contain values such as <c>class</c>, <c>notnull</c>,
	/// a base type, an interface, or <c>new()</c>.
	/// </remarks>
	public ImmutableArray<string> Constraints { get; init; } = [];

	public static implicit operator GenericTypeParameterOptions(string name) => new(name);
}
