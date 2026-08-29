namespace Purview.SourceGeneratorFramework;

/// <summary>Describes immutable settings shared by a source-generation operation.</summary>
public sealed record GenerationSettings
{
	/// <summary>Initializes source-generation settings.</summary>
	/// <param name="generatorName">The name of the generator.</param>
	/// <param name="generatorVersion">The version of the generator. If null, defaults to "1.0.0.0".</param>
	/// <param name="disabledSourceGenMSBuildProperty">An optional MSBuild property name that disables the generator when set to true.</param>
	public GenerationSettings(
		string generatorName,
		string? generatorVersion = null,
		string? disabledSourceGenMSBuildProperty = null
	)
	{
		if (string.IsNullOrWhiteSpace(generatorName))
			throw new ArgumentException("Generator name cannot be null or whitespace.", nameof(generatorName));

		GeneratorName = generatorName;
		GeneratorVersion = generatorVersion ?? "1.0.0.0";
		DisabledSourceGenMSBuildProperty = disabledSourceGenMSBuildProperty;
	}

	/// <summary>Gets the source generator name propagated to code writers.</summary>
	public string GeneratorName { get; }

	/// <summary>Gets the source generator version propagated to code writers.</summary>
	public string GeneratorVersion { get; }

	/// <summary>Gets the optional MSBuild property name that disables the generator when set to true.</summary>
	public string? DisabledSourceGenMSBuildProperty { get; }

	/// <summary>Gets whether created code writers validate undisposed scopes.</summary>
	public bool ValidateCodeWriterScopes { get; init; }

	/// <summary>Gets whether the source generator is disabled by build configuration.</summary>
	public bool IsSourceGeneratorDisabled { get; init; }

	/// <summary>Gets whether source-generator logging is active for this generation context.</summary>
	public bool IsLoggingEnabled { get; init; }

	/// <summary>
	/// Creates a new generation settings instance for the specified generator type, using the type name and assembly version.
	/// </summary>
	/// <typeparam name="TGenerator">The type of the generator.</typeparam>
	/// <param name="disabledSourceGenMSBuildProperty">An optional MSBuild property name that disables the generator when set to true.</param>
	/// <returns>A new <see cref="GenerationSettings"/> instance.</returns>
	public static GenerationSettings Create<TGenerator>(string? disabledSourceGenMSBuildProperty = null)
	{
		var generatorType = typeof(TGenerator);

		return new(
			generatorType.Name,
			generatorType.Assembly.GetName().Version?.ToString(),
			disabledSourceGenMSBuildProperty
		);
	}
}
