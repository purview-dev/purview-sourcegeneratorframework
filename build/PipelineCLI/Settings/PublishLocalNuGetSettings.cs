using System.ComponentModel.DataAnnotations;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed record PublishLocalNuGetSettings
{
	public const string SectionName = "PublishLocalNuGet";

	[Required(AllowEmptyStrings = false)]
	public string LocalFeedPath { get; init; } = string.Empty;

	public bool OverwriteExistingPackages { get; init; } = true;

	public bool ShutdownDotnetBuilderServer { get; init; } = true;

	public bool ClearPackageCache { get; init; } = true;
}
