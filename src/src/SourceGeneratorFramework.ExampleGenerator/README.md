# SourceGeneratorFramework.ExampleGenerator

Reference implementation of an incremental C# source generator built with `Purview.SourceGeneratorFramework`.

This project is not published as a NuGet package. It demonstrates how to:

- Define a custom attribute that is emitted by the generator at build time.
- Use `IncrementalPipeline.ForAttributeWithMetadataName` to discover attributed types.
- Validate discovered symbols and report diagnostics for invalid inputs.
- Use `CodeWriter` to generate partial classes with nested generated types.
- Unit test the generator with `Purview.SourceGeneratorFramework.Testing`.

## Usage

Build the project and reference the generated assembly from a test or sample project to see the framework in action. Unit tests are located in [`SourceGeneratorFramework.ExampleGenerator.UnitTests`](../../tests/SourceGeneratorFramework.ExampleGenerator.UnitTests).

## License

This project is licensed under the MIT license.
