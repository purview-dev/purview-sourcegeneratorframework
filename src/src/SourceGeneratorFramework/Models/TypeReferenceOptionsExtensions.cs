namespace Purview.SourceGeneratorFramework.Models;

public static class TypeReferenceOptionsExtensions
{
	public static bool IsNullOrEmpty(this TypeReferenceOptions? typeReference) =>
		typeReference is null || typeReference.Kind == TypeReferenceKind.None;
}
