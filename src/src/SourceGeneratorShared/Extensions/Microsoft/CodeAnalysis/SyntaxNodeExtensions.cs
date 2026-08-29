using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class SyntaxNodeExtensions
{
	/// <summary>
	/// Gets the declared accessibility of a method declaration syntax node.
	/// </summary>
	/// <param name="method">The method declaration syntax node.</param>
	/// <returns>The declared accessibility, or null if no explicit accessibility modifier is present.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the method parameter is null.</exception>
	public static Accessibility? GetDeclaredAccessibility(this MethodDeclarationSyntax method)
	{
		if (method == null)
			throw new ArgumentNullException(nameof(method));

		if (method.Modifiers.Any(SyntaxKind.PublicKeyword))
			return Accessibility.Public;

		if (method.Modifiers.Any(SyntaxKind.PrivateKeyword))
			return Accessibility.Private;

		if (method.Modifiers.Any(SyntaxKind.ProtectedKeyword))
		{
			return method.Modifiers.Any(SyntaxKind.InternalKeyword)
				? Accessibility.ProtectedOrInternal
				: Accessibility.Protected;
		}

		if (method.Modifiers.Any(SyntaxKind.InternalKeyword))
			return Accessibility.Internal;

		return null; // No explicit accessibility modifier.
	}
}
