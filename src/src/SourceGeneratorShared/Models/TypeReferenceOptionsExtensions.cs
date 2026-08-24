using System.ComponentModel;

namespace Purview.SourceGeneratorFramework.Models;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeReferenceOptionsExtensions
{
	/// <summary>
	/// Determines whether the specified <see cref="TypeReferenceOptions"/> is null or empty.
	/// </summary>
	/// <param name="typeReference">The <see cref="TypeReferenceOptions"/> to check.</param>
	/// <returns><see langword="true"/> if the specified <see cref="TypeReferenceOptions"/> is null or empty; otherwise, <see langword="false"/>.</returns>
	public static bool IsNullOrEmpty(this TypeReferenceOptions? typeReference) =>
		typeReference is null || typeReference.Kind == TypeReferenceKind.None;
}
