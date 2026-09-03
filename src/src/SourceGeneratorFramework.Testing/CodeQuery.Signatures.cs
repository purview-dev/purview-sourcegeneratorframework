using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Testing;

public sealed partial class CodeQuery
{
	/// <summary>
	/// Determines whether a method or constructor declaration matches the given parameter types.
	/// </summary>
	/// <remarks>
	/// Parameter types are resolved through the query's <see cref="Compilation"/> and matched with the
	/// framework's <c>MatchesTypeReference</c> semantics: nullable <i>value</i> types are significant
	/// (<c>int?</c> does not match <c>int</c>) while nullable <i>reference</i> annotations are metadata.
	/// </remarks>
	public bool HasParameters(BaseMethodDeclarationSyntax method, params TypeReference[] expected)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (expected is null)
			throw new ArgumentNullException(nameof(expected));

		var parameters = method.ParameterList.Parameters;
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

	/// <summary>
	/// Determines whether a method declaration's return type matches the given reference.
	/// </summary>
	public bool HasReturnType(string methodName, TypeReference returnType)
	{
		if (string.IsNullOrWhiteSpace(methodName))
			throw new ArgumentException("The method name cannot be null or whitespace.", nameof(methodName));
		if (returnType is null)
			throw new ArgumentNullException(nameof(returnType));

		if (!TryGetMethod(methodName, out var method))
			return false;

		// If the method has no return type (e.g., it's a constructor), it cannot match any reference.
		return method!.ReturnType is { } returnTypeSyntax && Matches(returnTypeSyntax, returnType);
	}

	/// <summary>
	/// Determines whether a type syntax resolves to the given reference, using the query's compilation.
	/// </summary>
	public bool Matches(TypeSyntax typeSyntax, TypeReference reference)
	{
		if (typeSyntax is null)
			throw new ArgumentNullException(nameof(typeSyntax));
		if (reference is null)
			throw new ArgumentNullException(nameof(reference));

		var compilation = Compilation;
		if (compilation is null || !compilation.ContainsSyntaxTree(typeSyntax.SyntaxTree))
		{
			throw new InvalidOperationException(
				"The query's compilation does not contain the syntax tree being matched. Construct the query from the compilation's own trees (for example via the Generated/Output/FixedCode adapters) or use the syntactic Get/Has overloads."
			);
		}

		// Delegate to the reference's MatchesTypeReference method, which handles symbol resolution and comparison.
		return reference.MatchesTypeReference(typeSyntax, compilation.GetSemanticModel(typeSyntax.SyntaxTree));
	}
}

/// <summary>
/// Signature inspection helpers for members obtained from a <see cref="CodeQuery"/>. These support chaining
/// from a member or type declaration, for example <c>query.GetClass("C").HasMethod(query, "M", intType)</c>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CodeQuerySignatureExtensions
{
	/// <summary>
	/// Creates a nested query scoped to this node, enabling chained searches such as
	/// <c>query.GetClass("C").Query(query).GetMethod("M")</c>.
	/// </summary>
	public static CodeQuery Query(this SyntaxNode node, CodeQuery parent)
	{
		if (node is null)
			throw new ArgumentNullException(nameof(node));
		if (parent is null)
			throw new ArgumentNullException(nameof(parent));

		// Delegate to the parent query's In method, which creates a new query scoped to the given node.
		return parent.In(node);
	}

	/// <summary>
	/// Determines whether the method's or constructor's parameters match the given types, resolved through the
	/// query's compilation.
	/// </summary>
	public static bool HasParameters(
		this BaseMethodDeclarationSyntax method,
		CodeQuery query,
		params TypeReference[] expected
	)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (query is null)
			throw new ArgumentNullException(nameof(query));

		// Delegate to the query's HasParameters method, which handles the parameter count and type matching.
		return query.HasParameters(method, expected);
	}

	/// <summary>
	/// Determines whether the method's return type matches the given reference, resolved through the query's
	/// compilation.
	/// </summary>
	public static bool HasReturnType(this MethodDeclarationSyntax method, CodeQuery query, TypeReference returnType)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (query is null)
			throw new ArgumentNullException(nameof(query));
		if (returnType is null)
			throw new ArgumentNullException(nameof(returnType));

		// If the method has no return type (e.g., it's a constructor), it cannot match any reference.
		return method.ReturnType is { } returnTypeSyntax && query.Matches(returnTypeSyntax, returnType);
	}

	/// <summary>
	/// Determines whether the property's type matches the given reference, resolved through the query's
	/// compilation.
	/// </summary>
	public static bool HasType(this PropertyDeclarationSyntax property, CodeQuery query, TypeReference propertyType)
	{
		if (property is null)
			throw new ArgumentNullException(nameof(property));
		if (query is null)
			throw new ArgumentNullException(nameof(query));
		if (propertyType is null)
			throw new ArgumentNullException(nameof(propertyType));

		// The property type is always non-nullable in C# syntax, so we can directly match it with the expected type reference.
		return query.Matches(property.Type, propertyType);
	}

	/// <summary>
	/// Determines whether the indexer's type matches the given reference, resolved through the query's
	/// compilation.
	/// </summary>
	public static bool HasType(this IndexerDeclarationSyntax indexer, CodeQuery query, TypeReference indexerType)
	{
		if (indexer is null)
			throw new ArgumentNullException(nameof(indexer));
		if (query is null)
			throw new ArgumentNullException(nameof(query));
		if (indexerType is null)
			throw new ArgumentNullException(nameof(indexerType));

		// The indexer type is always non-nullable in C# syntax, so we can directly match it with the expected type reference.
		return query.Matches(indexer.Type, indexerType);
	}
}
