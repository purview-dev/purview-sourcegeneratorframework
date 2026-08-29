using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class UseIncrementalGeneratorAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<UseIncrementalGeneratorAnalyzer>
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

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(UseIncrementalGeneratorAnalyzer.Rule.Id);
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

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
