namespace Purview.SourceGeneratorFramework;

/// <summary>Describes one argument supplied to a generated method call.</summary>
public readonly record struct MethodCallArgumentOptions
{
	/// <summary>Creates a method-call argument from its value expression.</summary>
	/// <param name="value">The argument expression or variable name.</param>
	/// <param name="name">An optional named-argument label.</param>
	/// <param name="modifier">The argument passing modifier.</param>
	public MethodCallArgumentOptions(
		string value,
		string? name = null,
		ParameterModifier modifier = ParameterModifier.None
	)
	{
		Value = string.IsNullOrWhiteSpace(value)
			? throw new ArgumentException("Argument value cannot be null or whitespace.", nameof(value))
			: value;
		Name = name;
		Modifier = modifier;
	}

	/// <summary>
	/// Creates a method-call argument from its value expression, with a specified argument passing modifier.
	/// </summary>
	/// <param name="value">The argument expression or variable name.</param>
	/// <param name="modifier">The argument passing modifier.</param>
	public MethodCallArgumentOptions(string value, ParameterModifier modifier)
		: this(value, null, modifier) { }

	/// <summary>Gets the argument expression or variable name.</summary>
	public string Value { get; }

	/// <summary>Gets an optional named-argument label.</summary>
	public string? Name { get; init; }

	/// <summary>Gets the argument passing modifier.</summary>
	public ParameterModifier Modifier { get; init; }

	public static implicit operator MethodCallArgumentOptions(string value) => new(value);
}
