using Microsoft.CodeAnalysis.CodeFixes;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class OrphanedFixableDiagnosticIdAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<OrphanedFixableDiagnosticIdAnalyzer>
{
	const string AnalyzerAndFixer = """
		using Microsoft.CodeAnalysis;
		using Microsoft.CodeAnalysis.CodeFixes;
		using Microsoft.CodeAnalysis.Diagnostics;

		[DiagnosticAnalyzer(LanguageNames.CSharp)]
		public sealed class MyAnalyzer : DiagnosticAnalyzer
		{
			public static readonly DiagnosticDescriptor Rule = new(
				"MY001",
				"Title",
				"Message",
				"Category",
				DiagnosticSeverity.Warning,
				isEnabledByDefault: true
			);

			public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];
			public override void Initialize(AnalysisContext context) { }
		}

		[ExportCodeFixProvider(LanguageNames.CSharp)]
		public sealed class MyFixer : CodeFixProvider
		{
			public override ImmutableArray<string> FixableDiagnosticIds => ["MY001", "ORPHAN"];
			public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
		}
		""";

	[Test]
	public async Task Fixer_WithOrphanedDiagnosticId_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		var result = await AnalyzeAsync(AnalyzerAndFixer, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(OrphanedFixableDiagnosticIdAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Fixer_WithMatchingDiagnosticId_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.CodeFixes;
			using Microsoft.CodeAnalysis.Diagnostics;

			[DiagnosticAnalyzer(LanguageNames.CSharp)]
			public sealed class MyAnalyzer : DiagnosticAnalyzer
			{
				public static readonly DiagnosticDescriptor Rule = new(
					"MY001",
					"Title",
					"Message",
					"Category",
					DiagnosticSeverity.Warning,
					isEnabledByDefault: true
				);

				public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];
				public override void Initialize(AnalysisContext context) { }
			}

			[ExportCodeFixProvider(LanguageNames.CSharp)]
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
	public async Task Fixer_WithoutSourceAnalyzer_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.CodeFixes;

			[ExportCodeFixProvider(LanguageNames.CSharp)]
			public sealed class MyFixer : CodeFixProvider
			{
				public override ImmutableArray<string> FixableDiagnosticIds => ["ORPHAN"];
				public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
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
