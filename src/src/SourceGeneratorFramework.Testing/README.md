# Purview.SourceGeneratorFramework.Testing

Framework-agnostic test runner and assertions for unit testing incremental C# source generators.

## Installation

```bash
dotnet add package Purview.SourceGeneratorFramework.Testing
```

## What's included

- **`SourceGeneratorTestRunner<TGenerator>`** — compiles a snippet of C# source, runs the generator, and returns a `DriverRunResult` with generated syntax trees, the output compilation, and captured log entries.
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

## Options

Configure a test run with `SourceGeneratorTestOptions`:

```csharp
var options = new SourceGeneratorTestOptions
{
    IncludeDefaultNamespaces = true,
    AdditionalNamespaces = ["MyNamespace"],
    AdditionalAssemblyTypes = [typeof(SomeExternalType)],
    CompileToAssembly = true,
    AnalyzerConfigOptions = { ["build_property.MyGenerator_Disable"] = "true" }
};

var result = await runner.RunAsync(source, options);
```

See [`SourceGeneratorFramework.Testing.TUnit`](../SourceGeneratorFramework.Testing.TUnit) for a ready-made TUnit integration.

## License

This project is licensed under the MIT license.
