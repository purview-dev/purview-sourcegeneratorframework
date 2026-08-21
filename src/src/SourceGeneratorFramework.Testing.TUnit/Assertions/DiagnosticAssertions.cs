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
	public static AssertionResult<Diagnostic> HasDiagnostic(
		this DriverRunResult diagnostic,
		DiagnosticDescriptor expected
	)
	{
		// Don't change the name of the parameter "diagnostic" to "result" because it will break the generated assertion method name.
		if (diagnostic == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		if (expected is null)
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null");

		var matchingDiagnostic = diagnostic.DriverResult.Diagnostics.FirstOrDefault(d => d.Id == expected.Id);
		return matchingDiagnostic is null
			? (AssertionResult<Diagnostic>)
				AssertionResult.Failed($"expected to contain diagnostic with Id {expected.Id}")
			: AssertionResult<Diagnostic>.Passed(matchingDiagnostic);
	}

	/// <summary>
	/// Asserts that the <paramref name="diagnostic"/> contains a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor"/>.
	/// </summary>
	/// <param name="diagnostic">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic descriptor.</param>
	/// <param name="count">The expected number of diagnostics with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor"/>.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		this DriverRunResult diagnostic,
		DiagnosticDescriptor expected,
		int count
	)
	{
		// Don't change the name of the parameter "diagnostic" to "result" because it will break the generated assertion method name.
		if (diagnostic == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");
		if (count < 1)
			return AssertionResult.Failed($"expected {nameof(count)} is less than 1");

		// Not null... process
		if (expected is null)
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null");

		var matchingDiagnostic = diagnostic.DriverResult.Diagnostics.Where(d => d.Id == expected.Id);
		return matchingDiagnostic is null
			? (AssertionResult<IEnumerable<Diagnostic>>)
				AssertionResult.Failed($"expected to contain {count} diagnostic(s) with Id {expected.Id}")
			: AssertionResult<IEnumerable<Diagnostic>>.Passed(matchingDiagnostic);
	}

	/// <summary>
	/// Asserts that the <paramref name="diagnostic"/> contains a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor.Id"/>.
	/// </summary>
	/// <param name="diagnostic">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic Id.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<Diagnostic> HasDiagnostic(this DriverRunResult diagnostic, string expected)
	{
		// Don't change the name of the parameter "diagnostic" to "result" because it will break the generated assertion method name.
		if (diagnostic == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		if (string.IsNullOrWhiteSpace(expected))
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor.Id)} is null/ empty/ whitespace");

		var matchedDiagnotics = diagnostic.DriverResult.Diagnostics.FirstOrDefault(d => d.Id == expected);
		return matchedDiagnotics is null
			? (AssertionResult<Diagnostic>)AssertionResult.Failed($"expected to contain diagnostic with Id {expected}")
			: AssertionResult<Diagnostic>.Passed(matchedDiagnotics);
	}

	/// <summary>
	/// Asserts that the <paramref name="diagnostic"/> contains a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor.Id"/>.
	/// </summary>
	/// <param name="diagnostic">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic Id.</param>
	/// <param name="count">The expected number of diagnostics with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor.Id"/>.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<IEnumerable<Diagnostic>> HasDiagnostics(
		this DriverRunResult diagnostic,
		string expected,
		int count
	)
	{
		// Don't change the name of the parameter "diagnostic" to "result" because it will break the generated assertion method name.
		if (diagnostic == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");
		if (count < 1)
			return AssertionResult.Failed($"expected {nameof(count)} is less than 1");

		if (string.IsNullOrWhiteSpace(expected))
			return AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor.Id)} is null/ empty/ whitespace");

		var matchedDiagnotics = diagnostic.DriverResult.Diagnostics.Where(d => d.Id == expected);
		return matchedDiagnotics is null
			? (AssertionResult<IEnumerable<Diagnostic>>)
				AssertionResult.Failed($"expected to contain {count} diagnostic(s) with Id {expected}")
			: AssertionResult<IEnumerable<Diagnostic>>.Passed(matchedDiagnotics);
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
		if (result == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		return expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				result.DriverResult.Diagnostics.Any(d => d.Id == expected.Id),
				$"expected not to contain diagnostic with Id {expected.Id}"
			);
	}

	/// <summary>
	/// Asserts that the <paramref name="result"/> does not contain a diagnostic with the same Id as the <paramref name="expected"/> <see cref="DiagnosticDescriptor.Id"/>.
	/// </summary>
	/// <param name="result">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="expected">The expected diagnostic Id.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnostic(this DriverRunResult result, string expected)
	{
		if (result == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		return expected is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				result.DriverResult.Diagnostics.Any(d => d.Id == expected),
				$"expected not to contain diagnostic with Id {expected}"
			);
	}

	/// <summary>
	/// Asserts that the <paramref name="result"/> does not contain a diagnostic where the Id starts with the <paramref name="startsWithValue"/> <see cref="DiagnosticDescriptor.Id"/>.
	/// </summary>
	/// <param name="result">The result of the driver run to check for the expected diagnostic.</param>
	/// <param name="startsWithValue">The expected diagnostic Id start value.</param>
	/// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult DoesNotHaveDiagnosticThatStartsWith(
		this DriverRunResult result,
		string startsWithValue
	)
	{
		if (result == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		return startsWithValue is null
			? AssertionResult.Failed($"expected {nameof(DiagnosticDescriptor)} is null")
			: AssertionResult.FailIf(
				result.DriverResult.Diagnostics.Any(d => d.Id.StartsWith(startsWithValue, StringComparison.Ordinal)),
				$"expected not to contain Diagnostic with Id starting with {startsWithValue}"
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
		if (result == null)
			return AssertionResult.Failed($"expected {nameof(DriverRunResult)} is null");

		// Not null... process
		return AssertionResult.FailIf(
			result.DriverResult.Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error),
			"expected no error diagnostics to be reported by the generator"
		);
	}
}
