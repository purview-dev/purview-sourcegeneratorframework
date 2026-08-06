namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// No-op implementation of <see cref="ITestOutput"/>.
/// </summary>
public sealed class NullTestOutput : ITestOutput
{
	/// <summary>
	/// Singleton instance of <see cref="NullTestOutput"/>.
	/// </summary>
	public static readonly NullTestOutput Instance = new();

	NullTestOutput() { }

	/// <inheritdoc />
	public void WriteLine(string message) { }
}
