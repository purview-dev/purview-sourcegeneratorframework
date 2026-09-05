using Microsoft.CodeAnalysis.CodeFixes;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class MissingExportCodeFixProviderAttributeAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<MissingExportCodeFixProviderAttributeAnalyzer>
{
	[Test]
	public async Task CodeFixProvider_WithoutExportAttribute_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.CodeFixes;

			public sealed class MyFixer : CodeFixProvider
			{
				public override ImmutableArray<string> FixableDiagnosticIds => ["MY001"];
				public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(MissingExportCodeFixProviderAttributeAnalyzer.Rule.Id);
	}

	[Test]
	public async Task CodeFixProvider_WithExportAttribute_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.CodeFixes;

			[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MyFixer))]
			public sealed class MyFixer : CodeFixProvider
			{
				public override ImmutableArray<string> FixableDiagnosticIds => ["MY001"];
				public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
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

	protected override AnalyzerTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		AnalyzerTestOptions options,
		CancellationToken cancellationToken
	) => base.OnBeforeRun(sources, options.WithAdditionalAssemblyTypes(typeof(CodeFixProvider)), cancellationToken);
}
