using System.Collections.Concurrent;
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
		options ??= new SourceGeneratorTestOptions();

		ConcurrentBag<LogEntry> logEntries = [];

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
		var references = SourceGeneratorHelpers.ResolveReferences(options, typeof(TGenerator).Assembly);
		var compilation = SourceGeneratorHelpers.CreateCompilation(syntaxTrees, references, options);
		TGenerator generator = new();
		var loggingSessionId = options.EnableLogging ? Guid.NewGuid().ToString("N") : null;
		using var loggingRegistration = ConfigureLogging(loggingSessionId, options, logEntries);

		var driver = CreateDriver(generator, options, loggingSessionId);
		driver = driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out _,
			cancellationToken
		);
		var result = driver.GetRunResult();

		Assembly? assembly = null;
		ImmutableArray<Diagnostic> compilationDiagnostics = [];
		if (options.CompileToAssembly)
		{
			(assembly, compilationDiagnostics) = await CompileToAssemblyAsync(outputCompilation, cancellationToken);
		}

		var excludedGeneratedSource = ExcludeGeneratedSources(result, options.ExcludeGeneratedSourceHintNames);

		return new(
			result,
			new(outputCompilation, assembly, compilationDiagnostics),
			result.GeneratedTrees,
			excludedGeneratedSource,
			[.. logEntries]
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

	static GeneratorDriver CreateDriver(
		TGenerator generator,
		SourceGeneratorTestOptions options,
		string? loggingSessionId
	)
	{
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			[generator.AsSourceGenerator()],
			additionalTexts: options.AdditionalText,
			parseOptions: new(options.LanguageVersion)
		);

		var analyzerOptions = new Dictionary<string, string>(options.AnalyzerConfigOptions)
		{
			[IncrementalPipeline.BuildProperty + GenerationContext.ValidateCodeWriterScopesBuildProperty] =
				options.ValidateCodeWriterScopes ? "true" : "false",
			[IncrementalPipeline.BuildProperty + GenerationContext.EnableLoggingBuildProperty] = options.EnableLogging
				? "true"
				: "false",
		};
		foreach (var (key, value) in options.AnalyzerConfigOptions)
		{
			if (!key.StartsWith(IncrementalPipeline.BuildProperty, StringComparison.Ordinal))
				analyzerOptions.TryAdd(IncrementalPipeline.BuildProperty + key, value);
		}
		if (loggingSessionId is not null)
			analyzerOptions[IncrementalPipeline.BuildProperty + GenerationContext.LoggingSessionIdBuildProperty] =
				loggingSessionId;
		if (options.DisableSourceGeneratorPropertyName is not null && options.DisableSourceGeneratorValue is not null)
			analyzerOptions[IncrementalPipeline.BuildProperty + options.DisableSourceGeneratorPropertyName] =
				options.DisableSourceGeneratorValue.Value.ToString();

		if (analyzerOptions.Count > 0)
			driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(analyzerOptions));

		return driver;
	}

	static LoggingRegistrations? ConfigureLogging(
		string? loggingSessionId,
		SourceGeneratorTestOptions options,
		ConcurrentBag<LogEntry> logEntries
	)
	{
		if (loggingSessionId is null)
			return null;

		Action<string, int> sink = (message, level) =>
		{
			var type = (SourceGenLogLevel)level;
			options.TestOutput.WriteLine($"[{type}] {message}");

			logEntries.Add(new(type, message));
		};

		List<IDisposable> registrations = [SourceGenLogging.RegisterSinkCore(loggingSessionId, sink)];

		// Self-contained generators may embed the framework logging types. Register the test sink
		// explicitly with that private copy rather than relying on process-wide shared state.
		var embeddedLoggingType = typeof(TGenerator).Assembly.GetType(
			"Purview.SourceGeneratorFramework.Logging.SourceGenLogging",
			throwOnError: false
		);
		if (embeddedLoggingType is not null && embeddedLoggingType != typeof(SourceGenLogging))
		{
			var registerSink = embeddedLoggingType.GetMethod(
				"RegisterSinkCore",
				BindingFlags.Static | BindingFlags.NonPublic
			);
			if (registerSink?.Invoke(null, [loggingSessionId, sink]) is IDisposable registration)
				registrations.Add(registration);
		}

		return new(registrations);
	}

	sealed class LoggingRegistrations(List<IDisposable> registrations) : IDisposable
	{
		List<IDisposable>? _registrations = registrations;

		public void Dispose()
		{
			var registrationsToDispose = Interlocked.Exchange(ref _registrations, null);
			if (registrationsToDispose is null)
				return;

			for (var index = registrationsToDispose.Count - 1; index >= 0; index--)
				registrationsToDispose[index].Dispose();
		}
	}

	static async Task<(Assembly?, ImmutableArray<Diagnostic>)> CompileToAssemblyAsync(
		Compilation compilation,
		CancellationToken cancellationToken
	)
	{
		await using var assemblyStream = new MemoryStream();
		var emitResult = compilation.Emit(assemblyStream, cancellationToken: cancellationToken);

		if (!emitResult.Success)
			return (null, emitResult.Diagnostics);

		assemblyStream.Position = 0;
		return (Assembly.Load(assemblyStream.ToArray()), emitResult.Diagnostics);
	}

	static ImmutableArray<SyntaxTree> ExcludeGeneratedSources(
		GeneratorDriverRunResult result,
		ImmutableArray<string> exclude
	)
	{
		return exclude.IsEmpty
			? result.GeneratedTrees
			:
			[
				.. result.GeneratedTrees.Where(tree =>
					!exclude.Any(attr =>
						tree.FilePath.EndsWith(attr, StringComparison.Ordinal)
						|| tree.FilePath.EndsWith(attr + ".g.cs", StringComparison.Ordinal)
						|| tree.FilePath.EndsWith(attr + ".cs", StringComparison.Ordinal)
					)
				),
			];
	}
}
