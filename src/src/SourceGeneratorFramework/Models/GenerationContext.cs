using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.Models;

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
}

/// <summary>
/// Provides execution services for source generation, including the compilation, immutable
/// settings, optional logging, and symbol-resolution helpers.
/// </summary>
/// <remarks>Initializes a generation context.</remarks>
public class GenerationContext(Compilation compilation, GenerationSettings settings, ISourceGenLogger? logger = null)
	: ISourceGenLogger
{
	/// <summary>The MSBuild property that controls validation of undisposed code-writer scopes.</summary>
	public const string ValidateCodeWriterScopesBuildProperty =
		"PurviewSourceGeneratorFrameworkValidateCodeWriterScopes";

	/// <summary>Gets the assembly name of the compilation being processed.</summary>
	public string AssemblyName { get; } = compilation.AssemblyName ?? string.Empty;

	/// <summary>Gets the language of the compilation being processed.</summary>
	public string Language { get; } = compilation.Language;

	/// <summary>Gets the compilation being processed.</summary>
	public Compilation Compilation { get; } = compilation ?? throw new ArgumentNullException(nameof(compilation));

	/// <summary>Gets the immutable generation settings.</summary>
	public GenerationSettings Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

	/// <summary>Gets the optional logger, used during test execution.</summary>
	public ISourceGenLogger? Logger { get; } = logger;

	/// <summary>Creates a new independently owned code writer.</summary>
	public CodeWriter CreateCodeWriter() =>
		new(
			Settings.GeneratorName,
			Settings.GeneratorVersion,
			throwOnUnclosedScopes: Settings.ValidateCodeWriterScopes
		);

	/// <summary>Resolves a type by its fully qualified metadata name.</summary>
	public INamedTypeSymbol? GetTypeByMetadataName(string fullyQualifiedName) =>
		Compilation.GetTypeByMetadataName(fullyQualifiedName);

	/// <summary>Resolves a type from a structured type value.</summary>
	public INamedTypeSymbol? GetTypeByMetadataName(TypeValueObject type) =>
		GetTypeByMetadataName(type.MetadataFullName);

	/// <inheritdoc />
	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) =>
		Logger?.Log(level, indentation, message, args);
}
