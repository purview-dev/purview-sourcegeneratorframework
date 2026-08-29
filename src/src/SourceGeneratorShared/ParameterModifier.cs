namespace Purview.SourceGeneratorFramework;

/// <summary>Identifies a generated parameter modifier.</summary>
public enum ParameterModifier
{
	/// <summary>No modifier.</summary>
	None,

	/// <summary>The <c>ref</c> modifier.</summary>
	Ref,

	/// <summary>The <c>out</c> modifier.</summary>
	Out,

	/// <summary>The <c>in</c> modifier.</summary>
	In,

	/// <summary>The <c>ref readonly</c> modifier.</summary>
	RefReadOnly,
}
