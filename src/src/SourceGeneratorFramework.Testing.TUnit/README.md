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

## License

This project is licensed under the MIT license.
