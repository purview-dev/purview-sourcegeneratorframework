using System.ComponentModel.DataAnnotations;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed record PublishLocalNuGetSettings : IValidatableObject
{
	public const string SectionName = "PublishLocalNuGet";

	[Required(AllowEmptyStrings = false)]
	public string LocalFeedPath { get; init; } = string.Empty;

	public bool OverwriteExistingPackages { get; init; } = true;

	public bool ShutdownDotnetBuilderServer { get; init; } = true;

	public bool ClearPackageCache { get; init; } = true;

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (string.IsNullOrWhiteSpace(LocalFeedPath))
		{
			yield return new ValidationResult("LocalFeedPath is required.", [nameof(LocalFeedPath)]);
			yield break;
		}

		if (!Path.IsPathRooted(LocalFeedPath))
		{
			yield return new ValidationResult(
				$"LocalFeedPath must be an absolute path. Received: '{LocalFeedPath}'.",
				[nameof(LocalFeedPath)]
			);
			yield break;
		}

		var root = Path.GetPathRoot(LocalFeedPath);
		if (root is null)
		{
			yield return new ValidationResult(
				$"LocalFeedPath could not be parsed. Received: '{LocalFeedPath}'.",
				[nameof(LocalFeedPath)]
			);
			yield break;
		}

		var lastChar = root[^1];
		if (lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar)
			yield break;

		if (root.StartsWith(@"\\", StringComparison.Ordinal) || root.StartsWith("//", StringComparison.Ordinal))
			yield break;

		yield return new ValidationResult(
			$"LocalFeedPath must be an absolute path (e.g. 'C:\\folder' or '\\\\server\\share'). Received: '{LocalFeedPath}'.",
			[nameof(LocalFeedPath)]
		);
	}
}
