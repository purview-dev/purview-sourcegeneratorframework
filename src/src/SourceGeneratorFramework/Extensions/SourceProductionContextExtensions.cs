using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Models;

namespace Purview.SourceGeneratorFramework.Extensions;

/// <summary>
/// Extension methods for <see cref="SourceProductionContext"/>.
/// </summary>
public static class SourceProductionContextExtensions
{
	/// <summary>
	/// Reports a single <see cref="DiagnosticInfo"/> to the source production context.
	/// </summary>
	public static void ReportDiagnostic(
		this SourceProductionContext context,
		DiagnosticInfo diagnostic
	)
	{
		if (diagnostic is null)
			throw new ArgumentNullException(nameof(diagnostic));

		context.ReportDiagnostic(diagnostic.ToDiagnostic());
	}

	/// <summary>
	/// Reports a sequence of <see cref="DiagnosticInfo"/> diagnostics to the source production context.
	/// </summary>
	public static void ReportDiagnostics(
		this SourceProductionContext context,
		IEnumerable<DiagnosticInfo> diagnostics
	)
	{
		if (diagnostics is null)
			throw new ArgumentNullException(nameof(diagnostics));

		foreach (var diagnostic in diagnostics)
			context.ReportDiagnostic(diagnostic);
	}

	/// <summary>
	/// Reports a sequence of <see cref="DiagnosticInfo"/> diagnostics to the source production context.
	/// </summary>
	public static void ReportDiagnostics(
		this SourceProductionContext context,
		EquatableArray<DiagnosticInfo> diagnostics
	)
	{
		foreach (var diagnostic in diagnostics)
			context.ReportDiagnostic(diagnostic);
	}
}
