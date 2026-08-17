using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.Generators.Model;

static class GeneratorTypeLibrary
{
	const string GeneratorsNamespace = "Purview.SourceGeneratorFramework.Generators";

	public static readonly TypeValueObject TypeValueObject = TypeValueObject.Create<TypeValueObject>();

	public static class Attirbutes
	{
		public static readonly TypeValueObject GenerateAttribute = new(nameof(GenerateAttribute), GeneratorsNamespace);

		public static readonly TypeValueObject PropertyAttribute = new(nameof(PropertyAttribute), GeneratorsNamespace);

		public static readonly TypeValueObject ArgumentAttribute = new(nameof(ArgumentAttribute), GeneratorsNamespace);

		public static readonly TypeValueObject NestedModelAttribute = new(
			nameof(NestedModelAttribute),
			GeneratorsNamespace
		);

		public static readonly TypeValueObject ExcludeAttribute = new(nameof(ExcludeAttribute), GeneratorsNamespace);

		public static readonly TypeValueObject GenericTypeArgumentAttribute = new(
			nameof(GenericTypeArgumentAttribute),
			GeneratorsNamespace
		);
	}

	public static class Logging
	{
		public static readonly TypeValueObject ISupportsSourceGenLogging =
			TypeValueObject.Create<ISupportsSourceGenLogging>();

		// Don't use the in-assembly SourceGenLogger type here because
		// we'll generate ours in a different namespace.
		public static readonly TypeValueObject SourceGenLogger = new(
			nameof(SourceGenLogger),
			typeof(ISupportsSourceGenLogging).Namespace
		);

		public static readonly TypeValueObject ISourceGenLogger = TypeValueObject.Create<ISourceGenLogger>();

		public static readonly TypeValueObject SourceGenLogLevel = TypeValueObject.Create<SourceGenLogLevel>();
	}

	public static class System
	{
		public static readonly TypeValueObject Action = TypeValueObject.Create<Action>();

		public static readonly TypeValueObject Object = TypeValueObject.Create<object>();

		public static readonly TypeValueObject String = TypeValueObject.Create<string>();

		public static readonly TypeValueObject Int32 = TypeValueObject.Create<int>();
	}

	public static class CodeAnalysis
	{
		public static readonly TypeValueObject IIncrementalGenerator =
			TypeValueObject.Create<Microsoft.CodeAnalysis.IIncrementalGenerator>();

		public static readonly TypeValueObject ISourceGenerator =
			TypeValueObject.Create<Microsoft.CodeAnalysis.ISourceGenerator>();

		public static readonly TypeValueObject EmbeddedAttribute =
			TypeValueObject.Create<Microsoft.CodeAnalysis.EmbeddedAttribute>();
	}
}
