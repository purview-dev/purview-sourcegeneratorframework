using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

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
	/// Gets generated attribute file names to exclude from the non-attribute syntax tree collection.
	/// </summary>
	public ImmutableArray<string> ExcludeGeneratedAttributes { get; init; } = [];
}
