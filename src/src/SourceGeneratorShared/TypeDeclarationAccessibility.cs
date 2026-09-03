namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Identifies an optional C# accessibility modifier for a generated type.
/// </summary>
public enum TypeDeclarationAccessibility
{
	/// <summary>
	/// The <c>public</c> accessibility modifier.
	/// </summary>
	Public,

	/// <summary>
	/// The <c>internal</c> accessibility modifier.
	/// </summary>
	Internal,

	/// <summary>
	/// The <c>protected</c> accessibility modifier.
	/// </summary>
	Protected,

	/// <summary>
	/// The <c>private</c> accessibility modifier.
	/// </summary>
	Private,

	/// <summary>
	/// The <c>protected internal</c> accessibility modifier.
	/// </summary>
	ProtectedInternal,

	/// <summary>
	/// The <c>private protected</c> accessibility modifier.
	/// </summary>
	PrivateProtected,

	/// <summary>
	/// The <c>file</c> accessibility modifier.
	/// </summary>
	File,
}
