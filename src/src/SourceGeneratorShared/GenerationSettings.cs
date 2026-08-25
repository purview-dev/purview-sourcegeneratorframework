namespace Purview.SourceGeneratorFramework;

/// <summary>Describes immutable settings shared by a source-generation operation.</summary>
public sealed record GenerationSettings
{
	/// <summary>Initializes source-generation settings.</summary>
	public GenerationSettings(string generatorName, string generatorVersion, bool validateCodeWriterScopes = false)
	{
		if (string.IsNullOrWhiteSpace(generatorName))
			throw new ArgumentException("Generator name cannot be null or whitespace.", nameof(generatorName));
		if (string.IsNullOrWhiteSpace(generatorVersion))
			throw new ArgumentException("Generator version cannot be null or whitespace.", nameof(generatorVersion));

		GeneratorName = generatorName;
		GeneratorVersion = generatorVersion;
		ValidateCodeWriterScopes = validateCodeWriterScopes;
	}

	/// <summary>Gets the source generator name propagated to code writers.</summary>
	public string GeneratorName { get; }

	/// <summary>Gets the source generator version propagated to code writers.</summary>
	public string GeneratorVersion { get; }

	/// <summary>Gets whether created code writers validate undisposed scopes.</summary>
	public bool ValidateCodeWriterScopes { get; init; }

	/// <summary>Gets whether the source generator is disabled by build configuration.</summary>
	public bool IsSourceGeneratorDisabled { get; init; }

	/// <summary>Gets whether source-generator logging is active for this generation context.</summary>
	public bool IsLoggingEnabled { get; init; }
}
