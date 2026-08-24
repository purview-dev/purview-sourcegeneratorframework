using System.ComponentModel;
using TUnit.Assertions.Attributes;

namespace Purview.SourceGeneratorFramework.Infra.Assertions;

[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class CodeWriterAssertionsExtensions
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[GenerateAssertion(ExpectationMessage = "generated code should be equal to {expectedCode}")]
	public static bool Generates(this CodeWriter writer, string expectedCode, bool flattenWhitespace = true)
	{
		var actualCode = writer.ToString();
		if (flattenWhitespace)
		{
			actualCode = actualCode
				.ReplaceLineEndings("")
				.Replace("\t", "", StringComparison.Ordinal)
				.Replace(" ", "", StringComparison.Ordinal);
			expectedCode = expectedCode
				.ReplaceLineEndings("")
				.Replace("\t", "", StringComparison.Ordinal)
				.Replace(" ", "", StringComparison.Ordinal);
		}

		return actualCode?.Trim() == expectedCode?.Trim();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[GenerateAssertion(ExpectationMessage = "generated code should contain {expectedCode}")]
	public static bool ContainsGenerated(this CodeWriter writer, string expectedCode, bool flattenWhitespace = true)
	{
		var actualCode = writer.ToString().Trim();
		expectedCode = expectedCode?.Trim() ?? "";
		if (flattenWhitespace)
		{
			actualCode = actualCode
				.ReplaceLineEndings("")
				.Replace("\t", "", StringComparison.Ordinal)
				.Replace(" ", "", StringComparison.Ordinal);
			expectedCode = expectedCode
				.ReplaceLineEndings("")
				.Replace("\t", "", StringComparison.Ordinal)
				.Replace(" ", "", StringComparison.Ordinal);
		}

		return actualCode.Contains(expectedCode, StringComparison.Ordinal);
	}
}
