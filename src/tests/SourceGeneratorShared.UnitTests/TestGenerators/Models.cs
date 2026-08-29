using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework.TestGenerators;

readonly record struct TargetInfo(string Name);

sealed record GenerationInputs(bool IsDisabled, string AssemblyName)
{
	public ImmutableArray<TargetInfo> Targets { get; init; } = [];
}
