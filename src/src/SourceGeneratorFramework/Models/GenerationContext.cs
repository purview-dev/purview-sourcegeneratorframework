using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// Provides a base context for source generation, including the compilation, a shared <see cref="CodeWriter"/>, and helper methods to resolve symbols.
/// </summary>
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
	/// <param name="validateCodeWriterScopes">
	/// Whether code writers created by this context validate that all disposable scopes are closed.
	/// </param>
	public GenerationContext(Compilation compilation, bool validateCodeWriterScopes = false)
	{
		Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
		ValidateCodeWriterScopes = validateCodeWriterScopes;
		CodeWriter = CreateCodeWriter();
	}

	/// <summary>Gets the compilation being processed.</summary>
	public Compilation Compilation { get; }

	/// <summary>Gets whether created code writers validate undisposed scopes.</summary>
	public bool ValidateCodeWriterScopes { get; private set; }

	/// <summary>Gets the default code writer owned by this context.</summary>
	public CodeWriter CodeWriter { get; private set; }

	/// <summary>
	/// Creates a code writer configured from this generation context's build properties
	/// and sets the <see cref="CodeWriter"/> property to the new instance.
	/// </summary>
	/// <returns>A new independently owned code writer, the same instance assigned to the <see cref="CodeWriter"/> property.</returns>
	public CodeWriter CreateCodeWriter() => CodeWriter = new(ValidateCodeWriterScopes);

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
		GetTypeByMetadataName(type.SymbolFullName);
}
