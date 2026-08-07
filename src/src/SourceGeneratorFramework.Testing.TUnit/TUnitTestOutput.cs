namespace Purview.SourceGeneratorFramework.Testing.TUnit;

/// <summary>
/// Routes generator log output to the current TUnit test context.
/// </summary>
sealed class TUnitTestOutput : ITestOutput
{
	/// <inheritdoc />
	public void WriteLine(string message)
	{
		TestContext.Current?.OutputWriter.WriteLine(message);
	}
}
