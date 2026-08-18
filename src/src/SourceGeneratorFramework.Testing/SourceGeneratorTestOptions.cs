using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Options that configure a source generator test run.
/// </summary>
public sealed record SourceGeneratorTestOptions
{
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
	/// Gets a value indicating whether generation exceptions should cause <see cref="DriverRunResult.EnsureValid"/> to throw.
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
	/// Gets generated attribute file names to exclude from the non-attribute syntax tree collection.
	/// </summary>
	public ImmutableArray<string> ExcludeGeneratedAttributes { get; init; } = [];

	/// <summary>
	/// Gets additional text files to include in the test compilation.
	/// </summary>
	public ImmutableArray<AdditionalText> AdditionalText { get; init; } = [];

	/// <summary>
	/// Gets a state object that can be used to pass arbitrary data to the test runner. Useful for the <see cref="SourceGeneratorTestBase{TGenerator}.OnBeforeRun(IEnumerable{string}, SourceGeneratorTestOptions, CancellationToken)"/>
	/// </summary>
	public object? State { get; init; }
}
