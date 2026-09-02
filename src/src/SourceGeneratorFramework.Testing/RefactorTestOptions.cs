using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>Options that configure a refactoring test run.</summary>
public record RefactorTestOptions : SourceGeneratorTestOptions
{
	/// <summary>Gets the index of the registered code action to apply.</summary>
	public int CodeActionIndex { get; init; }

	/// <summary>Gets the equivalence key used to select a registered code action.</summary>
	/// <remarks>When specified, this takes precedence over <see cref="CodeActionIndex"/>.</remarks>
	public string? EquivalenceKey { get; init; }

	/// <summary>Gets the span the refactoring is triggered on.</summary>
	/// <remarks>Either <see cref="Span"/> or <see cref="NodeSelector"/> must be provided.</remarks>
	public TextSpan? Span { get; init; }

	/// <summary>
	/// Gets a selector that locates the node the refactoring is triggered on, using the input compilation's
	/// <see cref="CodeQuery"/>. For example, <c>query =&gt; query.GetMethod("M")</c>.
	/// </summary>
	/// <remarks>Either <see cref="Span"/> or <see cref="NodeSelector"/> must be provided.</remarks>
	public Func<CodeQuery, SyntaxNode>? NodeSelector { get; init; }
}
