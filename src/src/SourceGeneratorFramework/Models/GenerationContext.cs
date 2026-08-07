using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// Provides a base context for source generation, including the compilation, a shared <see cref="CodeWriter"/>, and helper methods to resolve symbols.
/// </summary>
public record class GenerationContext(Compilation Compilation)
{
	/// <summary>
	/// Gets or sets the <see cref="CodeWriter"/> used for generating source code. This property must be set before generating any source code.
	/// </summary>
	public CodeWriter CodeWriter
	{
		get;
		set => field = value ?? throw new ArgumentNullException(nameof(value));
	} = default!;

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
