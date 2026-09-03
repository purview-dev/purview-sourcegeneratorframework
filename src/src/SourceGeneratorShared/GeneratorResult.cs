using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Represents the result of an incremental source generator transform, carrying either a value, diagnostics, or both.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public readonly record struct GeneratorResult<T>
{
	/// <summary>
	/// Gets the value of the generator result. If the result is a failure, this will be null or default(T).
	/// </summary>
	/// <remarks>
	/// This value can be null or default(T) if the result is a failure.
	/// </remarks>
	public T Value { get; private init; }

	/// <summary>
	/// Gets the diagnostics associated with the generator result. If the result is successful, this may be empty or contain warnings.
	/// </summary>
	public EquatableArray<DiagnosticInfo> Diagnostics { get; private init; }

	/// <summary>
	/// Indicates whether the generator result is successful (has a value) and does not contain any fatal diagnostics.
	/// </summary>
	public bool HasValue { get; private init; }

	/// <summary>
	/// Indicates whether the generator result contains any diagnostics, regardless of success or failure.
	/// </summary>
	public bool HasDiagnostics { get; private init; }

	/// <summary>
	/// Indicates whether the generator result should be processed, meaning it has a value and does not contain any error severity diagnostics.
	/// </summary>
	public bool ShouldProcess { get; private init; }

	/// <summary>
	/// Indicates whether the generator result contains any diagnostics with severity of Error.
	/// </summary>
	/// <remarks>We use the DefaultSeverity of the diagnostic descriptor, rather than the effective one
	/// because regardless of if the consumer has changed its level, the source generator is effectively saying
	/// it's serious and cannot continue.</remarks>
	public bool HasErrorDiagnostics { get; private init; }

	/// <summary>
	///	Indicates whether the generator result is empty, meaning it has no value and no diagnostics.
	/// </summary>
	public bool IsEmpty => this == Empty;

	/// <summary>
	/// Creates a successful generator result with the specified value and diagnostics.
	/// </summary>
	/// <param name="value">The value of the generator result.</param>
	/// <param name="diagnostics">The diagnostics associated with the result.</param>
	/// <returns>A successful generator result.</returns>
	public static GeneratorResult<T> Create(T value, ImmutableArray<DiagnosticInfo> diagnostics)
	{
		var hasValue = value is not null && !EqualityComparer<T>.Default.Equals(value, default!);
		var hasDiagnostics = !diagnostics.IsDefaultOrEmpty;
		var hasErrorDiagnostics = diagnostics.Any(d =>
			d.Descriptor.DefaultSeverity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error
		);

		return new()
		{
			Value = value!,
			Diagnostics = diagnostics,
			HasValue = hasValue,
			HasDiagnostics = hasDiagnostics,
			ShouldProcess = hasValue && !hasErrorDiagnostics,
			HasErrorDiagnostics = hasErrorDiagnostics,
		};
	}

	/// <summary>
	/// Creates a successful generator result with the specified value and optional diagnostics.
	/// </summary>
	/// <param name="value">The value of the generator result.</param>
	/// <param name="diagnostics">Optional diagnostics associated with the result.</param>
	/// <returns>A successful generator result.</returns>
	public static GeneratorResult<T> Create(T value, params DiagnosticInfo[] diagnostics) =>
		Create(
			value,
			diagnostics is null || diagnostics.Length == 0
				? EquatableArray<DiagnosticInfo>.Empty
				: EquatableArray<DiagnosticInfo>.Create(diagnostics)
		);

	/// <summary>
	/// Creates a failed generator result with the specified diagnostics. At least one diagnostic must be provided.
	/// </summary>
	/// <param name="diagnostics">The diagnostics associated with the failure result.</param>
	/// <returns>A failed generator result.</returns>
	/// <exception cref="ArgumentException">Thrown when no diagnostics are provided.</exception>
	public static GeneratorResult<T> Create(params DiagnosticInfo[] diagnostics)
	{
		if (diagnostics is null || diagnostics.Length == 0)
		{
			throw new ArgumentException(
				"At least one diagnostic must be provided for a failure result.",
				nameof(diagnostics)
			);
		}

		var hasErrorDiagnostics = diagnostics.Any(d =>
			d.Descriptor.DefaultSeverity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error
		);

		return new()
		{
			Value = default!,
			Diagnostics = EquatableArray<DiagnosticInfo>.Create(diagnostics),
			HasValue = false,
			HasDiagnostics = true,
			ShouldProcess = false,
			HasErrorDiagnostics = hasErrorDiagnostics,
		};
	}

	/// <summary>
	/// Implicitly converts a value of type T to a successful GeneratorResult{T} with that value and no diagnostics.
	/// </summary>
	/// <param name="value">The value to convert to a successful GeneratorResult{T}.</param>
	public static implicit operator GeneratorResult<T>(T value) => Create(value);

	/// <summary>
	/// Represents an empty generator result with no value and no diagnostics.
	/// </summary>
	public static readonly GeneratorResult<T> Empty;
}
