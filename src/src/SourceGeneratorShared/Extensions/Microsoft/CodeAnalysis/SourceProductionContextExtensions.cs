using System.ComponentModel;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Extension methods for <see cref="SourceProductionContext"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SourceProductionContextExtensions
{
	extension(SourceProductionContext context)
	{
		/// <summary>
		/// Reports a single <see cref="DiagnosticInfo"/> to the source production context.
		/// </summary>
		public void ReportDiagnostic(DiagnosticInfo diagnostic)
		{
			if (diagnostic is null)
				throw new ArgumentNullException(nameof(diagnostic));

			context.ReportDiagnostic(diagnostic.ToDiagnostic());
		}

		/// <summary>
		/// Reports a sequence of <see cref="DiagnosticInfo"/> diagnostics to the source production context.
		/// </summary>
		public void ReportDiagnostics(IEnumerable<DiagnosticInfo> diagnostics)
		{
			if (diagnostics is null)
				throw new ArgumentNullException(nameof(diagnostics));

			foreach (var diagnostic in diagnostics)
				context.ReportDiagnostic(diagnostic);
		}
	}
}
