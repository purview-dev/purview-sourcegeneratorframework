namespace Purview.SourceGeneratorFramework.Logging;

/// <summary>
/// Allows a source generator to report log information, only useful as part of a test run.
/// </summary>
public interface ISupportsSourceGenLogging
{
	/// <summary>
	/// Sets the action used to receive log messages and their severity.
	/// </summary>
	/// <param name="action">The log output action.</param>
	void SetOutput(Action<string, SourceGenLogLevel> action);
}
