namespace Purview.SourceGeneratorFramework.Analyzers;

public class UseIncrementalGeneratorAnalyzerTests
{
	[Test]
	public async Task ISourceGenerator_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : ISourceGenerator
			{
				public void Initialize(GeneratorInitializationContext context) { }
				public void Execute(GeneratorExecutionContext context) { }
			}
			""";

		var diagnostics = await new UseIncrementalGeneratorAnalyzer().GetAnalyzerDiagnosticsAsync(
			source,
			cancellationToken
		);

		await Assert.That(diagnostics).Count().IsEqualTo(1);
		await Assert.That(diagnostics.First().Id).IsEqualTo("PSGFR12");
	}

	[Test]
	public async Task IIncrementalGenerator_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context) { }
			}
			""";

		var diagnostics = await new UseIncrementalGeneratorAnalyzer().GetAnalyzerDiagnosticsAsync(
			source,
			cancellationToken
		);

		await Assert.That(diagnostics).IsEmpty();
	}
}
