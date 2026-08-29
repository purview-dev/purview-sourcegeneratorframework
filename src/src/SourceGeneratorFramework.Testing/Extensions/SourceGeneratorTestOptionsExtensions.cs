namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Fluent extension methods that preserve the concrete options type for downstream derived records.
/// </summary>
public static class SourceGeneratorTestOptionsExtensions
{
	extension<TOptions>(TOptions options)
		where TOptions : SourceGeneratorTestOptions
	{
		/// <summary>
		/// Creates a copy of these options with
		/// <see cref="SourceGeneratorTestOptions.CompileToAssembly"/> set to <see langword="true"/>.
		/// </summary>
		/// <remarks>
		/// The concrete options type is preserved, so derived records such as
		/// <see cref="AnalyzerTestOptions"/> and <see cref="CodeFixTestOptions"/> can opt into compiling
		/// the output assembly without losing their derived properties.
		/// </remarks>
		public TOptions Compile() => options with { CompileToAssembly = true };
	}
}
