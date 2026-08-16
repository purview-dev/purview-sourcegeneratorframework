using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// Provides a base context for source generation, including the compilation, a shared <see cref="CodeWriter"/>, and helper methods to resolve symbols.
/// </summary>
/// <remarks>
/// Equality is based only on the cache-friendly inputs exposed as properties (assembly name, language,
/// generator identity, and scope validation). The <see cref="Compilation"/> and <see cref="CodeWriter"/>
/// are intentionally excluded so the context remains stable across incremental pipeline runs.
/// </remarks>
public record class GenerationContext
{
	/// <summary>
	/// The MSBuild property that controls validation of undisposed <see cref="CodeWriter"/> scopes.
	/// </summary>
	public const string ValidateCodeWriterScopesBuildProperty =
		"PurviewSourceGeneratorFrameworkValidateCodeWriterScopes";

	/// <summary>
	/// Initializes a generation context.
	/// </summary>
	/// <param name="compilation">The compilation being processed.</param>
	/// <param name="generatorName">The source generator name propagated to code writers.</param>
	/// <param name="generatorVersion">The source generator version propagated to code writers.</param>
	/// <param name="validateCodeWriterScopes">
	/// Whether code writers created by this context validate that all disposable scopes are closed.
	/// </param>
	public GenerationContext(
		Compilation compilation,
		string generatorName,
		string generatorVersion,
		bool validateCodeWriterScopes = false
	)
	{
		Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
		AssemblyName = compilation.AssemblyName ?? string.Empty;
		Language = compilation.Language;
		GeneratorName = generatorName;
		GeneratorVersion = generatorVersion;
		ValidateCodeWriterScopes = validateCodeWriterScopes;
		CodeWriter = CreateCodeWriter();
	}

	/// <summary>Gets the assembly name of the compilation being processed.</summary>
	public string AssemblyName { get; }

	/// <summary>Gets the language of the compilation being processed.</summary>
	public string Language { get; }

	/// <summary>Gets the compilation being processed.</summary>
	public Compilation Compilation { get; }

	/// <summary>Gets whether created code writers validate undisposed scopes.</summary>
	public bool ValidateCodeWriterScopes { get; private set; }

	/// <summary>Gets the source generator name propagated to created code writers.</summary>
	public string GeneratorName { get; }

	/// <summary>Gets the source generator version propagated to code writers.</summary>
	public string GeneratorVersion { get; }

	/// <summary>Gets the default code writer owned by this context.</summary>
	public CodeWriter CodeWriter { get; private set; }

	/// <inheritdoc />
	public virtual bool Equals(GenerationContext? other)
	{
		if (other is null)
			return false;
		if (ReferenceEquals(this, other))
			return true;

		// Equality is based only on the cache-friendly inputs exposed as properties (assembly name, language,
		return AssemblyName == other.AssemblyName
			&& Language == other.Language
			&& GeneratorName == other.GeneratorName
			&& GeneratorVersion == other.GeneratorVersion
			&& ValidateCodeWriterScopes == other.ValidateCodeWriterScopes;
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		unchecked
		{
			var hash = StringComparer.Ordinal.GetHashCode(AssemblyName);
			hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Language);
			hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(GeneratorName);
			hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(GeneratorVersion);
			hash = (hash * 397) ^ ValidateCodeWriterScopes.GetHashCode();
			return hash;
		}
	}

	/// <summary>
	/// Creates a code writer configured from this generation context's build properties
	/// and sets the <see cref="CodeWriter"/> property to the new instance.
	/// </summary>
	/// <returns>A new independently owned code writer, the same instance assigned to the <see cref="CodeWriter"/> property.</returns>
	public CodeWriter CreateCodeWriter() =>
		CodeWriter = new(GeneratorName, GeneratorVersion, throwOnUnclosedScopes: ValidateCodeWriterScopes);

	/// <summary>
	/// Applies CodeWriter scope validation discovered by the incremental pipeline.
	/// </summary>
	/// <remarks>
	/// Configuration occurs during context creation, before the context is published to downstream
	/// incremental providers.
	/// </remarks>
	internal void ConfigureCodeWriterScopeValidation(bool enabled)
	{
		if (ValidateCodeWriterScopes == enabled)
			return;

		ValidateCodeWriterScopes = enabled;
		CreateCodeWriter();
	}

	/// <summary>
	/// Resolves a type by its fully qualified metadata name.
	/// </summary>
	public INamedTypeSymbol? GetTypeByMetadataName(string fullyQualifiedName) =>
		Compilation.GetTypeByMetadataName(fullyQualifiedName);

	/// <summary>
	/// Resolves a type from a <see cref="TypeValueObject"/>.
	/// </summary>
	public INamedTypeSymbol? GetTypeByMetadataName(TypeValueObject type) =>
		GetTypeByMetadataName(type.MetadataFullName);
}
