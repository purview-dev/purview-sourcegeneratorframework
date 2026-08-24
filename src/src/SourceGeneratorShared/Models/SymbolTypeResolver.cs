using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// Resolves the type carried by any symbol, so that matching can be applied uniformly to members.
/// </summary>
public static class SymbolTypeResolver
{
	/// <summary>
	/// Returns the type of the given symbol: the declared type for fields, properties, events, parameters and
	/// locals, the return type for methods, the target for aliases, and the symbol itself for type symbols.
	/// </summary>
	public static ITypeSymbol? Resolve(ISymbol? symbol) =>
		symbol switch
		{
			ITypeSymbol type => type,
			IFieldSymbol field => field.Type,
			IPropertySymbol property => property.Type,
			IMethodSymbol method => method.ReturnType,
			IEventSymbol @event => @event.Type,
			IParameterSymbol parameter => parameter.Type,
			ILocalSymbol local => local.Type,
			IDiscardSymbol discard => discard.Type,
			IAliasSymbol alias => alias.Target as ITypeSymbol,
			_ => null,
		};
}
