using System.ComponentModel;
using Microsoft.CodeAnalysis;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class DiagnosticAssertions
{
	/// <summary>
	/// Asserts that the <paramref name="diagnostic"/> contains a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor"/>.
	/// </summary>
	/// <param name="diagnostic">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic descriptor.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasDiagnostic(this DriverRunResult diagnostic, DiagnosticDescriptor expected)
	{
		// Don't change the name of the `diagnostic` parameter, as it is used in the generated assertion method.
		ArgumentNullException.ThrowIfNull(diagnostic);

		return expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				!diagnostic.Result.Diagnostics.Any(d => d.Id == expected.Id),
				$"expected to contain diagnostic with Id {expected.Id}\n\n"
					+ diagnostic
						.Result.GeneratedTrees.Select(t => $"  - {t.FilePath}")
						.Concat(diagnostic.Result.Diagnostics.Select(d => $"  - {d.Id}: {d.Descriptor.Title}"))
						.DefaultIfEmpty("  - (none)")
						.Aggregate((a, b) => $"{a}\n{b}")
			);
	}

	/// <summary>
	/// Asserts that the <paramref name="result"/> does not contain a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor"/>.
	/// </summary>
	/// <param name="result">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic descriptor.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnostic(this DriverRunResult result, DiagnosticDescriptor expected)
	{
		ArgumentNullException.ThrowIfNull(result);

		return expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				result.Result.Diagnostics.Any(d => d.Id == expected.Id),
				$"expected not to contain diagnostic with Id {expected.Id}\n\n"
					+ result
						.Result.GeneratedTrees.Select(t => $"  - {t.FilePath}")
						.Concat(result.Result.Diagnostics.Select(d => $"  - {d.Id}: {d.Descriptor.Title}"))
						.DefaultIfEmpty("  - (none)")
						.Aggregate((a, b) => $"{a}\n{b}")
			);
	}

	/// <summary>
	/// Asserts that the <paramref name="result"/> does not contain any diagnostics with severity <see cref="DiagnosticSeverity.Error"/>.
	/// </summary>
	/// <param name="result">The result of the driver run to check for error diagnostics.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult HasNoErrorDiagnostics(this DriverRunResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		return AssertionResult.FailIf(
			result.Result.Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error),
			"expected no error diagnostics to be reported by the generator:\n"
				+ string.Join(
					'\n',
					result
						.Result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)
						.Select(d => $"  - {d.Id}: {d.Descriptor.Title}")
				)
				+ "\n\n"
				+ result
					.Result.GeneratedTrees.Select(t => $"  - {t.FilePath}")
					.Concat(result.Result.Diagnostics.Select(d => $"  - {d.Id}: {d.Descriptor.Title}"))
					.DefaultIfEmpty("  - (none)")
					.Aggregate((a, b) => $"{a}\n{b}")
		);
	}
}
