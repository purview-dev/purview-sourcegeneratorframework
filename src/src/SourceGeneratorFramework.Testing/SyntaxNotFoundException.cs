namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Raised by <c>CodeQuery</c> when a requested syntax node or tree cannot be located.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
public sealed class SyntaxNotFoundException : InvalidOperationException
{
	/// <summary>Initializes a new instance of the <see cref="SyntaxNotFoundException"/> class.</summary>
	public SyntaxNotFoundException(string message)
		: base(message) { }

	/// <summary>Initializes a new instance of the <see cref="SyntaxNotFoundException"/> class.</summary>
	public SyntaxNotFoundException(string message, Exception innerException)
		: base(message, innerException) { }
}
