using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Represents information about a resolved type, including its reference and declared accessibility.
/// </summary>
/// <param name="Reference">The reference to the resolved type.</param>
/// <param name="DeclaredAccessibility">The declared accessibility of the resolved type.</param>
public readonly record struct ResolvedTypeInformation(TypeReference Reference, Accessibility DeclaredAccessibility)
{
	/// <summary>
	/// Gets the type value object associated with the resolved type reference.
	/// </summary>
	/// <remarks>
	/// Retrieved from <see cref="TypeReference.Identity" />
	/// </remarks>
	public TypeIdentity Type => Reference.Identity;
}
