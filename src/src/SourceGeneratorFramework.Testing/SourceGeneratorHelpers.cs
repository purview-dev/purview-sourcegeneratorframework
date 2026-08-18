using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework.Testing;

static class SourceGeneratorHelpers
{
	public static readonly string[] TrustedAssemblies = (
		(string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? ""
	).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

	public static CSharpCompilation CreateCompilation(
		IEnumerable<SyntaxTree> syntaxTrees,
		ImmutableArray<MetadataReference> references,
		SourceGeneratorTestOptions options
	)
	{
		return CSharpCompilation.Create(
			options.CompilationAssemblyName,
			syntaxTrees,
			references,
			new CSharpCompilationOptions(options.OutputKind)
		);
	}

	public static ImmutableArray<MetadataReference> ResolveReferences(
		SourceGeneratorTestOptions options,
		Assembly generatorAssembly
	)
	{
		if (generatorAssembly is null)
			throw new ArgumentNullException(nameof(generatorAssembly));

		var generatorAssemblyPath = generatorAssembly.GetType(
			"Purview.SourceGeneratorFramework.Models.TypeValueObject",
			throwOnError: false
		)
			is null
			? null
			: generatorAssembly.Location;
		var builder = ImmutableArray.CreateBuilder<MetadataReference>();
		builder.AddRange(
			TrustedAssemblies
				.Where(path =>
					generatorAssemblyPath is null
					|| !string.Equals(path, generatorAssemblyPath, StringComparison.OrdinalIgnoreCase)
				)
				.Select(static path => MetadataReference.CreateFromFile(path))
		);
		builder.AddRange(
			options.AdditionalAssemblyTypes.Select(static a => MetadataReference.CreateFromFile(a.Assembly.Location))
		);
		builder.AddRange(options.AdditionalReferences);

		var references = builder.ToImmutable();
		options.PreprocessReferences?.Invoke(references);
		return references;
	}
}
