using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Models;

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
	)
	{
		if (string.IsNullOrWhiteSpace(propertyName))
			throw new ArgumentException(
				"Property name cannot be null or whitespace.",
				nameof(propertyName)
			);

		// All valid...
		return context
			.AnalyzerConfigOptionsProvider.Select(
				(options, _) =>
				{
					options.GlobalOptions.TryGetValue(propertyName, out var value);
					return bool.TryParse(value, out var isDisabled) && isDisabled;
				}
			)
			.WithTrackingName($"IsDisabled_{propertyName}");
	}

	/// <summary>
	/// Creates a value provider that builds a generation context from the compilation.
	/// </summary>
	public static IncrementalValueProvider<TContext> GenerationContextValueProvider<TContext>(
		IncrementalGeneratorInitializationContext context,
		Func<Compilation, CancellationToken, TContext> factory
	)
	{
		if (factory is null)
			throw new ArgumentNullException(nameof(factory));

		// All valid...
		return context
			.CompilationProvider.Select(
				(compilation, cancellationToken) => factory(compilation, cancellationToken)
			)
			.WithTrackingName($"GetGenerationContext_{typeof(TContext).Name}");
	}

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
			.SyntaxProvider.ForAttributeWithMetadataName(
				attributeType.SymbolFullName,
				predicate,
				transform
			)
			.WithTrackingName(trackingName ?? $"ForAttribute_{attributeType.TypeName}");
	}

	/// <summary>
	/// Combines a values provider with a single value provider, returning a values provider of tuples.
	/// </summary>
	public static IncrementalValuesProvider<(TOutput Output, TContext Context)> CombineWithContext<
		TOutput,
		TContext
	>(
		this IncrementalValuesProvider<TOutput> valuesProvider,
		IncrementalValueProvider<TContext> contextProvider
	)
		where TOutput : notnull
	{
		return valuesProvider
			.Combine(contextProvider)
			.Select(static (combined, _) => (combined.Left, combined.Right));
	}
}
