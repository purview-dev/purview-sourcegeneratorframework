namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Identifies the C# declaration emitted for a generated type.
/// </summary>
public enum TypeDeclarationKind
{
	/// <summary>
	/// A class declaration.
	/// </summary>
	Class,

	/// <summary>
	/// A struct declaration.
	/// </summary>
	Struct,

	/// <summary>
	/// A record class declaration.
	/// </summary>
	RecordClass,

	/// <summary>
	/// A record struct declaration.
	/// </summary>
	RecordStruct,

	/// <summary>
	/// An interface declaration.
	/// </summary>
	Interface,

	/// <summary>
	/// An enum declaration.
	/// </summary>
	Enum,

	/// <summary>
	/// A delegate declaration.
	/// </summary>
	Delegate,
}
