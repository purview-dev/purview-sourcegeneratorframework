# Purview.SourceGeneratorFramework

A set of libraries for building and testing incremental C# source generators using Roslyn.

## Packages

| Package | Description | Packable |
| --- | --- | --- |
| [`Purview.SourceGeneratorFramework`](src/src/SourceGeneratorFramework) | Core helpers, models, and MSBuild integration for writing incremental source generators. | Yes |
| [`Purview.SourceGeneratorFramework.Testing`](src/src/SourceGeneratorFramework.Testing) | Framework-agnostic test runner and assertions for source generator unit tests. | Yes |
| [`Purview.SourceGeneratorFramework.Testing.TUnit`](src/src/SourceGeneratorFramework.Testing.TUnit) | TUnit-specific test base classes and assertions for source generator tests. | Yes |
| [`SourceGeneratorFramework.Testing.Generators`](src/src/SourceGeneratorFramework.Testing.Generators) | Internal Roslyn source generator used by the framework package. | No |
| [`SourceGeneratorFramework.ExampleGenerator`](src/src/SourceGeneratorFramework.ExampleGenerator) | Reference implementation showing how to build a generator with the framework. | No |

## Requirements

- .NET SDK 8.0 or later
- The test projects target `net8.0`, `net9.0`, and `net10.0`
- Source generators target `netstandard2.0`

## Building

Restore and build the solution using the `just` recipes or `dotnet` directly:

```bash
just build
# or
dotnet build src/SourceGeneratorFramework.slnx -c Release
```

## Running tests

```bash
just tests
# or
dotnet test src/SourceGeneratorFramework.slnx -c Release
```

## Packaging

```bash
just pack
# or
dotnet pack src/SourceGeneratorFramework.slnx -c Release -o ./artifacts
```

## License

This project is licensed under the MIT license.
