using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class AvoidRegisterImplementationSourceOutputAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<AvoidRegisterImplementationSourceOutputAnalyzer>
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

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AvoidRegisterImplementationSourceOutputAnalyzer.Rule.Id);
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

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
