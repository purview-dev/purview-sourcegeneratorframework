namespace Purview.SourceGeneratorFramework.PipelineCLI.Settings;

public sealed record PackValidationSettings
{
	public const string SectionName = "PackValidation";

	/// <summary>
	/// Every .nupkg must have a matching .snupkg (same id/version) and vice versa.
	/// </summary>
	public bool RequireSymbolPackage { get; init; } = true;

	/// <summary>
	/// Every .snupkg must contain at least one .pdb file.
	/// </summary>
	public bool RequireSymbolFiles { get; init; } = true;

	/// <summary>
	/// Package id (case-insensitive) to entry paths that MUST be present in the .nupkg.
	/// Entry paths use forward slashes, e.g. "lib/netstandard2.0/Foo.dll".
	/// </summary>
	public Dictionary<string, string[]> RequiredContent { get; init; } = [];

	/// <summary>
	/// Package id (case-insensitive) to entry paths that MUST NOT be present in the .nupkg.
	/// Entry paths use forward slashes, e.g. "lib/netstandard2.0/Foo.dll".
	/// </summary>
	public Dictionary<string, string[]> ForbiddenContent { get; init; } = [];
}
