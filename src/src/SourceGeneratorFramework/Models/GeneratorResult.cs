namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// Represents the result of an incremental source generator transform, carrying either a value, diagnostics, or both.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1000:Do not declare static members on generic types"
)]
public readonly record struct GeneratorResult<T>
{
	public T? Value { get; private init; }

	public EquatableArray<DiagnosticInfo> Diagnostics { get; private init; }

	public bool IsSuccess => Value is not null;

	public bool HasDiagnostics => !Diagnostics.IsEmpty;

	public bool IsFatal => Value is null && HasDiagnostics;

	public bool IsEmpty => Value is null && !HasDiagnostics;

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

	public static GeneratorResult<T> Empty { get; }
}
