using System.ComponentModel;

namespace Purview.SourceGeneratorFramework;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class CodeWriterExtensions
{
	extension(CodeWriter)
	{
		public static CodeWriter CreateTestWriter(
			string? generatorName = null,
			string? version = null,
			bool includeGeneratedAttributes = false
		)
		{
			return new CodeWriter(generatorName ?? "TestGenerator", version ?? "1")
			{
				DefaultIncludeGeneratedAttributes = includeGeneratedAttributes,
			};
		}
	}
}
