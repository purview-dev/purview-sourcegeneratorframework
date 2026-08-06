using System.Collections.Concurrent;

namespace Purview.SourceGeneratorFramework.Testing.Abstractions;

/// <summary>
/// Helper used by source generators to emit structured log output.
/// </summary>
public sealed class GenerationLogger(Action<string, OutputType> logger)
{
	static readonly ConcurrentDictionary<int, string> SpacingCache = new();

	/// <summary>
	/// Logs an informational message.
	/// </summary>
	public void Info(string message) => logger(message, OutputType.Info);

	/// <summary>
	/// Logs an informational message with the specified indentation.
	/// </summary>
	public void Info(string message, int spacing) => Info(GetSpacing(spacing, message));

	/// <summary>
	/// Logs a debug message.
	/// </summary>
	public void Debug(string message) => logger(message, OutputType.Debug);

	/// <summary>
	/// Logs a debug message with the specified indentation.
	/// </summary>
	public void Debug(string message, int spacing) => Debug(GetSpacing(spacing, message));

	/// <summary>
	/// Logs a diagnostic message.
	/// </summary>
	public void Diagnostic(string message) => logger(message, OutputType.Diagnostic);

	/// <summary>
	/// Logs a diagnostic message with the specified indentation.
	/// </summary>
	public void Diagnostic(string message, int spacing) => Diagnostic(GetSpacing(spacing, message));

	/// <summary>
	/// Logs a warning message.
	/// </summary>
	public void Warning(string message) => logger(message, OutputType.Warning);

	/// <summary>
	/// Logs a warning message with the specified indentation.
	/// </summary>
	public void Warning(string message, int spacing) => Warning(GetSpacing(spacing, message));

	/// <summary>
	/// Logs an error message.
	/// </summary>
	public void Error(string message) => logger(message, OutputType.Error);

	/// <summary>
	/// Logs an error message with the specified indentation.
	/// </summary>
	public void Error(string message, int spacing) => Error(GetSpacing(spacing, message));

	/// <summary>
	/// Logs an exception as an error.
	/// </summary>
	public void Error(Exception ex, string? message = null, int tabs = 0)
	{
		if (ex is null)
			throw new ArgumentNullException(nameof(ex));

		message ??= "The following exception occurred:";

		message += $"\n\nMessage: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";

		logger(GetSpacing(tabs, message), OutputType.Error);
	}

	static string GetSpacing(int tabs, string message) =>
		(tabs <= 0 ? string.Empty : SpacingCache.GetOrAdd(tabs, static t => new string(' ', t)))
		+ message;
}
