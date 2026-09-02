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
			actualCode = FlattenWhitespace(actualCode);
			expectedCode = FlattenWhitespace(expectedCode);
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
			actualCode = FlattenWhitespace(actualCode);
			expectedCode = FlattenWhitespace(expectedCode);
		}

		return
#if NETSTANDARD2_0
			actualCode.IndexOf(expectedCode, StringComparison.Ordinal) >= 0;
#else
		actualCode.Contains(expectedCode, StringComparison.Ordinal);
#endif
	}

	// netstandard2.0 lacks the StringComparison overloads for Replace/Contains.
	static string FlattenWhitespace(string value) =>
#if NETSTANDARD2_0
		value.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "");
#else
		value
			.Replace("\r", "", StringComparison.Ordinal)
			.Replace("\n", "", StringComparison.Ordinal)
			.Replace("\t", "", StringComparison.Ordinal)
			.Replace(" ", "", StringComparison.Ordinal);
#endif
}
