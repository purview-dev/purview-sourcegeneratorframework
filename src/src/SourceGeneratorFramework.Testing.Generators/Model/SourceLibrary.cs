namespace Purview.SourceGeneratorFramework.Testing.Generators.Model;

static class SourceLibrary
{
	public const string EmbeddedAttributeSource =
		@"namespace Microsoft.CodeAnalysis;

sealed partial class EmbeddedAttribute : global::System.Attribute
{
}
";
}
