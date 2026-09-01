using ModularPipelines.Options;

namespace Purview.SourceGeneratorFramework.PipelineCLI.Helpers;

public sealed record DotNetCLIOptions : CommandLineToolOptions
{
	public static DotNetCLIOptions Create(params string[] commandParts) =>
		new() { Tool = "dotnet", CommandParts = commandParts };
}
