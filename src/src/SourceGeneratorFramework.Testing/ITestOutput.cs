namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Receives log output from a source generator test run.
/// </summary>
public interface ITestOutput
{
	/// <summary>
	/// Writes a line of output.
	/// </summary>
	/// <param name="message">The message to write.</param>
	void WriteLine(string message);
}
