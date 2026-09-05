using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class MissingGeneratorAttributeAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<MissingGeneratorAttributeAnalyzer>
{
	[Test]
	public async Task IncrementalGenerator_WithoutAttribute_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public sealed class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context) { }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(MissingGeneratorAttributeAnalyzer.Rule.Id);
	}

	[Test]
	public async Task LegacyGenerator_WithoutAttribute_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public sealed class MyGenerator : ISourceGenerator
			{
				public void Initialize(GeneratorInitializationContext context) { }
				public void Execute(GeneratorExecutionContext context) { }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(MissingGeneratorAttributeAnalyzer.Rule.Id);
	}

	[Test]
	public async Task IncrementalGenerator_WithAttribute_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			[Generator]
			public sealed class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context) { }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task UnrelatedType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			public sealed class NotAComponent
			{
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
