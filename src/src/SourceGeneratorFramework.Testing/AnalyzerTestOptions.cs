namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Options that configure a diagnostic analyzer test run.
/// </summary>
public record AnalyzerTestOptions : SourceGeneratorTestOptions;

/// <summary>
/// Options that configure a code fix test run.
/// </summary>
public record CodeFixTestOptions : AnalyzerTestOptions
{
	/// <summary>
	/// Gets the index of the registered code action to apply.
	/// </summary>
	public int CodeActionIndex { get; init; }

	/// <summary>
	/// Gets the equivalence key used to select a registered code action.
	/// </summary>
	/// <remarks>
	/// When specified, this takes precedence over <see cref="CodeActionIndex"/>.
	/// </remarks>
	public string? EquivalenceKey { get; init; }
}
