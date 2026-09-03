using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Describes immutable settings shared by a source-generation operation.
/// </summary>
public sealed record GenerationSettings
{
	/// <summary>
	/// Initializes source-generation settings.
	/// </summary>
	/// <param name="generatorName">The name of the generator.</param>
	/// <param name="generatorVersion">The version of the generator. If null, defaults to "1.0.0.0".</param>
	/// <param name="disabledSourceGenMSBuildProperty">An optional MSBuild property name that disables the generator when set to true.</param>
	public GenerationSettings(
		string generatorName,
		string? generatorVersion = null,
		string? disabledSourceGenMSBuildProperty = null
	)
	{
		if (string.IsNullOrWhiteSpace(generatorName))
			throw new ArgumentException("Generator name cannot be null or whitespace.", nameof(generatorName));

		GeneratorName = generatorName;
		GeneratorVersion = generatorVersion ?? "1.0.0.0";
		DisabledSourceGenMSBuildProperty = disabledSourceGenMSBuildProperty;
	}

	/// <summary>
	/// Gets the source generator name propagated to code writers.
	/// </summary>
	public string GeneratorName { get; }

	/// <summary>
	/// Gets the source generator version propagated to code writers.
	/// </summary>
	public string GeneratorVersion { get; }

	/// <summary>
	/// Gets the optional MSBuild property name that disables the generator when set to true.
	/// </summary>
	public string? DisabledSourceGenMSBuildProperty { get; }

	/// <summary>
	/// Gets how nullable annotations and the <c>#nullable enable</c> directive are emitted by generated
	/// code written via <c>CodeWriter</c>. The default is <see cref="NullableDirectiveMode.Auto"/>,
	/// which uses <see cref="IsNullableContextEnabled"/> when it is known.
	/// </summary>
	public NullableDirectiveMode NullableDirectiveMode { get; init; } = NullableDirectiveMode.Auto;

	/// <summary>
	/// Gets whether the target compilation has nullable annotations enabled. The incremental pipeline
	/// sets this from the compilation when it is available and no explicit value was configured;
	/// <see langword="null"/> when the value is unknown, such as for post-initialization outputs or
	/// tests that construct settings directly.
	/// </summary>
	public bool? IsNullableContextEnabled { get; init; }

	/// <summary>
	/// Gets whether created code writers validate undisposed scopes.
	/// </summary>
	public bool ValidateCodeWriterScopes { get; init; }

	/// <summary>
	/// Gets whether the source generator is disabled by build configuration.
	/// </summary>
	public bool IsSourceGeneratorDisabled { get; init; }

	/// <summary>
	/// Gets whether source-generator logging is active for this generation context.
	/// </summary>
	public bool IsLoggingEnabled { get; init; }

	/// <summary>
	/// Gets how generated code is indented. The default is <see cref="IndentationStyle.Tabs"/>.
	/// </summary>
	public IndentationStyle IndentationStyle { get; init; } = IndentationStyle.Tabs;

	/// <summary>
	/// Gets the indentation width: the number of spaces used per level when
	/// <see cref="IndentationStyle"/> is <see cref="IndentationStyle.Spaces"/>, or the display width of a
	/// single tab when it is <see cref="IndentationStyle.Tabs"/>. The default is 4.
	/// </summary>
	public int IndentationSize { get; init; } = 4;

	/// <summary>
	/// Gets the maximum line length that drives inline-versus-multiline wrapping heuristics. The default is 100.
	/// </summary>
	public int MaximumLineLength { get; init; } = 100;

	/// <summary>
	/// Gets the C# language version of the target compilation, when it is known. Generators can use this to
	/// gate emitted features such as primary constructors or collection expressions. It is seeded from the
	/// consuming project's <c>LangVersion</c> when available.
	/// </summary>
	public LanguageVersion? LanguageVersion { get; init; }

	/// <summary>
	/// Creates a new generation settings instance for the specified generator type, using the type name and assembly version.
	/// </summary>
	/// <typeparam name="TGenerator">The type of the generator.</typeparam>
	/// <param name="disabledSourceGenMSBuildProperty">An optional MSBuild property name that disables the generator when set to true.</param>
	/// <returns>A new <see cref="GenerationSettings"/> instance.</returns>
	public static GenerationSettings Create<TGenerator>(string? disabledSourceGenMSBuildProperty = null)
	{
		var generatorType = typeof(TGenerator);

		return new(
			generatorType.Name,
			generatorType.Assembly.GetName().Version?.ToString(),
			disabledSourceGenMSBuildProperty
		);
	}
}
