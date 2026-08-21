using System.ComponentModel;
using TUnit.Assertions.Attributes;

namespace Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class GeneratedCodeAssertionsExtensions
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[GenerateAssertion(ExpectationMessage = "generated code should be equal to {expectedCode}")]
	public static bool GeneratesCode(this string generatedCode, string expectedCode, bool flattenWhitespace = true)
	{
		if (generatedCode is null && expectedCode is null)
			return true;

		var actualCode = generatedCode ?? "";
		expectedCode ??= "";

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

		return actualCode.Trim() == expectedCode.Trim();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[GenerateAssertion(ExpectationMessage = "generated code should contain {expectedCode}")]
	public static bool ContainsGeneratedCode(
		this string generatedCode,
		string expectedCode,
		bool flattenWhitespace = true
	)
	{
		if (generatedCode is null && expectedCode is null)
			return true;

		var actualCode = generatedCode ?? "";
		expectedCode ??= "";

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
