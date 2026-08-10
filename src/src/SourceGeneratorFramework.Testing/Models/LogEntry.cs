using Purview.SourceGeneratorFramework.Testing.Abstractions;

namespace Purview.SourceGeneratorFramework.Testing.Models;

public readonly record struct LogEntry(OutputType Type, string Message);
