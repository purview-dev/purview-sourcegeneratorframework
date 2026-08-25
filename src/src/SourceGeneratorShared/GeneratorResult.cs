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
	public T? Value { get; private init; }

	/// <summary>
	/// Gets the diagnostics associated with the generator result. If the result is successful, this may be empty or contain warnings.
	/// </summary>
	public EquatableArray<DiagnosticInfo> Diagnostics { get; private init; }

	/// <summary>
	/// Indicates whether the generator result is successful (has a value) and does not contain any fatal diagnostics.
	/// </summary>
	public bool IsSuccess => Value is not null && !EqualityComparer<T>.Default.Equals(Value, default!);

	/// <summary>
	/// Indicates whether the generator result contains any diagnostics, regardless of success or failure.
	/// </summary>
	public bool HasDiagnostics => !Diagnostics.IsEmpty;

	/// <summary>
	/// Indicates whether the generator result is a failure and contains at least one fatal diagnostic.
	/// </summary>
	public bool IsFatal => !IsSuccess && HasDiagnostics;

	/// <summary>
	///	Indicates whether the generator result is empty, meaning it has no value and no diagnostics.
	/// </summary>
	public bool IsEmpty => !IsSuccess && !HasDiagnostics;

	/// <summary>
	/// Creates a successful generator result with the specified value and optional diagnostics.
	/// </summary>
	/// <param name="value">The value of the generator result.</param>
	/// <param name="diagnostics">Optional diagnostics associated with the result.</param>
	/// <returns>A successful generator result.</returns>
	public static GeneratorResult<T> Ok(T value, params DiagnosticInfo[] diagnostics)
	{
		return new GeneratorResult<T>
		{
			Value = value,
			Diagnostics =
				diagnostics is null || diagnostics.Length == 0
					? EquatableArray<DiagnosticInfo>.Empty
					: EquatableArray<DiagnosticInfo>.Create(diagnostics),
		};
	}

	/// <summary>
	/// Creates a failed generator result with the specified diagnostics. At least one diagnostic must be provided.
	/// </summary>
	/// <param name="diagnostics">The diagnostics associated with the failure result.</param>
	/// <returns>A failed generator result.</returns>
	/// <exception cref="ArgumentException">Thrown when no diagnostics are provided.</exception>
	public static GeneratorResult<T> Fail(params DiagnosticInfo[] diagnostics)
	{
		if (diagnostics is null || diagnostics.Length == 0)
		{
			throw new ArgumentException(
				"At least one diagnostic must be provided for a failure result.",
				nameof(diagnostics)
			);
		}

		// All valid...
		return new() { Diagnostics = EquatableArray<DiagnosticInfo>.Create(diagnostics) };
	}

	/// <summary>
	/// Represents an empty generator result with no value and no diagnostics.
	/// </summary>
	public static readonly GeneratorResult<T> Empty;
}
