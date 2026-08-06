namespace Purview.SourceGeneratorFramework.Testing.Abstractions;

/// <summary>
/// Defines the severity of a log message emitted by a source generator.
/// </summary>
public enum OutputType
{
	/// <summary>
	/// Diagnostic message.
	/// </summary>
	Diagnostic,

	/// <summary>
	/// Debug message.
	/// </summary>
	Debug,

	/// <summary>
	/// Informational message.
	/// </summary>
	Info,

	/// <summary>
	/// Warning message.
	/// </summary>
	Warning,

	/// <summary>
	/// Error message.
	/// </summary>
	Error,
}
