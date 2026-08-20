using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Options that configure a source generator test run.
/// </summary>
public record SourceGeneratorTestOptions
{
	/// <summary>
	/// Gets or sets the template copied by newly constructed source generator test options, including
	/// options records derived from this type.
	/// </summary>
	/// <remarks>
	/// Configure this once during test-assembly initialization, before tests execute in parallel.
	/// Existing options instances are snapshots and are not changed when this property is updated.
	/// </remarks>
	public static SourceGeneratorTestOptions Default
	{
		get;
		set => field = value ?? throw new ArgumentNullException(nameof(value), "Default options cannot be null.");
	} = new(true);

	/// <summary>
	/// Initializes a new instance by copying the current <see cref="Default"/> options.
	/// </summary>
	/// <remarks>Derived records implicitly call this constructor unless they select another base constructor.</remarks>
	public SourceGeneratorTestOptions()
		: this(Default) { }

	// Bootstraps Default using the property initializers without recursively reading Default.
	SourceGeneratorTestOptions(bool _) { }

	/// <summary>Initializes an options snapshot by copying another instance.</summary>
	/// <param name="source">The options to copy.</param>
	/// <remarks>
	/// This is also the record copy constructor. Mutable collections are copied so options snapshots
	/// can be customized independently.
	/// </remarks>
	protected SourceGeneratorTestOptions(SourceGeneratorTestOptions source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		IncludeDefaultNamespaces = source.IncludeDefaultNamespaces;
		DefaultNamespaces = source.DefaultNamespaces;
		AdditionalNamespaces = source.AdditionalNamespaces;
		AdditionalAssemblyTypes = source.AdditionalAssemblyTypes;
		AdditionalReferences = source.AdditionalReferences;
		PreprocessReferences = source.PreprocessReferences;
		ThrowOnGenerationException = source.ThrowOnGenerationException;
		CompileToAssembly = source.CompileToAssembly;
		ValidateCodeWriterScopes = source.ValidateCodeWriterScopes;
		EnableLogging = source.EnableLogging;
		ThrowOnLogError = source.ThrowOnLogError;
		DisableSourceGeneratorPropertyName = source.DisableSourceGeneratorPropertyName;
		DisableSourceGeneratorValue = source.DisableSourceGeneratorValue;

		AnalyzerConfigOptions = [with(source.AnalyzerConfigOptions)];

		TestOutput = source.TestOutput;
		CompilationAssemblyName = source.CompilationAssemblyName;
		OutputKind = source.OutputKind;
		LanguageVersion = source.LanguageVersion;
		ExcludeGeneratedSourceHintNames = source.ExcludeGeneratedSourceHintNames;
		AdditionalText = source.AdditionalText;
	}

	/// <summary>
	/// Gets a value indicating whether the default namespaces should be prepended to the source.
	/// </summary>
	public bool IncludeDefaultNamespaces { get; init; } = true;

	/// <summary>
	/// Gets the default namespaces to prepend.
	/// </summary>
	public ImmutableArray<string> DefaultNamespaces { get; init; } =
	["System", "System.Collections.Generic", "System.Linq"];

	/// <summary>
	/// Gets additional namespaces to prepend after the defaults.
	/// </summary>
	public ImmutableArray<string> AdditionalNamespaces { get; init; } = [];

	/// <summary>
	/// Gets additional types whose assemblies should be referenced.
	/// </summary>
	public ImmutableArray<Type> AdditionalAssemblyTypes { get; init; } = [];

	/// <summary>
	/// Gets additional metadata references to include.
	/// </summary>
	public ImmutableArray<MetadataReference> AdditionalReferences { get; init; } = [];

	/// <summary>
	/// Gets a callback that can mutate the resolved references before the compilation is created.
	/// </summary>
	public Action<ImmutableArray<MetadataReference>>? PreprocessReferences { get; init; }

	/// <summary>
	/// Gets a value indicating whether generation automatically calls <see cref="DriverRunResult.EnsureValid"/>, which throws on errors.
	/// </summary>
	public bool ThrowOnGenerationException { get; init; } = true;

	/// <summary>
	/// Gets a value indicating whether the output compilation should be emitted to an assembly.
	/// </summary>
	public bool CompileToAssembly { get; init; } = true;

	/// <summary>
	/// Gets whether code writers created by the generation context should throw when generated
	/// source is materialized while disposable scopes remain open.
	/// </summary>
	/// <remarks>
	/// The default is <see langword="true"/> for generator tests so incomplete generated source is
	/// detected at its point of creation. This value is passed to the generator driver as the
	/// <c>PurviewSourceGeneratorFrameworkValidateCodeWriterScopes</c> compiler-visible property.
	/// </remarks>
	public bool ValidateCodeWriterScopes { get; init; } = true;

	/// <summary>Gets whether framework source-generator logging is captured for this test run.</summary>
	public bool EnableLogging { get; init; } = true;

	/// <summary>
	/// Gets a value indicating whether generator log errors should cause <see cref="DriverRunResult.EnsureValid"/> to throw.
	/// </summary>
	public bool ThrowOnLogError { get; init; } = true;

	/// <summary>
	/// Gets the analyzer-config property name used to disable the source generator.
	/// </summary>
	public string? DisableSourceGeneratorPropertyName { get; init; }

	/// <summary>
	/// Gets the value for <see cref="DisableSourceGeneratorPropertyName"/>.
	/// </summary>
	public bool? DisableSourceGeneratorValue { get; init; }

	/// <summary>
	/// Gets additional analyzer-config options to pass to the generator driver.
	/// </summary>
	public Dictionary<string, string> AnalyzerConfigOptions { get; init; } = [];

	/// <summary>
	/// Gets the test output receiver used for generator logging.
	/// </summary>
	public ITestOutput TestOutput { get; init; } = NullTestOutput.Instance;

	/// <summary>
	/// Gets the assembly name used for the test compilation.
	/// </summary>
	public string CompilationAssemblyName { get; init; } = "TestAssembly";

	/// <summary>
	/// Gets the output kind of the test compilation.
	/// </summary>
	public OutputKind OutputKind { get; init; } = OutputKind.DynamicallyLinkedLibrary;

	/// <summary>
	/// Gets the language version of the test compilation.
	/// </summary>
	public LanguageVersion LanguageVersion { get; init; } = LanguageVersion.Preview;

	/// <summary>
	/// Gets generated hint names to exclude from the syntax tree collection. These
	/// are usually marker attributes or other generated content added during
	/// <see cref="IncrementalGeneratorInitializationContext.RegisterPostInitializationOutput(Action{IncrementalGeneratorPostInitializationContext})"/>.
	/// </summary>
	public ImmutableArray<string> ExcludeGeneratedSourceHintNames { get; init; } = [];

	/// <summary>
	/// Gets additional text files to include in the test compilation.
	/// </summary>
	public ImmutableArray<AdditionalText> AdditionalText { get; init; } = [];
}
