using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Describes a disposable <see cref="CodeWriter"/> scope that was still open when source was
/// materialized.
/// </summary>
/// <param name="Kind">The kind of scope.</param>
/// <param name="Header">The block header, when available.</param>
/// <param name="OpeningStackTrace">The call stack captured when the scope was opened.</param>
public sealed record CodeWriterOpenScope(string Kind, string? Header, string OpeningStackTrace);

/// <summary>
/// The exception thrown when generated source is requested while one or more
/// <see cref="CodeWriter"/> scopes remain open.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1032:Implement standard exception constructors",
	Justification = "The open scope count is required context for this validation failure."
)]
public sealed class CodeWriterScopeValidationException : InvalidOperationException
{
	readonly ImmutableArray<CodeWriterOpenScope> _openScopes;

	/// <summary>
	/// Initializes a new instance of the <see cref="CodeWriterScopeValidationException"/> class.
	/// </summary>
	/// <param name="openScopes">The scopes that have not been disposed.</param>
	public CodeWriterScopeValidationException(IEnumerable<CodeWriterOpenScope> openScopes)
		: this(Capture(openScopes)) { }

	CodeWriterScopeValidationException(ImmutableArray<CodeWriterOpenScope> openScopes)
		: base(CreateMessage(openScopes))
	{
		_openScopes = openScopes;
	}

	/// <summary>
	/// Gets the number of scopes that had not been disposed when source creation was attempted.
	/// </summary>
	public int OpenScopeCount => _openScopes.Length;

	/// <summary>
	/// Gets contextual information for each scope that had not been disposed.
	/// </summary>
	public IReadOnlyList<CodeWriterOpenScope> OpenScopes => _openScopes;

	static ImmutableArray<CodeWriterOpenScope> Capture(IEnumerable<CodeWriterOpenScope> openScopes) =>
		openScopes is null ? throw new ArgumentNullException(nameof(openScopes)) : [.. openScopes];

	static string CreateMessage(ImmutableArray<CodeWriterOpenScope> openScopes)
	{
		var builder = new System.Text.StringBuilder()
			.Append("Cannot create generated source while ")
			.Append(openScopes.Length)
			.AppendLine(" disposable scope(s) remain open. Dispose every scope before calling ToString().");

		for (var index = 0; index < openScopes.Length; index++)
		{
			var scope = openScopes[index];
			builder.AppendLine().Append("Open scope #").Append(index + 1).Append(": ").Append(scope.Kind);

			if (!string.IsNullOrWhiteSpace(scope.Header))
				builder.Append(" — ").Append(scope.Header);

			builder.AppendLine().AppendLine(scope.OpeningStackTrace);
		}

		return builder.ToString().TrimEnd();
	}
}
