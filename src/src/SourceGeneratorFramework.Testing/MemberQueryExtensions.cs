using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Member query extensions that chain from a type declaration obtained from a <see cref="CodeQuery"/>, for
/// example <c>query.GetClass("Service").HasMethod(query, "Add", intType)</c> or
/// <c>query.GetRecord("Person").HasConstructor(query, stringType)</c>.
/// </summary>
public static class MemberQueryExtensions
{
	// ---------------------------------------------------------------------------------------------
	// Properties
	// ---------------------------------------------------------------------------------------------

	/// <summary>Gets a property declared on the type, optionally matching its type.</summary>
	public static PropertyDeclarationSyntax GetProperty(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		string name,
		TypeReference? propertyType = null
	) =>
		type.TryGetProperty(query, name, out var property, propertyType)
			? property!
			: throw new SyntaxNotFoundException(
				$"No property named '{name}' was found on '{type.Identifier.ValueText}'."
			);

	/// <summary>Determines whether the type declares a property with the given name, optionally matching its type.</summary>
	public static bool HasProperty(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		string name,
		TypeReference? propertyType = null
	) => type.TryGetProperty(query, name, out _, propertyType);

	/// <summary>Attempts to get a property declared on the type, optionally matching its type.</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public static bool TryGetProperty(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		string name,
		out PropertyDeclarationSyntax? property,
		TypeReference? propertyType = null
	)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));
		if (query is null)
			throw new ArgumentNullException(nameof(query));
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("The property name cannot be null or whitespace.", nameof(name));

		foreach (var candidate in type.Members.OfType<PropertyDeclarationSyntax>())
		{
			if (candidate.Identifier.ValueText != name)
				continue;

			if (propertyType is not null && !query.Matches(candidate.Type, propertyType))
				continue;

			property = candidate;
			return true;
		}

		property = null;
		return false;
	}

	// ---------------------------------------------------------------------------------------------
	// Indexers
	// ---------------------------------------------------------------------------------------------

	/// <summary>Gets an indexer declared on the type, optionally matching its type and index parameters.</summary>
	public static IndexerDeclarationSyntax GetIndexer(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		TypeReference? indexerType = null,
		params TypeReference[]? indexParameters
	) =>
		type.TryGetIndexer(query, out var indexer, indexerType, indexParameters)
			? indexer!
			: throw new SyntaxNotFoundException($"No matching indexer was found on '{type.Identifier.ValueText}'.");

	/// <summary>Determines whether the type declares an indexer, optionally matching its type and index parameters.</summary>
	public static bool HasIndexer(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		TypeReference? indexerType = null,
		params TypeReference[]? indexParameters
	) => type.TryGetIndexer(query, out _, indexerType, indexParameters);

	/// <summary>Attempts to get an indexer declared on the type, optionally matching its type and index parameters.</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public static bool TryGetIndexer(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		out IndexerDeclarationSyntax? indexer,
		TypeReference? indexerType = null,
		params TypeReference[]? indexParameters
	)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));
		if (query is null)
			throw new ArgumentNullException(nameof(query));

		var expected = indexParameters ?? [];
		foreach (var candidate in type.Members.OfType<IndexerDeclarationSyntax>())
		{
			if (indexerType is not null && !query.Matches(candidate.Type, indexerType))
				continue;

			if (expected.Length > 0 && !IndexerParametersMatch(query, candidate, expected))
				continue;

			indexer = candidate;
			return true;
		}

		indexer = null;
		return false;
	}

	// ---------------------------------------------------------------------------------------------
	// Methods
	// ---------------------------------------------------------------------------------------------

	/// <summary>Gets a method declared on the type, optionally matching its parameter types.</summary>
	public static MethodDeclarationSyntax GetMethod(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		string name,
		params TypeReference[]? parameters
	) =>
		type.TryGetMethod(query, name, out var method, parameters)
			? method!
			: throw new SyntaxNotFoundException(
				$"No method named '{name}' was found on '{type.Identifier.ValueText}'."
			);

	/// <summary>Determines whether the type declares a method with the given name, optionally matching its parameter types.</summary>
	public static bool HasMethod(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		string name,
		params TypeReference[]? parameters
	) => type.TryGetMethod(query, name, out _, parameters);

	/// <summary>Attempts to get a method declared on the type, optionally matching its parameter types.</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public static bool TryGetMethod(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		string name,
		out MethodDeclarationSyntax? method,
		params TypeReference[]? parameters
	)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));
		if (query is null)
			throw new ArgumentNullException(nameof(query));
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("The method name cannot be null or whitespace.", nameof(name));

		var expected = parameters ?? [];
		foreach (var candidate in type.Members.OfType<MethodDeclarationSyntax>())
		{
			if (candidate.Identifier.ValueText != name)
				continue;

			if (expected.Length > 0 && !query.HasParameters(candidate, expected))
				continue;

			method = candidate;
			return true;
		}

		method = null;
		return false;
	}

	/// <summary>
	/// Determines whether the type declares a method with the given name and return type.
	/// </summary>
	public static bool HasMethodReturnType(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		string name,
		TypeReference returnType
	)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));
		if (query is null)
			throw new ArgumentNullException(nameof(query));
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("The method name cannot be null or whitespace.", nameof(name));
		if (returnType is null)
			throw new ArgumentNullException(nameof(returnType));

		foreach (var candidate in type.Members.OfType<MethodDeclarationSyntax>())
		{
			if (candidate.Identifier.ValueText != name)
				continue;

			if (candidate.ReturnType is { } returnTypeSyntax && query.Matches(returnTypeSyntax, returnType))
				return true;
		}

		return false;
	}

	// ---------------------------------------------------------------------------------------------
	// Constructors
	// ---------------------------------------------------------------------------------------------

	/// <summary>Gets a constructor declared on the type, optionally matching its parameter types.</summary>
	public static ConstructorDeclarationSyntax GetConstructor(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		params TypeReference[]? parameters
	) =>
		type.TryGetConstructor(query, out var constructor, parameters)
			? constructor!
			: throw new SyntaxNotFoundException($"No constructor was found on '{type.Identifier.ValueText}'.");

	/// <summary>Determines whether the type declares a constructor, optionally matching its parameter types.</summary>
	public static bool HasConstructor(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		params TypeReference[]? parameters
	) => type.TryGetConstructor(query, out _, parameters);

	/// <summary>Attempts to get a constructor declared on the type, optionally matching its parameter types.</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public static bool TryGetConstructor(
		this TypeDeclarationSyntax type,
		CodeQuery query,
		out ConstructorDeclarationSyntax? constructor,
		params TypeReference[]? parameters
	)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));
		if (query is null)
			throw new ArgumentNullException(nameof(query));

		var expected = parameters ?? [];
		foreach (var candidate in type.Members.OfType<ConstructorDeclarationSyntax>())
		{
			if (expected.Length > 0 && !query.HasParameters(candidate, expected))
				continue;

			constructor = candidate;
			return true;
		}

		constructor = null;
		return false;
	}

	static bool IndexerParametersMatch(CodeQuery query, IndexerDeclarationSyntax indexer, TypeReference[] expected)
	{
		var parameters = indexer.ParameterList.Parameters;
		if (parameters.Count != expected.Length)
			return false;

		for (var index = 0; index < parameters.Count; index++)
		{
			var typeSyntax = parameters[index].Type;
			if (typeSyntax is null || !query.Matches(typeSyntax, expected[index]))
				return false;
		}

		return true;
	}
}
