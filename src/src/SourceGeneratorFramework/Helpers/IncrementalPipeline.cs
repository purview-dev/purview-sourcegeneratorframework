using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.Helpers;

/// <summary>
/// Helpers for building common incremental source generator pipelines.
/// </summary>
public static class IncrementalPipeline
{
	public const string BuildProperty = "build_property.";

	/// <summary>
	/// Combines a state provider with another value and immediately projects the pair into a
	/// named result, avoiding deeply nested tuple structures as a pipeline expands.
	/// </summary>
	public static IncrementalValueProvider<TResult> CombineWith<TState, TValue, TResult>(
		this IncrementalValueProvider<TState> stateProvider,
		IncrementalValueProvider<TValue> valueProvider,
		Func<TState, TValue, CancellationToken, TResult> selector,
		string? trackingName = null
	)
	{
		if (selector is null)
			throw new ArgumentNullException(nameof(selector));

		var result = stateProvider
			.Combine(valueProvider)
			.Select((pair, cancellationToken) => selector(pair.Left, pair.Right, cancellationToken));

		return string.IsNullOrWhiteSpace(trackingName) ? result : result.WithTrackingName(trackingName!);
	}

	/// <summary>
	/// Combines every item from a values provider with a single value and immediately projects
	/// each pair, preserving independent per-item incrementality.
	/// </summary>
	public static IncrementalValuesProvider<TResult> CombineWith<TState, TValue, TResult>(
		this IncrementalValuesProvider<TState> stateProvider,
		IncrementalValueProvider<TValue> valueProvider,
		Func<TState, TValue, CancellationToken, TResult> selector,
		string? trackingName = null
	)
	{
		if (selector is null)
			throw new ArgumentNullException(nameof(selector));

		var result = stateProvider
			.Combine(valueProvider)
			.Select((pair, cancellationToken) => selector(pair.Left, pair.Right, cancellationToken));

		return string.IsNullOrWhiteSpace(trackingName) ? result : result.WithTrackingName(trackingName!);
	}

	/// <summary>
	/// Combines every item from a values provider with a single state value and immediately
	/// projects each pair, preserving independent per-item incrementality.
	/// </summary>
	public static IncrementalValuesProvider<TResult> CombineWith<TState, TValue, TResult>(
		this IncrementalValueProvider<TState> stateProvider,
		IncrementalValuesProvider<TValue> valuesProvider,
		Func<TState, TValue, CancellationToken, TResult> selector,
		string? trackingName = null
	)
	{
		return selector is null
			? throw new ArgumentNullException(nameof(selector))
			: valuesProvider.CombineWith(
				stateProvider,
				(value, state, cancellationToken) => selector(state, value, cancellationToken),
				trackingName
			);
	}

	/// <summary>
	/// Collects a values provider and immediately projects its immutable array together with an
	/// existing state, making additional pipeline inputs straightforward to add.
	/// </summary>
	public static IncrementalValueProvider<TResult> CollectWith<TState, TValue, TResult>(
		this IncrementalValueProvider<TState> stateProvider,
		IncrementalValuesProvider<TValue> valuesProvider,
		Func<TState, ImmutableArray<TValue>, CancellationToken, TResult> selector,
		string? trackingName = null
	)
	{
		return selector is null
			? throw new ArgumentNullException(nameof(selector))
			: stateProvider.CombineWith(valuesProvider.Collect(), selector, trackingName);
	}

	/// <summary>
	/// Collects all items from a values provider, combines the resulting immutable array with a
	/// single value, and projects both into one aggregate result.
	/// </summary>
	public static IncrementalValueProvider<TResult> CollectWith<TState, TValue, TResult>(
		this IncrementalValuesProvider<TState> stateProvider,
		IncrementalValueProvider<TValue> valueProvider,
		Func<ImmutableArray<TState>, TValue, CancellationToken, TResult> selector,
		string? trackingName = null
	)
	{
		return selector is null
			? throw new ArgumentNullException(nameof(selector))
			: stateProvider.Collect().CombineWith(valueProvider, selector, trackingName);
	}

	/// <summary>
	/// Collects all items from two values providers and projects both immutable arrays into one
	/// aggregate result. This is an aggregate operation rather than a Cartesian product.
	/// </summary>
	public static IncrementalValueProvider<TResult> CollectWith<TLeft, TRight, TResult>(
		this IncrementalValuesProvider<TLeft> leftProvider,
		IncrementalValuesProvider<TRight> rightProvider,
		Func<ImmutableArray<TLeft>, ImmutableArray<TRight>, CancellationToken, TResult> selector,
		string? trackingName = null
	)
	{
		return selector is null
			? throw new ArgumentNullException(nameof(selector))
			: leftProvider.Collect().CombineWith(rightProvider.Collect(), selector, trackingName);
	}

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
		if (!propertyName.StartsWith(BuildProperty, StringComparison.Ordinal))
			msbuildPropertyValue = BuildProperty + propertyName;

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
	public static IncrementalValueProvider<TContext> GenerationContextValueProvider<TContext>(
		IncrementalGeneratorInitializationContext context,
		string generatorName,
		string generatorVersion,
		Func<Compilation, GenerationSettings, ISourceGenLogger?, CancellationToken, TContext> factory,
		string? disablePropertyName = null
	)
		where TContext : notnull, GenerationContext
	{
		if (factory is null)
			throw new ArgumentNullException(nameof(factory));

		GenerationSettings baseSettings = new(generatorName, generatorVersion);

		return context
			.CompilationProvider.Combine(GenerationConfigurationValueProvider(context, disablePropertyName))
			.Select(
				(input, cancellationToken) =>
				{
					cancellationToken.ThrowIfCancellationRequested();

					var configuration = input.Right;
					var logger = configuration.IsLoggingEnabled
						? SourceGenLogging.CreateLogger(configuration.LoggingSessionId)
						: null;

					logger?.Info(
						$"Creating generation context ({typeof(TContext)}) for compilation '{input.Left.AssemblyName}'."
					);

					return factory(
						input.Left,
						baseSettings with
						{
							ValidateCodeWriterScopes = configuration.ValidateCodeWriterScopes,
							IsSourceGeneratorDisabled = configuration.IsSourceGeneratorDisabled,
							IsLoggingEnabled = logger is not null,
						},
						logger,
						cancellationToken
					);
				}
			)
			.WithTrackingName($"GetGenerationContext_{typeof(TContext).Name}");
	}

	/// <summary>
	/// Creates a value provider that builds a default generation context from the compilation and
	/// resolved framework configuration.
	/// </summary>
	public static IncrementalValueProvider<GenerationContext> DefaultGenerationContextValueProvider(
		IncrementalGeneratorInitializationContext context,
		string generatorName,
		string generatorVersion,
		string? disablePropertyName = null
	)
	{
		return GenerationContextValueProvider(
			context,
			generatorName,
			generatorVersion,
			static (compilation, settings, logger, _) => new GenerationContext(compilation, settings, logger),
			disablePropertyName
		);
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
						BuildProperty + GenerationContext.ValidateCodeWriterScopesBuildProperty,
						out var scopeValidationValue
					);
					options.GlobalOptions.TryGetValue(
						BuildProperty + GenerationContext.EnableLoggingBuildProperty,
						out var loggingEnabledValue
					);
					options.GlobalOptions.TryGetValue(
						BuildProperty + GenerationContext.LoggingSessionIdBuildProperty,
						out var loggingSessionId
					);

					string? disabledValue = null;
					if (!string.IsNullOrWhiteSpace(disablePropertyName))
					{
						var propertyName = disablePropertyName!.StartsWith(BuildProperty, StringComparison.Ordinal)
							? disablePropertyName
							: BuildProperty + disablePropertyName;
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

	readonly record struct GenerationConfiguration(
		bool ValidateCodeWriterScopes,
		bool IsSourceGeneratorDisabled,
		bool IsLoggingEnabled,
		string? LoggingSessionId
	);

	/// <summary>
	/// Creates a values provider for syntax nodes annotated with a specific attribute.
	/// </summary>
	public static IncrementalValuesProvider<TOutput> ForAttributeWithMetadataName<TOutput>(
		IncrementalGeneratorInitializationContext context,
		TypeValueObject attributeType,
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

	/// <summary>
	/// Combines a values provider with a single value provider, returning a values provider of tuples.
	/// </summary>
	public static IncrementalValuesProvider<(TOutput Output, TContext Context)> CombineWithContext<TOutput, TContext>(
		this IncrementalValuesProvider<TOutput> valuesProvider,
		IncrementalValueProvider<TContext> contextProvider
	)
		where TOutput : notnull
	{
		return valuesProvider.CombineWith(
			contextProvider,
			static (output, generationContext, _) => (output, generationContext)
		);
	}

	/// <summary>
	/// Registers a source output that combines each <see cref="GeneratorResult{T}"/> with a
	/// generation context, reports any diagnostics, and invokes the generator callback only for
	/// successful results. This keeps generator <see cref="IIncrementalGenerator.Initialize"/> methods thin
	/// and enforces the rule that diagnostics are reported in the source output stage.
	/// </summary>
	public static void RegisterSourceOutput<TOutput, TContext>(
		this IncrementalGeneratorInitializationContext context,
		IncrementalValuesProvider<GeneratorResult<TOutput>> outputs,
		IncrementalValueProvider<TContext> contextProvider,
		Action<SourceProductionContext, TOutput, TContext> generate,
		string? trackingName = null
	)
		where TOutput : notnull
		where TContext : GenerationContext
	{
		if (generate is null)
			throw new ArgumentNullException(nameof(generate));

		var combined = outputs
			.CombineWith(contextProvider, static (output, ctx, _) => (Output: output, Context: ctx))
			.WithTrackingName(trackingName ?? $"RegisterSourceOutput_{typeof(TOutput).Name}");

		context.RegisterSourceOutput(
			combined,
			(spc, item) =>
			{
				if (item.Output.HasDiagnostics)
					spc.ReportDiagnostics(item.Output.Diagnostics);

				if (!item.Output.IsSuccess || item.Output.IsFatal)
					return;

				generate(spc, item.Output.Value!, item.Context);
			}
		);
	}
}
