using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.Generators.Model;

sealed class SourceGenLogger(Action<string, SourceGenLogLevel> logger) : ISourceGenLogger
{
	readonly Action<string, SourceGenLogLevel> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (args is not null && args.Length > 0)
			message = string.Format(System.Globalization.CultureInfo.InvariantCulture, message, args);

		// We're using 2 spaces per indentation level.
		_logger(indentation <= 0 ? message : string.Concat(new string(' ', indentation * 2), message), level);
	}
}
