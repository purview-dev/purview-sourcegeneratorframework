using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Helpers;

/// <summary>
/// Helpers for resolving symbols from a <see cref="Compilation"/>.
/// </summary>
public static class SymbolResolver
{
	/// <summary>
	/// Resolves a type by its fully qualified metadata name.
	/// </summary>
	public static INamedTypeSymbol? Resolve(Compilation compilation, string fullyQualifiedName) =>
		compilation is null
			? throw new ArgumentNullException(nameof(compilation))
			: compilation.GetTypeByMetadataName(fullyQualifiedName);

	/// <summary>
	/// Resolves a type from a <see cref="TypeValueObject"/>.
	/// </summary>
	public static INamedTypeSymbol? Resolve(Compilation compilation, TypeValueObject type) =>
		compilation is null
			? throw new ArgumentNullException(nameof(compilation))
			: Resolve(compilation, type.MetadataFullName);

	/// <summary>
	/// Resolves a type from a <see cref="TypeValueObject"/> and returns a value indicating whether it was found.
	/// </summary>
	public static bool TryResolve(Compilation compilation, TypeValueObject type, out INamedTypeSymbol? symbol)
	{
		symbol = Resolve(compilation, type);
		return symbol is not null;
	}

	/// <summary>
	/// Resolves a type by its fully qualified metadata name and returns a value indicating whether it was found.
	/// </summary>
	public static bool TryResolve(Compilation compilation, string fullyQualifiedName, out INamedTypeSymbol? symbol)
	{
		symbol = Resolve(compilation, fullyQualifiedName);
		return symbol is not null;
	}
}
