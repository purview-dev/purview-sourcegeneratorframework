using System.Globalization;

namespace Purview.SourceGeneratorFramework.Logging;

/// <summary>
/// Helper used by source generators to emit structured log output.
/// </summary>
/// <remarks>
/// Initializes a new generation logger.
/// </remarks>
/// <param name="logger">The destination for formatted log messages.</param>
public sealed class GenerationLogger(Action<string, OutputType> logger)
{
	readonly Action<string, OutputType> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	/// <summary>
	/// Logs an informational message.
	/// </summary>
	public void Info(string message) => _logger(message, OutputType.Info);

	/// <summary>
	/// Logs an informational message with the specified indentation.
	/// </summary>
	public void Info(string message, int indentation) => LogIndented(message, OutputType.Info, indentation);

	/// <summary>
	/// Logs a debug message.
	/// </summary>
	public void Debug(string message) => _logger(message, OutputType.Debug);

	/// <summary>
	/// Logs a debug message with the specified indentation.
	/// </summary>
	public void Debug(string message, int indentation) => LogIndented(message, OutputType.Debug, indentation);

	/// <summary>
	/// Logs a diagnostic message.
	/// </summary>
	public void Diagnostic(string message) => _logger(message, OutputType.Diagnostic);

	/// <summary>
	/// Logs a diagnostic message with the specified indentation.
	/// </summary>
	public void Diagnostic(string message, int indentation) => LogIndented(message, OutputType.Diagnostic, indentation);

	/// <summary>
	/// Logs a diagnostic message.
	/// </summary>
	public void Diagnostic(DiagnosticInfo diagnostic) => Diagnostic(diagnostic, 0);

	/// <summary>
	/// Logs a diagnostic message with the specified indentation.
	/// </summary>
	public void Diagnostic(DiagnosticInfo diagnostic, int indentation)
	{
		if (diagnostic is null)
			throw new ArgumentNullException(nameof(diagnostic));

		var d = diagnostic.ToDiagnostic();
		LogIndented($"{d.Id}: {d.GetMessage(CultureInfo.InvariantCulture)}", OutputType.Diagnostic, indentation);
	}

	/// <summary>
	/// Logs a warning message.
	/// </summary>
	public void Warning(string message) => _logger(message, OutputType.Warning);

	/// <summary>
	/// Logs a warning message with the specified indentation.
	/// </summary>
	public void Warning(string message, int indentation) => LogIndented(message, OutputType.Warning, indentation);

	/// <summary>
	/// Logs an error message.
	/// </summary>
	public void Error(string message) => _logger(message, OutputType.Error);

	/// <summary>
	/// Logs an error message with the specified indentation.
	/// </summary>
	public void Error(string message, int indentation) => LogIndented(message, OutputType.Error, indentation);

	/// <summary>
	/// Logs an exception as an error.
	/// </summary>
	public void Error(Exception ex, string? message = null, int indentation = 0)
	{
		if (ex is null)
			throw new ArgumentNullException(nameof(ex));

		message ??= "The following exception occurred:";
		message += $"\n\nMessage: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";

		LogIndented(message, OutputType.Error, indentation);
	}

	void LogIndented(string message, OutputType outputType, int indentation)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		_logger(indentation <= 0 ? message : string.Concat(new string('\t', indentation), message), outputType);
	}
}
