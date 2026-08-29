using System.ComponentModel;

namespace Purview.SourceGeneratorFramework;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeReferenceExtensions
{
	/// <summary>
	/// Determines whether the specified <see cref="TypeReference"/> is null or empty.
	/// </summary>
	/// <param name="typeReference">The <see cref="TypeReference"/> to check.</param>
	/// <returns><see langword="true"/> if the specified <see cref="TypeReference"/> is null or empty; otherwise, <see langword="false"/>.</returns>
	public static bool IsNullOrEmpty(this TypeReference? typeReference) =>
		typeReference is null || typeReference.Kind == TypeReferenceKind.None;
}
