using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Testing;

public sealed partial class CodeQuery
{
	// ---------------------------------------------------------------------------------------------
	// Methods
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Gets a method declaration by name, optionally matching its parameter types.
	/// </summary>
	/// <exception cref="SyntaxNotFoundException">No method matched.</exception>
	public MethodDeclarationSyntax GetMethod(string name, params TypeReference[]? parameters) =>
		TryGetMethod(name, out var method, parameters)
			? method!
			: throw new SyntaxNotFoundException(
				$"No method named '{name}' was found in the {ScopeDescription()}{(parameters is { Length: > 0 } ? " with the specified parameters" : "")}."
			);

	/// <summary>
	/// Determines whether a method declaration with the given name, optionally matching parameter types, exists.
	/// </summary>
	public bool HasMethod(string name, params TypeReference[]? parameters) => TryGetMethod(name, out _, parameters);

	/// <summary>
	/// Attempts to get a method declaration by name, optionally matching its parameter types.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public bool TryGetMethod(string name, out MethodDeclarationSyntax? method, params TypeReference[]? parameters)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("The method name cannot be null or whitespace.", nameof(name));

		var expected = parameters ?? [];
		return TryFind(
			name,
			static candidate => candidate.Identifier.ValueText,
			expected.Length == 0 ? null : candidate => HasParameters(candidate, expected),
			out method
		);
	}

	// ---------------------------------------------------------------------------------------------
	// Type declarations
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Gets a type declaration (class, struct, interface, record, enum or delegate) by name.
	/// </summary>
	public MemberDeclarationSyntax GetTypeDeclaration(string name, string? @namespace = null) =>
		Get<MemberDeclarationSyntax>(node => IsTypeDeclarationMatch(node, name) && NamespaceMatches(node, @namespace));

	/// <summary>
	/// Determines whether a type declaration with the given name exists.
	/// </summary>
	public bool HasTypeDeclaration(string name, string? @namespace = null) =>
		Has<MemberDeclarationSyntax>(node => IsTypeDeclarationMatch(node, name) && NamespaceMatches(node, @namespace));

	/// <summary>
	/// Gets a class declaration by name, optionally within a namespace.
	/// </summary>
	public ClassDeclarationSyntax GetClass(string name, string? @namespace = null) =>
		FindByName<ClassDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Determines whether a class declaration with the given name exists, optionally within a namespace.
	/// </summary>
	public bool HasClass(string name, string? @namespace = null) => HasByName<ClassDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Attempts to get a class declaration by name, optionally within a namespace.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public bool TryGetClass(string name, out ClassDeclarationSyntax? declaration, string? @namespace = null) =>
		TryFindByName(name, out declaration, @namespace);

	/// <summary>
	/// Gets a struct declaration by name, optionally within a namespace.
	/// </summary>
	public StructDeclarationSyntax GetStruct(string name, string? @namespace = null) =>
		FindByName<StructDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Determines whether a struct declaration with the given name exists, optionally within a namespace.
	/// </summary>
	public bool HasStruct(string name, string? @namespace = null) =>
		HasByName<StructDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Gets an interface declaration by name, optionally within a namespace.
	/// </summary>
	public InterfaceDeclarationSyntax GetInterface(string name, string? @namespace = null) =>
		FindByName<InterfaceDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Determines whether an interface declaration with the given name exists, optionally within a namespace.
	/// </summary>
	public bool HasInterface(string name, string? @namespace = null) =>
		HasByName<InterfaceDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Gets an enum declaration by name, optionally within a namespace.
	/// </summary>
	public EnumDeclarationSyntax GetEnum(string name, string? @namespace = null) =>
		FindByName<EnumDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Determines whether an enum declaration with the given name exists, optionally within a namespace.
	/// </summary>
	public bool HasEnum(string name, string? @namespace = null) => HasByName<EnumDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Gets a delegate declaration by name, optionally within a namespace.
	/// </summary>
	public DelegateDeclarationSyntax GetDelegate(string name, string? @namespace = null) =>
		FindByName<DelegateDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Determines whether a delegate declaration with the given name exists, optionally within a namespace.
	/// </summary>
	public bool HasDelegate(string name, string? @namespace = null) =>
		HasByName<DelegateDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Gets a record declaration by name, optionally within a namespace.
	/// </summary>
	public RecordDeclarationSyntax GetRecord(string name, string? @namespace = null) =>
		FindByName<RecordDeclarationSyntax>(name, @namespace);

	/// <summary>
	/// Determines whether a record declaration with the given name exists, optionally within a namespace.
	/// </summary>
	public bool HasRecord(string name, string? @namespace = null) =>
		HasByName<RecordDeclarationSyntax>(name, @namespace);

	// ---------------------------------------------------------------------------------------------
	// Members
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Gets a property declaration by name.
	/// </summary>
	public PropertyDeclarationSyntax GetProperty(string name) =>
		TryGetProperty(name, out var property)
			? property!
			: throw new SyntaxNotFoundException($"No property named '{name}' was found in the {ScopeDescription()}.");

	/// <summary>
	/// Determines whether a property declaration with the given name exists.
	/// </summary>
	public bool HasProperty(string name) => TryGetProperty(name, out _);

	/// <summary>
	/// Attempts to get a property declaration by name.
	/// </summary>
	public bool TryGetProperty(string name, out PropertyDeclarationSyntax? property) =>
		TryFindByName(name, out property);

	/// <summary>
	/// Gets a field declaration by name.
	/// </summary>
	/// <remarks>
	/// Finds a <see cref="VariableDeclaratorSyntax"/> by identifier and returns its declaring field.
	/// </remarks>
	public FieldDeclarationSyntax GetField(string name) =>
		TryGetField(name, out var field)
			? field!
			: throw new SyntaxNotFoundException($"No field named '{name}' was found in the {ScopeDescription()}.");

	/// <summary>
	/// Determines whether a field declaration with the given name exists.
	/// </summary>
	public bool HasField(string name) => TryGetField(name, out _);

	/// <summary>
	/// Attempts to get a field declaration by name.
	/// </summary>
	public bool TryGetField(string name, out FieldDeclarationSyntax? field)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("The field name cannot be null or whitespace.", nameof(name));

		foreach (var tree in Trees)
		{
			foreach (var declarator in RootOf(tree).DescendantNodes().OfType<VariableDeclaratorSyntax>())
			{
				if (declarator.Identifier.ValueText != name)
					continue;

				if (declarator.Parent is not VariableDeclarationSyntax { Parent: FieldDeclarationSyntax candidate })
					continue;

				field = candidate;
				return true;
			}
		}

		field = null;
		return false;
	}

	/// <summary>
	/// Gets a constructor declaration by the name of its containing type.
	/// </summary>
	public ConstructorDeclarationSyntax GetConstructor(string containingTypeName) =>
		FindByName<ConstructorDeclarationSyntax>(containingTypeName);

	/// <summary>
	/// Determines whether a constructor declaration for the given containing type exists.
	/// </summary>
	public bool HasConstructor(string containingTypeName) =>
		HasByName<ConstructorDeclarationSyntax>(containingTypeName);

	/// <summary>
	/// Gets a namespace declaration (block or file-scoped) by its dotted name.
	/// </summary>
	public BaseNamespaceDeclarationSyntax GetNamespace(string name) =>
		FindByName<BaseNamespaceDeclarationSyntax>(
			name,
			null,
			namespaceDeclaration => namespaceDeclaration.Name.ToString()
		);

	/// <summary>
	/// Determines whether a namespace declaration with the given dotted name exists.
	/// </summary>
	public bool HasNamespace(string name) =>
		HasByName<BaseNamespaceDeclarationSyntax>(
			name,
			null,
			namespaceDeclaration => namespaceDeclaration.Name.ToString()
		);

	// ---------------------------------------------------------------------------------------------
	// Shared
	// ---------------------------------------------------------------------------------------------

	static bool IsTypeDeclarationMatch(MemberDeclarationSyntax node, string name) =>
		node switch
		{
			ClassDeclarationSyntax @class => @class.Identifier.ValueText == name,
			StructDeclarationSyntax @struct => @struct.Identifier.ValueText == name,
			InterfaceDeclarationSyntax @interface => @interface.Identifier.ValueText == name,
			EnumDeclarationSyntax @enum => @enum.Identifier.ValueText == name,
			DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText == name,
			RecordDeclarationSyntax record => record.Identifier.ValueText == name,
			_ => false,
		};

	T FindByName<T>(string name, string? @namespace = null, Func<T, string>? getName = null)
		where T : SyntaxNode =>
		TryFindByName(name, out var node, @namespace, getName)
			? node!
			: throw new SyntaxNotFoundException(
				$"No {typeof(T).Name} named '{name}' was found in the {ScopeDescription()}{(string.IsNullOrEmpty(@namespace) ? "" : $" within namespace '{@namespace}'")}."
			);

	bool HasByName<T>(string name, string? @namespace = null, Func<T, string>? getName = null)
		where T : SyntaxNode => TryFindByName(name, out _, @namespace, getName);

	bool TryFindByName<T>(string name, out T? node, string? @namespace = null, Func<T, string>? getName = null)
		where T : SyntaxNode
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("The name cannot be null or whitespace.", nameof(name));

		// If a namespace is provided, we need to check that the node's declared namespace matches.
		return TryFind(
			name,
			getName is null ? static candidate => GetIdentifier(candidate) : getName,
			string.IsNullOrEmpty(@namespace) ? null : candidate => NamespaceMatches(candidate, @namespace),
			out node
		);
	}

	static bool NamespaceMatches(SyntaxNode node, string? @namespace) =>
		string.IsNullOrEmpty(@namespace)
		|| string.Equals(TypeSyntaxFacts.GetDeclaredNamespace(node), @namespace, StringComparison.Ordinal);

	bool TryFind<T>(string name, Func<T, string> getName, Func<T, bool>? additional, out T? node)
		where T : SyntaxNode
	{
		foreach (var tree in Trees)
		{
			foreach (var candidate in RootOf(tree).DescendantNodes().OfType<T>())
			{
				if (getName(candidate) != name)
					continue;

				if (additional is not null && !additional(candidate))
					continue;

				node = candidate;
				return true;
			}
		}

		node = null;
		return false;
	}

	static string GetIdentifier<T>(T node)
		where T : SyntaxNode =>
		node switch
		{
			MethodDeclarationSyntax method => method.Identifier.ValueText,
			ClassDeclarationSyntax @class => @class.Identifier.ValueText,
			StructDeclarationSyntax @struct => @struct.Identifier.ValueText,
			InterfaceDeclarationSyntax @interface => @interface.Identifier.ValueText,
			EnumDeclarationSyntax @enum => @enum.Identifier.ValueText,
			DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
			RecordDeclarationSyntax record => record.Identifier.ValueText,
			PropertyDeclarationSyntax property => property.Identifier.ValueText,
			ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
			_ => string.Empty,
		};
}
