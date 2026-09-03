using System.ComponentModel;

namespace Purview.SourceGeneratorFramework;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeIdentityExtensions
{
	/// <summary>
	/// Returns a fully qualified reference to a static member on the specified type.
	/// </summary>
	/// <param name="typeIdentity">The type that declares the static member.</param>
	/// <param name="memberName">The static field, property, method, or nested-type name.</param>
	/// <returns>A C# expression in the form <c>global::Namespace.Type.Member</c>.</returns>
	public static string StaticMember(this in TypeIdentity typeIdentity, string memberName)
	{
		if (typeIdentity == TypeIdentity.Empty)
			throw new ArgumentException("The declaring type cannot be empty.", nameof(typeIdentity));
		if (string.IsNullOrWhiteSpace(memberName))
			throw new ArgumentException("Member name cannot be null or whitespace.", nameof(memberName));

		// The RenderFullName property already includes the global:: prefix and the namespace, so we can just append the member name.
		return $"{typeIdentity.RenderFullName}.{memberName}";
	}
}
