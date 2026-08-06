namespace Purview.SourceGeneratorFramework.Testing.Abstractions;

/// <summary>
/// Allows a source generator to receive log output from a test runner.
/// </summary>
public interface ILogSupport
{
	/// <summary>
	/// Sets the action used to receive log messages and their severity.
	/// </summary>
	/// <param name="action">The log output action.</param>
	void SetLogOutput(Action<string, OutputType> action);
}
