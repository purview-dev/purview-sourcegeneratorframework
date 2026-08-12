using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Describes an ordinary instance or static constructor declaration.
/// </summary>
public readonly record struct ConstructorDeclarationOptions
{
	/// <summary>
	/// Initializes a constructor declaration description.
	/// </summary>
	/// <param name="typeName">The name of the containing type without generic parameters.</param>
	public ConstructorDeclarationOptions(string typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
			throw new ArgumentException(
				"Type name cannot be null or whitespace.",
				nameof(typeName)
			);

		TypeName = typeName;
	}

	/// <summary>Gets the containing type name without generic parameters.</summary>
	public string TypeName { get; }

	/// <summary>
	/// Gets the optional accessibility modifier, or <see langword="null"/> to omit accessibility.
	/// </summary>
	public TypeDeclarationAccessibility? Accessibility { get; init; }

	/// <summary>Gets whether a static constructor is emitted.</summary>
	/// <remarks>Static constructors cannot declare parameters or an initializer.</remarks>
	public bool IsStatic { get; init; }

	/// <summary>Gets the constructor parameters.</summary>
	/// <remarks>Each entry is emitted verbatim as a complete parameter declaration.</remarks>
	public ImmutableArray<ParameterDeclarationOptions> Parameters { get; init; }

	/// <summary>Gets attributes applied to the constructor.</summary>
	public ImmutableArray<AttributeDeclarationOptions> Attributes { get; init; }

	/// <summary>
	/// Gets whether the parameters are written on separate lines, with each parameter indented.
	/// </summary>
	public bool WriteParametersOnSeparateLines { get; init; }

	/// <summary>Gets the optional constructor initializer without the leading colon.</summary>
	/// <example><c>base(connectionString)</c> or <c>this("Default")</c>.</example>
	public string? Initializer { get; init; }
}
