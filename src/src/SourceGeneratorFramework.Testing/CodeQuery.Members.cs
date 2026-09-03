using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Testing;

public sealed partial class CodeQuery
{
	// ---------------------------------------------------------------------------------------------
	// Operators
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Gets the first operator declaration matching the given token, such as <c>==</c> or <c>implicit</c>.
	/// </summary>
	/// <exception cref="SyntaxNotFoundException">No operator matched.</exception>
	public OperatorDeclarationSyntax GetOperator(string operatorToken) =>
		TryGetOperator(operatorToken, out var @operator)
			? @operator!
			: throw new SyntaxNotFoundException(
				$"No operator '{operatorToken}' was found in the {ScopeDescription()}."
			);

	/// <summary>
	/// Determines whether an operator declaration with the given token exists.
	/// </summary>
	public bool HasOperator(string operatorToken) => TryGetOperator(operatorToken, out _);

	/// <summary>
	/// Attempts to get the first operator declaration matching the given token.
	/// </summary>
	public bool TryGetOperator(string operatorToken, out OperatorDeclarationSyntax? @operator)
	{
		if (string.IsNullOrWhiteSpace(operatorToken))
			throw new ArgumentException("The operator token cannot be null or whitespace.", nameof(operatorToken));

		foreach (var tree in Trees)
		{
			foreach (var candidate in RootOf(tree).DescendantNodes().OfType<OperatorDeclarationSyntax>())
			{
				if (candidate.OperatorToken.ValueText == operatorToken)
				{
					@operator = candidate;
					return true;
				}
			}
		}

		@operator = null;
		return false;
	}

	/// <summary>
	/// Gets the first conversion operator matching the given keyword, such as <c>implicit</c> or <c>explicit</c>.
	/// </summary>
	/// <exception cref="SyntaxNotFoundException">No conversion operator matched.</exception>
	public ConversionOperatorDeclarationSyntax GetConversionOperator(string keyword) =>
		TryGetConversionOperator(keyword, out var conversion)
			? conversion!
			: throw new SyntaxNotFoundException(
				$"No '{keyword}' conversion operator was found in the {ScopeDescription()}."
			);

	/// <summary>
	/// Determines whether a conversion operator with the given keyword exists.
	/// </summary>
	public bool HasConversionOperator(string keyword) => TryGetConversionOperator(keyword, out _);

	/// <summary>
	/// Attempts to get the first conversion operator matching the given keyword.
	/// </summary>
	public bool TryGetConversionOperator(string keyword, out ConversionOperatorDeclarationSyntax? conversion)
	{
		if (keyword is not ("implicit" or "explicit"))
			throw new ArgumentException("The keyword must be 'implicit' or 'explicit'.", nameof(keyword));

		foreach (var tree in Trees)
		{
			foreach (var candidate in RootOf(tree).DescendantNodes().OfType<ConversionOperatorDeclarationSyntax>())
			{
				if (candidate.ImplicitOrExplicitKeyword.ValueText == keyword)
				{
					conversion = candidate;
					return true;
				}
			}
		}

		conversion = null;
		return false;
	}

	// ---------------------------------------------------------------------------------------------
	// Indexers
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Gets an indexer declaration whose parameters match the given types.
	/// </summary>
	/// <exception cref="SyntaxNotFoundException">No indexer matched.</exception>
	public IndexerDeclarationSyntax GetIndexer(params TypeReference[] parameters) =>
		TryGetIndexer(out var indexer, parameters)
			? indexer!
			: throw new SyntaxNotFoundException($"No indexer was found in the {ScopeDescription()}.");

	/// <summary>
	/// Determines whether an indexer declaration with the given parameter types exists.
	/// </summary>
	public bool HasIndexer(params TypeReference[] parameters) => TryGetIndexer(out _, parameters);

	/// <summary>
	/// Attempts to get an indexer declaration whose parameters match the given types.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public bool TryGetIndexer(out IndexerDeclarationSyntax? indexer, params TypeReference[] parameters)
	{
		var expected = parameters ?? [];
		foreach (var tree in Trees)
		{
			foreach (var candidate in RootOf(tree).DescendantNodes().OfType<IndexerDeclarationSyntax>())
			{
				if (expected.Length == 0 || MatchesParameters(candidate.ParameterList, expected))
				{
					indexer = candidate;
					return true;
				}
			}
		}

		indexer = null;
		return false;
	}

	bool MatchesParameters(BracketedParameterListSyntax parameterList, TypeReference[] expected)
	{
		var parameters = parameterList.Parameters;
		if (parameters.Count != expected.Length)
			return false;

		for (var index = 0; index < parameters.Count; index++)
		{
			var typeSyntax = parameters[index].Type;
			if (typeSyntax is null || !Matches(typeSyntax, expected[index]))
				return false;
		}

		return true;
	}

	// ---------------------------------------------------------------------------------------------
	// Attributes
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Gets all attribute applications on or within the given node.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
	public ImmutableArray<AttributeSyntax> GetAttributes(SyntaxNode node)
	{
		if (node is null)
			throw new ArgumentNullException(nameof(node));

		// Note: This intentionally returns attributes on the node itself and any nested nodes, such as parameters.
		return [.. node.DescendantNodes().OfType<AttributeSyntax>()];
	}

	/// <summary>
	/// Determines whether the given node has an attribute with the specified name.
	/// </summary>
	/// <remarks>
	/// The name may be supplied with or without the <c>Attribute</c> suffix.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
	public bool HasAttribute(SyntaxNode node, string name)
	{
		if (node is null)
			throw new ArgumentNullException(nameof(node));
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("The attribute name cannot be null or whitespace.", nameof(name));

		foreach (var attribute in node.DescendantNodes().OfType<AttributeSyntax>())
		{
			if (MatchesAttributeName(attribute, name))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Gets the first attribute with the specified name, or <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// The name may be supplied with or without the <c>Attribute</c> suffix.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
	public AttributeSyntax? GetAttribute(SyntaxNode node, string name)
	{
		if (node is null)
			throw new ArgumentNullException(nameof(node));
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("The attribute name cannot be null or whitespace.", nameof(name));

		foreach (var attribute in node.DescendantNodes().OfType<AttributeSyntax>())
		{
			if (MatchesAttributeName(attribute, name))
				return attribute;
		}

		return null;
	}

	static bool MatchesAttributeName(AttributeSyntax attribute, string name)
	{
		var rendered = attribute.Name.ToString();
		if (string.Equals(rendered, name, StringComparison.Ordinal))
			return true;

		// The attribute name may be supplied with or without the "Attribute" suffix, and may be fully qualified.
		return name.EndsWith("Attribute", StringComparison.Ordinal)
			? string.Equals(rendered, name, StringComparison.Ordinal)
			: string.Equals(rendered, name + "Attribute", StringComparison.Ordinal)
				|| string.Equals(rendered, $"global::{name}", StringComparison.Ordinal)
				|| string.Equals(rendered, $"global::{name}Attribute", StringComparison.Ordinal);
	}
}
