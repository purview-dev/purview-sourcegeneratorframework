# Purview.SourceGeneratorFramework.Testing

Framework-agnostic test runner and assertions for unit testing incremental C# source generators.

## Installation

```bash
dotnet add package Purview.SourceGeneratorFramework.Testing
```

## What's included

- **`SourceGeneratorTestRunner<TGenerator>`** — compiles a snippet of C# source, runs the generator, automatically registers an isolated framework logging sink, and returns a `DriverRunResult` with generated syntax trees, the output compilation, and captured log entries.
- **`SourceGeneratorTestBase<TGenerator>`** — abstract base class that accepts an `ITestOutput` instance for framework-specific logging integration.
- **`SourceGeneratorTestOptions`** — options for configuring references, namespaces, analyzer-config values, output kind, and whether to emit the output compilation to an assembly.
- **`DriverRunResult`** — wrapper around `GeneratorDriverRunResult` that exposes generated trees, the output compilation, emitted assembly, and log entries.
- **`DriverRunResultExtensions`** — assertion helpers such as `AssertNoCompilationErrors`, `AssertNoGenerationExceptions`, `AssertSingleGeneratedSource`, `AssertGeneratedSourceContains`, and more.
- **`ITestOutput`** / **`NullTestOutput`** — abstraction for capturing generator log output during tests.

## Usage

Reference the package from a test project and write a test using the runner directly:

```xml
<ItemGroup>
  <PackageReference Include="Purview.SourceGeneratorFramework.Testing" />
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
</ItemGroup>
```

```csharp
using Purview.SourceGeneratorFramework.Testing;

public class MyGeneratorTests
{
    [Test]
    public async Task GeneratesExpectedSource()
    {
        var source = """
            [MyNamespace.MyAttribute]
            public partial class MyClass { }
            """;

        var runner = new SourceGeneratorTestRunner<MyGenerator>();
        var result = await runner.RunAsync(source);

        result.AssertNoCompilationErrors();
        var generated = result.AssertSingleGeneratedSource();

        // Use your test framework's assertions, e.g. with TUnit:
        // await Assert.That(generated).Contains("public static partial class MyClass");
    }
}
```

Or derive from `SourceGeneratorTestBase<TGenerator>` and plug in your own `ITestOutput` implementation.

## Running the generator in the test project

Sometimes the test project's own source uses types produced by the generator—for example, an
integration test may attach a generated marker attribute to a fixture class while also passing the
generator type to `SourceGeneratorTestRunner<TGenerator>`.

Reference the generator project twice, once in each role:

```xml
<ItemGroup>
  <!-- Runs the generator during compilation of the test project. -->
  <ProjectReference
    Include="..\..\src\MyGenerator\MyGenerator.csproj"
    PrivateAssets="all"
    OutputItemType="Analyzer"
    ReferenceOutputAssembly="false"
  />

  <!-- Exposes MyGenerator to SourceGeneratorTestRunner<MyGenerator>. -->
  <ProjectReference
    Include="..\..\src\MyGenerator\MyGenerator.csproj"
    PrivateAssets="all"
    ReferenceOutputAssembly="true"
  />
</ItemGroup>
```

The analyzer reference makes generated declarations available to the test project's compilation.
The normal reference makes the generator's CLR type available to the testing API. These are
separate from the in-memory compilation created by `SourceGeneratorTestRunner`; source supplied to
the runner is still compiled and generated independently.

The normal reference also exposes the generator's assembly dependencies to every target framework
of the test project. Keep the generator on the oldest compatible Roslyn version—for example,
Roslyn 4.13 when tests target .NET 8, .NET 9, and .NET 10. A generator built against Roslyn 5 and
`System.Collections.Immutable` 10 will conflict with the framework assemblies supplied by .NET 8
and .NET 9. Use the framework's `RegisterEmbeddedAttribute` helper when avoiding a newer Roslyn API
such as `AddEmbeddedAttributeDefinition`.

## Options

Configure a test run with `SourceGeneratorTestOptions`:

```csharp
var options = new SourceGeneratorTestOptions
{
    IncludeDefaultNamespaces = true,
    AdditionalNamespaces = ["MyNamespace"],
    AdditionalAssemblyTypes = [typeof(SomeExternalType)],
    EnableLogging = true,
    AnalyzerConfigOptions = { ["MyGenerator_Disable"] = "true" }
};

// Emitting the output to an assembly is opt-in because it is expensive.
var result = await runner.RunAsync(source, options.Compile());
```

`Compile()` is an extension method that preserves the concrete options type. A derived options record
that wants a typed default must hide the inherited `SourceGeneratorTestOptions.Default` with a typed
static, otherwise `Default.Compile()` returns the base type:

```csharp
public record MyTestOptions : SourceGeneratorTestOptions
{
    public static new MyTestOptions Default => new();
}

// Returns MyTestOptions with CompileToAssembly enabled.
var result = await runner.RunAsync(source, MyTestOptions.Default.Compile());
```

Analyzer options are preserved under their supplied keys. Keys without the Roslyn
`build_property.` prefix are additionally exposed as compiler-visible MSBuild properties, so either
`MyGenerator_Disable` or `build_property.MyGenerator_Disable` can be used in tests.

See [`SourceGeneratorFramework.Testing.TUnit`](../SourceGeneratorFramework.Testing.TUnit) for a ready-made TUnit integration.

## License

This project is licensed under the MIT license.
