namespace Purview.SourceGeneratorFramework.Analyzers;

public class AvoidRegisterImplementationSourceOutputAnalyzerTests
{
	[Test]
	public async Task RegisterImplementationSourceOutput_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					context.RegisterImplementationSourceOutput(
						context.CompilationProvider,
						static (spc, compilation) => { }
					);
				}
			}
			""";

		var diagnostics = await new AvoidRegisterImplementationSourceOutputAnalyzer().GetAnalyzerDiagnosticsAsync(
			source,
			cancellationToken
		);

		await Assert.That(diagnostics).Count().IsEqualTo(1);
		await Assert.That(diagnostics.First().Id).IsEqualTo("PSGFR14");
	}

	[Test]
	public async Task RegisterSourceOutput_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					context.RegisterSourceOutput(
						context.CompilationProvider,
						static (spc, compilation) => { }
					);
				}
			}
			""";

		var diagnostics = await new AvoidRegisterImplementationSourceOutputAnalyzer().GetAnalyzerDiagnosticsAsync(
			source,
			cancellationToken
		);

		await Assert.That(diagnostics).IsEmpty();
	}
}
