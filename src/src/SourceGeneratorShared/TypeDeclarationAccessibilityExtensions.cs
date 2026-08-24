using System.ComponentModel;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeDeclarationAccessibilityExtensions
{
	/// <summary>
	/// Converts a Roslyn <see cref="Accessibility"/> value to the corresponding
	/// <see cref="TypeDeclarationAccessibility"/> value.
	/// </summary>
	/// <param name="accessibility">The Roslyn accessibility value.</param>
	/// <returns>
	/// The corresponding declaration accessibility, or <see langword="null"/> when Roslyn reports
	/// <see cref="Accessibility.NotApplicable"/> or an unknown future value.
	/// </returns>
	/// <remarks>This method never throws for an accessibility value.</remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases")]
	public static TypeDeclarationAccessibility? ToTypeDeclarationAccessibility(this Accessibility accessibility) =>
		accessibility switch
		{
			Accessibility.Private => TypeDeclarationAccessibility.Private,
			Accessibility.ProtectedAndInternal => TypeDeclarationAccessibility.PrivateProtected,
			Accessibility.Protected => TypeDeclarationAccessibility.Protected,
			Accessibility.Internal => TypeDeclarationAccessibility.Internal,
			Accessibility.ProtectedOrInternal => TypeDeclarationAccessibility.ProtectedInternal,
			Accessibility.Public => TypeDeclarationAccessibility.Public,
			_ => null,
		};

	/// <summary>
	/// Converts a declaration accessibility value to the corresponding Roslyn
	/// <see cref="Accessibility"/> value.
	/// </summary>
	/// <param name="accessibility">The declaration accessibility value.</param>
	/// <returns>
	/// The corresponding Roslyn accessibility, or <see cref="Accessibility.NotApplicable"/> for
	/// <see cref="TypeDeclarationAccessibility.File"/> or an unknown future value.
	/// </returns>
	/// <remarks>
	/// Roslyn represents file-local accessibility separately from <see cref="Accessibility"/>, so
	/// <see cref="TypeDeclarationAccessibility.File"/> has no direct mapping. This method never
	/// throws for an accessibility value.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases")]
	public static Accessibility ToRoslynAccessibility(this TypeDeclarationAccessibility accessibility) =>
		accessibility switch
		{
			TypeDeclarationAccessibility.Private => Accessibility.Private,
			TypeDeclarationAccessibility.PrivateProtected => Accessibility.ProtectedAndInternal,
			TypeDeclarationAccessibility.Protected => Accessibility.Protected,
			TypeDeclarationAccessibility.Internal => Accessibility.Internal,
			TypeDeclarationAccessibility.ProtectedInternal => Accessibility.ProtectedOrInternal,
			TypeDeclarationAccessibility.Public => Accessibility.Public,
			_ => Accessibility.NotApplicable,
		};
}
