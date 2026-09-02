using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// A synchronous query over a set of syntax trees, optionally backed by a <see cref="Compilation"/> for
/// semantic resolution. Exposed by test results (see <c>CodeQueryResultExtensions</c>) so tests can locate a
/// syntax node — a method, class, property, and so on — and inspect it, including its parameter types.
/// </summary>
/// <remarks>
/// Every operation is synchronous: <c>SyntaxTree.GetRoot()</c> and
/// <c>Compilation.GetSemanticModel</c> are lazy, so repeated queries over test-sized payloads are cheap.
/// </remarks>
/// <remarks>Initializes a new query over the given trees.</remarks>
/// <param name="trees">The trees to search.</param>
/// <param name="compilation">
/// The compilation backing <paramref name="trees"/>, used to resolve symbols for type matching. May be
/// <see langword="null"/> when only syntactic matching is required.
/// </param>
/// <param name="isGenerated">Whether the trees represent generated code, used in error messages.</param>
public sealed partial class CodeQuery(
	ImmutableArray<SyntaxTree> trees,
	Compilation? compilation = null,
	bool isGenerated = false
)
{
	/// <summary>Gets the trees being searched.</summary>
	public ImmutableArray<SyntaxTree> Trees { get; } = trees.IsDefault ? [] : trees;

	/// <summary>Gets the compilation backing the trees, when one is available.</summary>
	public Compilation? Compilation { get; } = compilation;

	/// <summary>Gets whether the trees represent generated code, used in error messages.</summary>
	public bool IsGenerated { get; } = isGenerated;

	// ---------------------------------------------------------------------------------------------
	// Syntax trees
	// ---------------------------------------------------------------------------------------------

	/// <summary>Gets the tree whose file path ends with or equals the given name.</summary>
	/// <exception cref="SyntaxNotFoundException">No tree matched.</exception>
	public SyntaxTree GetSyntaxTree(string name) =>
		TryGetSyntaxTree(name, out var tree)
			? tree!
			: throw new SyntaxNotFoundException(
				$"No syntax tree named '{name}' was found in the {ScopeDescription()}."
			);

	/// <summary>Determines whether a tree whose file path ends with or equals the given name exists.</summary>
	public bool HasSyntaxTree(string name) => TryGetSyntaxTree(name, out _);

	/// <summary>Attempts to get the tree whose file path ends with or equals the given name.</summary>
	public bool TryGetSyntaxTree(string name, out SyntaxTree? tree)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("The tree name cannot be null or whitespace.", nameof(name));

		foreach (var candidate in Trees)
		{
			if (
				string.Equals(candidate.FilePath, name, StringComparison.Ordinal)
				|| candidate.FilePath.EndsWith(name, StringComparison.Ordinal)
			)
			{
				tree = candidate;
				return true;
			}
		}

		tree = null;
		return false;
	}

	// ---------------------------------------------------------------------------------------------
	// Generic
	// ---------------------------------------------------------------------------------------------

	/// <summary>Gets the first syntax node of the specified type, optionally matching a predicate.</summary>
	/// <exception cref="SyntaxNotFoundException">No node matched.</exception>
	public T Get<T>(Func<T, bool>? predicate = null)
		where T : SyntaxNode => TryGet(out var node, predicate) ? node! : throw NotFound<T>();

	/// <summary>Determines whether a syntax node of the specified type exists, optionally matching a predicate.</summary>
	public bool Has<T>(Func<T, bool>? predicate = null)
		where T : SyntaxNode => TryGet(out _, predicate);

	/// <summary>Attempts to get the first syntax node of the specified type, optionally matching a predicate.</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1021:Avoid out parameters")]
	public bool TryGet<T>(out T? node, Func<T, bool>? predicate = null)
		where T : SyntaxNode
	{
		foreach (var tree in Trees)
		{
			var root = tree.GetRoot();
			if (root is T rootNode && (predicate is null || predicate(rootNode)))
			{
				node = rootNode;
				return true;
			}

			foreach (var candidate in root.DescendantNodes().OfType<T>())
			{
				if (predicate is null || predicate(candidate))
				{
					node = candidate;
					return true;
				}
			}
		}

		node = null;
		return false;
	}

	SyntaxNotFoundException NotFound<T>()
		where T : SyntaxNode =>
		new($"No syntax node of type '{typeof(T).Name}' was found in the {ScopeDescription()}.");

	string ScopeDescription() => IsGenerated ? "generated code" : "code";
}
