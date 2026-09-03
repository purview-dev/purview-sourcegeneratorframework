namespace Purview.SourceGeneratorFramework.Examples;

/// <summary>
/// Defines the severity of a log entry.
/// </summary>
public enum LogLevel
{
	/// <summary>
	/// Trace-level detail.
	/// </summary>
	Trace = 0,

	/// <summary>
	/// Debug-level detail.
	/// </summary>
	Debug = 1,

	/// <summary>
	/// Informational messages.
	/// </summary>
	Information = 2,

	/// <summary>
	/// Warnings.
	/// </summary>
	Warning = 3,

	/// <summary>
	/// Errors.
	/// </summary>
	Error = 4,

	/// <summary>
	/// Critical failures.
	/// </summary>
	Critical = 5,
}

/// <summary>
/// Marks a type or member as a candidate for log emission.
/// </summary>
/// <remarks>
/// The derived <see cref="DebugAttribute"/> demonstrates attribute inheritance: it shares every property
/// declared here while pinning <see cref="Level"/> to <see cref="LogLevel.Debug"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1813:Avoid unsealed attributes")]
public class LogAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LogAttribute"/> class.
	/// </summary>
	public LogAttribute() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="LogAttribute"/> class with a message.
	/// </summary>
	/// <param name="message">The log message template.</param>
	public LogAttribute(string message)
	{
		Message = message;
	}

	/// <summary>
	/// Gets or sets the log message template.
	/// </summary>
	public string? Message { get; private set; }

	/// <summary>
	/// Gets or sets the event identifier.
	/// </summary>
	public int EventId { get; init; }

	/// <summary>
	/// Gets or sets the log category name.
	/// </summary>
	public string? CategoryName { get; init; }

	/// <summary>
	/// Gets or sets the log level.
	/// </summary>
	public LogLevel Level { get; init; } = LogLevel.Information;
}

/// <summary>
/// A <see cref="LogAttribute"/> that defaults <see cref="LogAttribute.Level"/> to
/// <see cref="LogLevel.Debug"/> while inheriting all other properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class DebugAttribute : LogAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DebugAttribute"/> class.
	/// </summary>
	public DebugAttribute()
	{
		Level = LogLevel.Debug;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DebugAttribute"/> class with a message.
	/// </summary>
	/// <param name="message">The log message template.</param>
	public DebugAttribute(string message)
		: base(message)
	{
		Level = LogLevel.Debug;
	}
}
