namespace Purview.SourceGeneratorFramework.Logging;

sealed class SourceGenLogger(string sessionId) : ISourceGenLogger
{
	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		SourceGenLogging.Write(sessionId, level, indentation, message, args);
	}
}
