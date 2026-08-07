namespace Purview.SourceGeneratorFramework.Testing.Generators.Model;

static class TypeLibrary
{
	public static readonly TypeValueObject ILogSupport =
		TypeValueObject.Create<Abstractions.ILogSupport>();

	public static readonly TypeValueObject GenerationLogger =
		TypeValueObject.Create<Abstractions.GenerationLogger>();

	public static readonly TypeValueObject OutputType =
		TypeValueObject.Create<Abstractions.OutputType>();

	public static readonly TypeValueObject IIncrementalGenerator =
		TypeValueObject.Create<Microsoft.CodeAnalysis.IIncrementalGenerator>();

	public static readonly TypeValueObject ISourceGenerator =
		TypeValueObject.Create<Microsoft.CodeAnalysis.ISourceGenerator>();

	public static readonly TypeValueObject EmbeddedAttribute =
		TypeValueObject.Create<Microsoft.CodeAnalysis.EmbeddedAttribute>();
}
