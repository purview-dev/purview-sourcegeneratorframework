namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Identifies how generated code is indented.
/// </summary>
public enum IndentationStyle
{
	/// <summary>
	/// Indents with tab characters. This is the default.
	/// </summary>
	Tabs,

	/// <summary>
	/// Indents with the configured number of space characters.
	/// </summary>
	Spaces,
}
