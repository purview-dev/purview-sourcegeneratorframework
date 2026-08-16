# Purview.SourceGeneratorFramework.Testing.TUnit

TUnit integration for testing incremental C# source generators built with `Purview.SourceGeneratorFramework`.

## Installation

```bash
dotnet add package Purview.SourceGeneratorFramework.Testing.TUnit
```

## What's included

- **`TUnitSourceGeneratorTestBase<TGenerator>`** — ready-made base class for TUnit tests. It wires generator log output to `TestContext.Current.OutputWriter`.
- **Custom TUnit assertions** for inspecting `DriverRunResult` instances directly in TUnit tests.
- **MSBuild `.props`** — automatically adds `global using` directives for `Purview.SourceGeneratorFramework.Testing.TUnit` and `Purview.SourceGeneratorFramework.Testing.TUnit.Assertions`.

## Usage

Reference the package from a TUnit test project:

```xml
<ItemGroup>
  <PackageReference Include="TUnit" />
  <PackageReference Include="Purview.SourceGeneratorFramework.Testing.TUnit" />
</ItemGroup>
```

Derive your test class from `TUnitSourceGeneratorTestBase<TGenerator>` and use the inherited `GenerateAsync` method:

```csharp
using Purview.SourceGeneratorFramework.Testing.TUnit;

public class MyGeneratorTests : TUnitSourceGeneratorTestBase<MyGenerator>
{
    [Test]
    public async Task GeneratesExpectedSource()
    {
        var source = """
            [MyNamespace.MyAttribute]
            public partial class MyClass { }
            """;

        var result = await GenerateAsync(source);

        result.AssertNoCompilationErrors();
        var generated = result.AssertSingleGeneratedSource();

        await Assert.That(generated).Contains("public static partial class MyClass");
    }
}
```

The base class also provides access to the underlying `SourceGeneratorTestRunner<TGenerator>` behavior through `GenerateAsync`.

## Using generated types in the TUnit project

If test source files use generated attributes or other generated declarations while the tests also
derive from `TUnitSourceGeneratorTestBase<TGenerator>`, reference the generator project both as an
analyzer and as a normal assembly:

```xml
<ItemGroup>
  <!-- Generates declarations used by classes in this TUnit project. -->
  <ProjectReference
    Include="..\..\src\MyGenerator\MyGenerator.csproj"
    PrivateAssets="all"
    OutputItemType="Analyzer"
    ReferenceOutputAssembly="false"
  />

  <!-- Makes MyGenerator available as TGenerator. -->
  <ProjectReference
    Include="..\..\src\MyGenerator\MyGenerator.csproj"
    PrivateAssets="all"
    ReferenceOutputAssembly="true"
  />
</ItemGroup>
```

For example, the analyzer reference allows a test fixture to use `[MyGeneratedAttribute]`, while
the normal reference allows the test class to derive from
`TUnitSourceGeneratorTestBase<MyGenerator>`. Do not add `OutputItemType="Analyzer"` to the normal
reference.

For multi-target TUnit projects, the normal reference means the generator's Roslyn dependencies
participate in reference resolution for every target. Build the generator against the oldest
compatible Roslyn version (Roslyn 4.13 for a .NET 8–10 test matrix) and avoid forcing a newer
`System.Collections.Immutable` version through central package management. The framework's
`RegisterEmbeddedAttribute` helper can be used instead of Roslyn 4.14's
`AddEmbeddedAttributeDefinition` API when .NET 8 compatibility is required.

## License

This project is licensed under the MIT license.
