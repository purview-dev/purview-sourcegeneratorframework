namespace Purview.SourceGeneratorFramework.Logging;

/// <summary>
/// Defines a logger interface for source generators to emit structured log output.
/// <b>Note:</b> This is used for compatible test implementations of the source generator testing framework.
/// During normal operations there will be no logger available.
/// </summary>
public interface ISourceGenLogger
{
	/// <summary>
	/// Logs a message with the specified log level and indentation.
	/// </summary>
	/// <param name="level">The log level of the message.</param>
	/// <param name="indentation">The number of spaces to indent the message.</param>
	/// <param name="message">The message to log.</param>
	/// <param name="args">The arguments to format the message.</param>
	void Log(SourceGenLogLevel level, int indentation, string message, params object[] args);
}
