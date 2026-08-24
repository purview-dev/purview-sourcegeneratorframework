using System.Collections.Concurrent;
using System.Globalization;

namespace Purview.SourceGeneratorFramework.Logging;

/// <summary>Registers output sinks for isolated source-generator logging sessions.</summary>
public static class SourceGenLogging
{
	static readonly ConcurrentDictionary<string, Action<string, int>> Sinks = new();

	/// <summary>Registers a sink for a source-generator logging session.</summary>
	public static IDisposable RegisterSink(string sessionId, Action<string, SourceGenLogLevel> sink)
	{
		if (string.IsNullOrWhiteSpace(sessionId))
			throw new ArgumentException("Logging session ID cannot be null or whitespace.", nameof(sessionId));
		if (sink is null)
			throw new ArgumentNullException(nameof(sink));

		// Wrap the sink to convert the SourceGenLogLevel to an int for the internal dictionary.
		return RegisterSinkCore(sessionId, (message, level) => sink(message, (SourceGenLogLevel)level));
	}

	// Uses BCL-only argument types so the testing framework can explicitly register the same sink
	// with a framework copy embedded in a generator assembly.
	public static IDisposable RegisterSinkCore(string sessionId, Action<string, int> sink)
	{
		if (string.IsNullOrWhiteSpace(sessionId))
			throw new ArgumentException("Logging session ID cannot be null or whitespace.", nameof(sessionId));
		if (sink is null)
			throw new ArgumentNullException(nameof(sink));
		if (!Sinks.TryAdd(sessionId, sink))
			throw new InvalidOperationException($"A logging sink is already registered for session '{sessionId}'.");

		// Return a disposable that will unregister the sink when disposed.
		return new SinkRegistration(sessionId);
	}

	public static ISourceGenLogger? CreateLogger(string? sessionId) =>
		string.IsNullOrWhiteSpace(sessionId) || !Sinks.ContainsKey(sessionId!) ? null : new SourceGenLogger(sessionId!);

	internal static void Write(
		string sessionId,
		SourceGenLogLevel level,
		int indentation,
		string message,
		object[] args
	)
	{
		if (!Sinks.TryGetValue(sessionId, out var sink))
			return;

		if (args is { Length: > 0 })
			message = string.Format(CultureInfo.InvariantCulture, message, args);

		var prefix = indentation <= 0 ? string.Empty : new string(' ', indentation * 2);
		sink(prefix + message, (int)level);
	}

	sealed class SinkRegistration(string sessionId) : IDisposable
	{
		string? _sessionId = sessionId;

		public void Dispose()
		{
			var id = Interlocked.Exchange(ref _sessionId, null);
			if (id is not null)
				Sinks.TryRemove(id, out _);
		}
	}
}
