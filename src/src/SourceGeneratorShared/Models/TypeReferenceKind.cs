namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// Identifies what a <see cref="TypeReferenceOptions"/> refers to at its core, beneath any modifiers.
/// </summary>
public enum TypeReferenceKind
{
	/// <summary>No type. The default, uninitialised state.</summary>
	None = 0,

	/// <summary>A named type described by a <see cref="TypeValueObject"/>.</summary>
	Named = 1,

	/// <summary>An open generic parameter, identified by name.</summary>
	TypeParameter = 2,

	/// <summary>The <see langword="dynamic"/> type.</summary>
	Dynamic = 3,
}
