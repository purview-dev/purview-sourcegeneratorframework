namespace Purview.SourceGeneratorFramework.Logging;

/// <summary>
/// Defines the severity of a log message emitted by a source generator.
/// </summary>
public enum SourceGenLogLevel
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
	/// Fatal message.
	/// </summary>
	Fatal,
}
