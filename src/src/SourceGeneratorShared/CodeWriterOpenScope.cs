namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Describes a disposable <see cref="CodeWriter"/> scope that was still open when source was
/// materialized.
/// </summary>
/// <param name="Kind">The kind of scope.</param>
/// <param name="Header">The block header, when available.</param>
/// <param name="OpeningStackTrace">The call stack captured when the scope was opened.</param>
public sealed record CodeWriterOpenScope(string Kind, string? Header, string OpeningStackTrace);
