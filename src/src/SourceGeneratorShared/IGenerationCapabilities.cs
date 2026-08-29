namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Defines the capabilities of a source generator, which can be used to determine what features are available during generation.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1040:Avoid empty interfaces",
	Justification = "Used as a marker interface for generation capabilities so an analyzer can identify them and provide appropriate warnings or suggestions."
)]
public interface IGenerationCapabilities;
