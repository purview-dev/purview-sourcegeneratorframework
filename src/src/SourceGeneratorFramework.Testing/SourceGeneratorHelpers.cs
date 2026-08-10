using System.Collections.Immutable;
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
		SourceGeneratorTestOptions options
	)
	{
		var builder = ImmutableArray.CreateBuilder<MetadataReference>();
		builder.AddRange(TrustedAssemblies.Select(static p => MetadataReference.CreateFromFile(p)));
		builder.AddRange(
			options.AdditionalAssemblyTypes.Select(static a =>
				MetadataReference.CreateFromFile(a.Assembly.Location)
			)
		);
		builder.AddRange(options.AdditionalReferences);

		var references = builder.ToImmutable();
		options.PreprocessReferences?.Invoke(references);
		return references;
	}
}
