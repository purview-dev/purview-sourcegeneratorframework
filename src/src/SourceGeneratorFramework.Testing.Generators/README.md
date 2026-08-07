# SourceGeneratorFramework.Testing.Generators

Internal Roslyn source generator used by `Purview.SourceGeneratorFramework`. It generates framework code that is bundled into the main framework package at pack time.

This project is not published as a standalone NuGet package. It is referenced as an analyzer by `SourceGeneratorFramework` and its output is included in the `Purview.SourceGeneratorFramework` package under the `analyzers/dotnet/cs/` folder.

## License

This project is licensed under the MIT license.
