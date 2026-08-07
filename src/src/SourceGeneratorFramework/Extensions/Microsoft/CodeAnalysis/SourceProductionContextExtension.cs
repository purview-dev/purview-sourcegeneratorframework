namespace Microsoft.CodeAnalysis;

public static class SourceProductionContextExtension
{
	extension(SourceProductionContext spc)
	{
		public void ReportDiagnostics(IEnumerable<DiagnosticInfo> diagnostics)
		{
			if (diagnostics is null)
				throw new ArgumentNullException(nameof(diagnostics));

			foreach (var diagnostic in diagnostics)
			{
				spc.ReportDiagnostic(diagnostic.ToDiagnostic());
			}
		}
	}
}
