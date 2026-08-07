using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.SourceGeneratorFramework.Helpers;
using Purview.SourceGeneratorFramework.Testing.Abstractions;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Executes a source generator against a test compilation and returns the result.
/// </summary>
public sealed class SourceGeneratorTestRunner<TGenerator>
	where TGenerator : class, IIncrementalGenerator, new()
{
	static readonly string[] TrustedAssemblies = (
		(string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? ""
	).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

	/// <summary>
	/// Runs the generator against the provided source using the specified options.
	/// </summary>
	public Task<DriverRunResult> RunAsync(
		string source,
		SourceGeneratorTestOptions? options = null,
		CancellationToken cancellationToken = default
	) => RunAsync([source], options, cancellationToken);

	/// <summary>
	/// Runs the generator against the provided sources using the specified options.
	/// </summary>
	public async Task<DriverRunResult> RunAsync(
		IEnumerable<string> sources,
		SourceGeneratorTestOptions? options = null,
		CancellationToken cancellationToken = default
	)
	{
		options ??= new();

		var logEntries = new List<(string Message, OutputType Type)>();

		var syntaxTrees = sources
			.Select(source =>
				CSharpSyntaxTree.ParseText(
					PrepareSource(source, options),
					cancellationToken: cancellationToken
				)
			)
			.ToImmutableArray();
		var references = ResolveReferences(options);
		var compilation = CreateCompilation(syntaxTrees, references, options);
		TGenerator generator = new();
		ConfigureLogging(generator, options, logEntries);

		var driver = CreateDriver(generator, options);
		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out _,
			cancellationToken
		);
		var result = driver.GetRunResult();

		Assembly? assembly = null;
		if (options.CompileToAssembly)
			assembly = await CompileToAssemblyAsync(outputCompilation, cancellationToken);

		var nonAttributeTrees = ExcludeGeneratedAttributes(
			result,
			options.ExcludeGeneratedAttributes
		);

		return new(
			result,
			outputCompilation,
			assembly,
			result.GeneratedTrees,
			nonAttributeTrees,
			logEntries
		);
	}

	static string PrepareSource(string source, SourceGeneratorTestOptions options)
	{
		if (!options.IncludeDefaultNamespaces)
			return source;

		var namespaces = options.DefaultNamespaces.AddRange(options.AdditionalNamespaces);
		var usings = string.Join(Environment.NewLine, namespaces.Select(n => $"using {n};"));

		return usings + Environment.NewLine + Environment.NewLine + source;
	}

	static CSharpCompilation CreateCompilation(
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

	static ImmutableArray<MetadataReference> ResolveReferences(SourceGeneratorTestOptions options)
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

	static GeneratorDriver CreateDriver(TGenerator generator, SourceGeneratorTestOptions options)
	{
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		var analyzerOptions = new Dictionary<string, string>(options.AnalyzerConfigOptions);
		if (
			options.DisableSourceGeneratorPropertyName is not null
			&& options.DisableSourceGeneratorValue is not null
		)
			analyzerOptions[
				IncrementalPipeline.BuildProperty + options.DisableSourceGeneratorPropertyName
			] = options.DisableSourceGeneratorValue.Value.ToString();

		if (analyzerOptions.Count > 0)
			driver = driver.WithUpdatedAnalyzerConfigOptions(
				new TestAnalyzerConfigOptionsProvider(analyzerOptions)
			);

		return driver;
	}

	static void ConfigureLogging(
		TGenerator generator,
		SourceGeneratorTestOptions options,
		List<(string, OutputType)> logEntries
	)
	{
		if (generator is ILogSupport logSupport)
		{
			logSupport.SetLogOutput(
				(message, type) =>
				{
					options.TestOutput.WriteLine($"[{type}] {message}");
					logEntries.Add((message, type));
				}
			);
			return;
		}
	}

	static async Task<Assembly?> CompileToAssemblyAsync(
		Compilation compilation,
		CancellationToken cancellationToken
	)
	{
		await using var assemblyStream = new MemoryStream();
		var emitResult = compilation.Emit(assemblyStream, cancellationToken: cancellationToken);
		if (!emitResult.Success)
			return null;

		assemblyStream.Position = 0;
		return Assembly.Load(assemblyStream.ToArray());
	}

	static IEnumerable<SyntaxTree> ExcludeGeneratedAttributes(
		GeneratorDriverRunResult result,
		ImmutableArray<string> exclude
	)
	{
		return exclude.IsEmpty
			? result.GeneratedTrees
			: result.GeneratedTrees.Where(tree =>
				!exclude.Any(attr => tree.FilePath.EndsWith(attr, StringComparison.Ordinal))
			);
	}
}
