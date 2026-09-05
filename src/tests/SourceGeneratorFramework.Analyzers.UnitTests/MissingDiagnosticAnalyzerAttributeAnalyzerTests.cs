using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class MissingDiagnosticAnalyzerAttributeAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<MissingDiagnosticAnalyzerAttributeAnalyzer>
{
	[Test]
	public async Task DiagnosticAnalyzer_WithoutAttribute_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.Diagnostics;

			public sealed class MyAnalyzer : DiagnosticAnalyzer
			{
				public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [];
				public override void Initialize(AnalysisContext context) { }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(MissingDiagnosticAnalyzerAttributeAnalyzer.Rule.Id);
	}

	[Test]
	public async Task DiagnosticAnalyzer_WithAttribute_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.Diagnostics;

			[DiagnosticAnalyzer(LanguageNames.CSharp)]
			public sealed class MyAnalyzer : DiagnosticAnalyzer
			{
				public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [];
				public override void Initialize(AnalysisContext context) { }
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
