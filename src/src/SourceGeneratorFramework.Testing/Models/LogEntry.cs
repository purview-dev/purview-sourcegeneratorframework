using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.Testing.Models;

public readonly record struct LogEntry(OutputType Type, string Message);
