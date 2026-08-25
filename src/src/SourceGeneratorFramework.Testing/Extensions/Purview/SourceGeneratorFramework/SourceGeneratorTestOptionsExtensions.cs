using System.Collections.Immutable;
using System.ComponentModel;

namespace Purview.SourceGeneratorFramework;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class SourceGeneratorTestOptionsExtensions
{
	extension<TOptions>(TOptions options)
		where TOptions : SourceGeneratorTestOptions
	{
		/// <summary>
		/// Creates a new options snapshot with the specified analyzer-config options added to the existing set.
		/// </summary>
		/// <param name="configOptions">The analyzer-config options to add.</param>
		/// <returns>A new <see cref="SourceGeneratorTestOptions"/> instance with the specified options added.</returns>
		public TOptions WithAnalyzerConfigOptions(params (string, string)[] configOptions)
		{
			if (configOptions is null || configOptions.Length == 0)
				return options;

			var analyzerConfigOptions = options.AnalyzerConfigOptions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			foreach (var (key, value) in configOptions)
			{
				analyzerConfigOptions[key] = value;
			}

			return options with
			{
				AnalyzerConfigOptions = analyzerConfigOptions.ToImmutableDictionary(),
			};
		}
	}
}
