using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.Helpers;

/// <summary>
/// Helpers for building common incremental source generator pipelines.
/// </summary>
public static class IncrementalPipeline
{
	/// <summary>
	/// Creates a value provider that reads an MSBuild property to determine whether the generator is disabled.
	/// </summary>
	public static IncrementalValueProvider<bool> IsDisabledValueProvider(
		IncrementalGeneratorInitializationContext context,
		string propertyName
	) => PropertyValueProvider(context, propertyName, value => bool.TryParse(value, out var isDisabled) && isDisabled);

	/// <summary>
	/// Creates a value provider that reads an MSBuild property.
	/// </summary>
	public static IncrementalValueProvider<T> PropertyValueProvider<T>(
		IncrementalGeneratorInitializationContext context,
		string propertyName,
		Func<string?, T> converter
	)
	{
		if (string.IsNullOrWhiteSpace(propertyName))
			throw new ArgumentException("Property name cannot be null or whitespace.", nameof(propertyName));
		if (converter == null)
			throw new ArgumentNullException(nameof(converter));

		var msbuildPropertyValue = propertyName;
		if (!propertyName.StartsWith(SourceGeneratorBuildProperties.BuildProperty, StringComparison.Ordinal))
			msbuildPropertyValue = SourceGeneratorBuildProperties.BuildProperty + propertyName;

		// All valid...
		return context
			.AnalyzerConfigOptionsProvider.Select(
				(options, _) =>
				{
					options.GlobalOptions.TryGetValue(msbuildPropertyValue, out var value);

					return converter(value);
				}
			)
			.WithTrackingName($"GetMSBuildPropertyValue_{propertyName}");
	}

	/// <summary>
	/// Creates a value provider that builds a generation context from the compilation, framework
	/// build properties, an optional generator-disable property, and any registered logging sink.
	/// </summary>
	/// <param name="context">The generator initialization context.</param>
	/// <param name="settings">The generation settings.</param>
	/// <param name="factory">A factory that creates a generation context from the compilation, settings, and optional logger.</param>
	public static IncrementalValueProvider<
		GenerationContext<TCapabilities>
	> GenerationContextValueProvider<TCapabilities>(
		IncrementalGeneratorInitializationContext context,
		GenerationSettings settings,
		Func<Compilation, GenerationSettings, ISourceGenLogger?, CancellationToken, TCapabilities> factory
	)
		where TCapabilities : class, IGenerationCapabilities
	{
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));
		if (factory is null)
			throw new ArgumentNullException(nameof(factory));

		// Combine the compilation and generation configuration into a single value provider, then transform it into a generation context.
		return context
			.CompilationProvider.Combine(
				GenerationConfigurationValueProvider(context, settings.DisabledSourceGenMSBuildProperty)
			)
			.Select(
				(input, cancellationToken) =>
				{
					cancellationToken.ThrowIfCancellationRequested();

					var compilation = input.Left;
					var configuration = input.Right;
					var logger = configuration.IsLoggingEnabled
						? SourceGenLogging.CreateLogger(configuration.LoggingSessionId)
						: null;

					logger?.Info(
						$"Creating generation context ({typeof(TCapabilities)}) for compilation '{input.Left.AssemblyName}'."
					);

					settings = settings with
					{
						ValidateCodeWriterScopes = configuration.ValidateCodeWriterScopes,
						IsSourceGeneratorDisabled = configuration.IsSourceGeneratorDisabled,
						IsLoggingEnabled = logger is not null,
					};

					var capabilities = factory(compilation, settings, logger, cancellationToken);

					return new GenerationContext<TCapabilities>(capabilities, settings, logger);
				}
			)
			.WithTrackingName($"GetGenerationContext_{typeof(TCapabilities).Name}");
	}

	static IncrementalValueProvider<GenerationConfiguration> GenerationConfigurationValueProvider(
		IncrementalGeneratorInitializationContext context,
		string? disablePropertyName
	) =>
		context
			.AnalyzerConfigOptionsProvider.Select(
				(options, _) =>
				{
					options.GlobalOptions.TryGetValue(
						SourceGeneratorBuildProperties.ValidateCodeWriterScopes,
						out var scopeValidationValue
					);
					options.GlobalOptions.TryGetValue(
						SourceGeneratorBuildProperties.EnableLogging,
						out var loggingEnabledValue
					);
					options.GlobalOptions.TryGetValue(
						SourceGeneratorBuildProperties.LoggingSessionId,
						out var loggingSessionId
					);

					string? disabledValue = null;
					if (!string.IsNullOrWhiteSpace(disablePropertyName))
					{
						var propertyName = disablePropertyName!.StartsWith(
							SourceGeneratorBuildProperties.BuildProperty,
							StringComparison.Ordinal
						)
							? disablePropertyName
							: SourceGeneratorBuildProperties.BuildProperty + disablePropertyName;
						options.GlobalOptions.TryGetValue(propertyName, out disabledValue);
					}

					return new GenerationConfiguration(
						ValidateCodeWriterScopes: bool.TryParse(scopeValidationValue, out var validateScopes)
							&& validateScopes,
						IsSourceGeneratorDisabled: bool.TryParse(disabledValue, out var isDisabled) && isDisabled,
						IsLoggingEnabled: bool.TryParse(loggingEnabledValue, out var loggingEnabled) && loggingEnabled,
						LoggingSessionId: loggingSessionId
					);
				}
			)
			.WithTrackingName("GetGenerationConfiguration");

	/// <summary>
	/// Creates a values provider for syntax nodes annotated with a specific attribute.
	/// </summary>
	public static IncrementalValuesProvider<TOutput> ForAttributeWithMetadataName<TOutput>(
		IncrementalGeneratorInitializationContext context,
		TypeIdentity attributeType,
		Func<GeneratorAttributeSyntaxContext, CancellationToken, TOutput> transform,
		Func<SyntaxNode, CancellationToken, bool>? predicate = null,
		string? trackingName = null
	)
		where TOutput : notnull
	{
		if (transform is null)
			throw new ArgumentNullException(nameof(transform));

		predicate ??= static (_, _) => true;

		return context
			.SyntaxProvider.ForAttributeWithMetadataName(attributeType.MetadataFullName, predicate, transform)
			.WithTrackingName(trackingName ?? $"ForAttribute_{attributeType.Name}");
	}

	readonly record struct GenerationConfiguration(
		bool ValidateCodeWriterScopes,
		bool IsSourceGeneratorDisabled,
		bool IsLoggingEnabled,
		string? LoggingSessionId
	);
}
