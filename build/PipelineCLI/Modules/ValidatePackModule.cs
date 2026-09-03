using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using NuGet.Packaging;
using NuGet.Versioning;

namespace Purview.SourceGeneratorFramework.PipelineCLI.Modules;

[ModuleCategory("Build")]
[DependsOn<PackModule>]
public sealed class ValidatePackModule(
	IOptions<BuildSettings> buildSettings,
	IOptions<PackValidationSettings> packValidationSettings
) : Module<PackValidationResult[]>
{
	protected override ModuleConfiguration Configure() =>
		ModuleConfiguration
			.Create()
			.WithSkipWhen(_ =>
				!buildSettings.Value.ValidatePack
					? SkipDecision.Skip("Pack validation is disabled. Set Build__ValidatePack=true to enable it.")
					: SkipDecision.DoNotSkip
			)
			.Build();

	protected override async Task<PackValidationResult[]?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var artifactsFolder = Path.GetFullPath(buildSettings.Value.ArtifactsFolder);
		if (!Directory.Exists(artifactsFolder))
		{
			throw new InvalidOperationException(
				$"The artifacts folder '{artifactsFolder}' does not exist. Run the pack step first."
			);
		}

		var nupkgFiles = Directory.EnumerateFiles(artifactsFolder, "*.nupkg", SearchOption.TopDirectoryOnly).ToArray();
		var snupkgFiles = Directory
			.EnumerateFiles(artifactsFolder, "*.snupkg", SearchOption.TopDirectoryOnly)
			.ToArray();

		if (nupkgFiles.Length == 0)
		{
			throw new InvalidOperationException($"No .nupkg files found in {artifactsFolder}.");
		}

		var results = new List<PackValidationResult>(nupkgFiles.Length + snupkgFiles.Length);
		var packagePairs = new Dictionary<string, PackagePair>(StringComparer.OrdinalIgnoreCase);

		foreach (var package in nupkgFiles)
		{
			var result = await ValidateNupkgAsync(package, packValidationSettings.Value, cancellationToken);
			results.Add(result);

			var pair = GetOrAddPair(packagePairs, result.PackageKey);
			pair.Nupkg = result;
		}

		foreach (var package in snupkgFiles)
		{
			var result = await ValidateSnupkgAsync(package, packValidationSettings.Value, cancellationToken);
			results.Add(result);

			var pair = GetOrAddPair(packagePairs, result.PackageKey);
			pair.Snupkg = result;
		}

		if (packValidationSettings.Value.RequireSymbolPackage)
		{
			foreach (var pair in packagePairs.Values)
			{
				if (pair.Nupkg is not null && pair.Snupkg is null)
				{
					pair.Nupkg.AddError(
						$"Package '{pair.Nupkg.PackageId}' {pair.Nupkg.Version.ToNormalizedString()} has no matching .snupkg."
					);
				}

				if (pair.Snupkg is not null && pair.Nupkg is null)
				{
					pair.Snupkg.AddError(
						$"Symbol package '{pair.Snupkg.PackageId}' {pair.Snupkg.Version.ToNormalizedString()} has no matching .nupkg."
					);
				}
			}
		}

		var invalid = results.Where(result => result.Errors.Count > 0).ToList();
		foreach (var result in results)
		{
			if (result.Errors.Count == 0)
			{
				context.Logger.LogInformation(
					"Validated {FileName} ({Kind}): {PackageId} {Version}.",
					result.FileName,
					result.Kind,
					result.PackageId,
					result.Version.ToNormalizedString()
				);
			}
			else
			{
				foreach (var error in result.Errors)
					context.Logger.LogError("{FileName}: {Error}", result.FileName, error);
			}
		}

		var validCount = results.Count - invalid.Count;
		context.Summary.KeyValue("PackValidation", "Valid packages", $"{validCount}/{results.Count}");
		context.Summary.KeyValue("PackValidation", "Invalid packages", $"{invalid.Count}/{results.Count}");

		if (invalid.Count > 0)
		{
			var detail = string.Join(
				Environment.NewLine,
				invalid.Select(result =>
					$"  {result.FileName}:{Environment.NewLine}    "
					+ string.Join(Environment.NewLine + "    ", result.Errors)
				)
			);

			throw new InvalidOperationException(
				$"Pack validation failed for {invalid.Count} of {results.Count} package(s):{Environment.NewLine}{detail}"
			);
		}

		return results.ToArray();
	}

	static async Task<PackValidationResult> ValidateNupkgAsync(
		string packagePath,
		PackValidationSettings settings,
		CancellationToken cancellationToken
	)
	{
		var errors = new List<string>();

		try
		{
			using var reader = new PackageArchiveReader(packagePath);
			var nuspec = await reader.GetNuspecReaderAsync(cancellationToken);
			var id = nuspec.GetId();
			var version = nuspec.GetVersion();

			ValidateFileName(packagePath, id, version, ".nupkg", errors);

			var files = reader.GetFiles().ToArray();
			ValidateNoPdbFiles(files, errors);

			var required = GetContentRule(settings.RequiredContent, id);
			if (required is not null)
			{
				foreach (var entry in required)
				{
					if (!files.Contains(entry, StringComparer.OrdinalIgnoreCase))
						errors.Add($"Required content '{entry}' is missing from the package.");
				}
			}

			var forbidden = GetContentRule(settings.ForbiddenContent, id);
			if (forbidden is not null)
			{
				foreach (var entry in forbidden)
				{
					if (files.Contains(entry, StringComparer.OrdinalIgnoreCase))
						errors.Add($"Forbidden content '{entry}' must not be in the package.");
				}
			}

			var result = new PackValidationResult(
				Path.GetFileName(packagePath),
				"nupkg",
				CreatePackageKey(id, version),
				id,
				version
			);
			result.AddErrors(errors);
			return result;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			var result = new PackValidationResult(
				Path.GetFileName(packagePath),
				"nupkg",
				Path.GetFileName(packagePath) + "|unreadable",
				"<unreadable>",
				new NuGetVersion(0, 0, 0)
			);
			result.AddError($"Failed to read package: {ex.Message}");
			return result;
		}
	}

	static async Task<PackValidationResult> ValidateSnupkgAsync(
		string packagePath,
		PackValidationSettings settings,
		CancellationToken cancellationToken
	)
	{
		var errors = new List<string>();

		try
		{
			using var reader = new PackageArchiveReader(packagePath);
			var nuspec = await reader.GetNuspecReaderAsync(cancellationToken);
			var id = nuspec.GetId();
			var version = nuspec.GetVersion();

			ValidateFileName(packagePath, id, version, ".snupkg", errors);

			var files = reader.GetFiles().ToArray();
			var nonSymbolFiles = files.Where(file => !IsPdbFile(file) && !IsSymbolPackageMetadata(file)).ToArray();
			if (nonSymbolFiles.Length > 0)
				errors.Add($"Symbol package contains non-symbol file(s): {string.Join(", ", nonSymbolFiles)}.");

			if (settings.RequireSymbolFiles && !files.Any(IsPdbFile))
				errors.Add("Symbol package contains no .pdb files.");

			var result = new PackValidationResult(
				Path.GetFileName(packagePath),
				"snupkg",
				CreatePackageKey(id, version),
				id,
				version
			);
			result.AddErrors(errors);
			return result;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			var result = new PackValidationResult(
				Path.GetFileName(packagePath),
				"snupkg",
				Path.GetFileName(packagePath) + "|unreadable",
				"<unreadable>",
				new NuGetVersion(0, 0, 0)
			);
			result.AddError($"Failed to read package: {ex.Message}");
			return result;
		}
	}

	static void ValidateFileName(
		string packagePath,
		string id,
		NuGetVersion version,
		string extension,
		List<string> errors
	)
	{
		var expected = $"{id}.{version.ToNormalizedString()}{extension}";
		if (!string.Equals(Path.GetFileName(packagePath), expected, StringComparison.OrdinalIgnoreCase))
			errors.Add(
				$"File name '{Path.GetFileName(packagePath)}' does not match the nuspec id/version '{expected}'."
			);
	}

	static void ValidateNoPdbFiles(IEnumerable<string> files, List<string> errors)
	{
		var pdbFiles = files.Where(IsPdbFile).ToArray();
		if (pdbFiles.Length > 0)
			errors.Add(
				$"Package contains PDB file(s): {string.Join(", ", pdbFiles)}. "
					+ "PDBs must only be delivered through the .snupkg."
			);
	}

	static bool IsPdbFile(string path) =>
		string.Equals(Path.GetExtension(path), ".pdb", StringComparison.OrdinalIgnoreCase);

	static bool IsSymbolPackageMetadata(string path) =>
		string.Equals(path, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase)
		|| path.StartsWith("_rels/", StringComparison.OrdinalIgnoreCase)
		|| path.StartsWith("package/services/metadata/", StringComparison.OrdinalIgnoreCase)
		|| path.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase);

	static string[]? GetContentRule(Dictionary<string, string[]> rules, string packageId)
	{
		if (rules.TryGetValue(packageId, out var exact))
			return exact;

		foreach (var rule in rules)
		{
			if (string.Equals(rule.Key, packageId, StringComparison.OrdinalIgnoreCase))
				return rule.Value;
		}

		return null;
	}

	static string CreatePackageKey(string id, NuGetVersion version) => $"{id}|{version.ToNormalizedString()}";

	static PackagePair GetOrAddPair(Dictionary<string, PackagePair> pairs, string key)
	{
		if (!pairs.TryGetValue(key, out var pair))
		{
			pair = new PackagePair();
			pairs.Add(key, pair);
		}

		return pair;
	}
}

public sealed class PackValidationResult
{
	readonly List<string> _errors = [];

	public PackValidationResult(string fileName, string kind, string packageKey, string packageId, NuGetVersion version)
	{
		FileName = fileName;
		Kind = kind;
		PackageKey = packageKey;
		PackageId = packageId;
		Version = version;
	}

	public string FileName { get; }

	public string Kind { get; }

	public string PackageKey { get; }

	public string PackageId { get; }

	public NuGetVersion Version { get; }

	public IReadOnlyList<string> Errors => _errors;

	internal void AddError(string error) => _errors.Add(error);

	internal void AddErrors(IEnumerable<string> errors) => _errors.AddRange(errors);
}

sealed class PackagePair
{
	public PackValidationResult? Nupkg { get; set; }

	public PackValidationResult? Snupkg { get; set; }
}
