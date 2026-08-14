using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.SourceGeneratorFramework.Helpers;
using Purview.SourceGeneratorFramework.Logging;
using Purview.SourceGeneratorFramework.Models;
using Purview.SourceGeneratorFramework.Testing.Models;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Executes a source generator against a test compilation and returns the result.
/// </summary>
public sealed class SourceGeneratorTestRunner<TGenerator>
	where TGenerator : class, IIncrementalGenerator, new()
{
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

		List<LogEntry> logEntries = [];

		var syntaxTrees = sources
			.Select(source =>
				CSharpSyntaxTree.ParseText(
					PrepareSource(source, options),
					encoding: System.Text.Encoding.UTF8,
					options: new CSharpParseOptions(options.LanguageVersion),
					cancellationToken: cancellationToken
				)
			)
			.ToImmutableArray();
		var references = SourceGeneratorHelpers.ResolveReferences(options);
		var compilation = SourceGeneratorHelpers.CreateCompilation(
			syntaxTrees,
			references,
			options
		);
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
		Diagnostic[] compilationDiagnostics = [];
		if (options.CompileToAssembly)
		{
			(assembly, compilationDiagnostics) = await CompileToAssemblyAsync(
				outputCompilation,
				cancellationToken
			);
		}

		var nonAttributeTrees = ExcludeGeneratedAttributes(
			result,
			options.ExcludeGeneratedAttributes
		);

		return new(
			result,
			outputCompilation,
			assembly,
			compilationDiagnostics,
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

	static GeneratorDriver CreateDriver(TGenerator generator, SourceGeneratorTestOptions options)
	{
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			[generator.AsSourceGenerator()],
			parseOptions: new(options.LanguageVersion)
		);

		var analyzerOptions = new Dictionary<string, string>(options.AnalyzerConfigOptions)
		{
			[
				IncrementalPipeline.BuildProperty
					+ GenerationContext.ValidateCodeWriterScopesBuildProperty
			] = options.ValidateCodeWriterScopes ? "true" : "false",
		};
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
		List<LogEntry> logEntries
	)
	{
		if (generator is ILogSupport logSupport)
		{
			logSupport.SetLogOutput(
				(message, type) =>
				{
					options.TestOutput.WriteLine($"[{type}] {message}");
					logEntries.Add(new LogEntry(type, message));
				}
			);
			return;
		}
	}

	static async Task<(Assembly?, Diagnostic[])> CompileToAssemblyAsync(
		Compilation compilation,
		CancellationToken cancellationToken
	)
	{
		await using var assemblyStream = new MemoryStream();
		var emitResult = compilation.Emit(assemblyStream, cancellationToken: cancellationToken);

		if (!emitResult.Success)
			return (null, emitResult.Diagnostics.ToArray());

		assemblyStream.Position = 0;
		return (Assembly.Load(assemblyStream.ToArray()), emitResult.Diagnostics.ToArray());
	}

	static IEnumerable<SyntaxTree> ExcludeGeneratedAttributes(
		GeneratorDriverRunResult result,
		ImmutableArray<string> exclude
	)
	{
		return exclude.IsEmpty
			? result.GeneratedTrees
			: result.GeneratedTrees.Where(tree =>
				!exclude.Any(attr =>
					tree.FilePath.EndsWith(attr, StringComparison.Ordinal)
					|| tree.FilePath.EndsWith(attr + ".g.cs", StringComparison.Ordinal)
					|| tree.FilePath.EndsWith(attr + ".cs", StringComparison.Ordinal)
				)
			);
	}
}
