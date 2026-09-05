using Microsoft.CodeAnalysis.CodeFixes;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class NonPublicRoslynComponentAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<NonPublicRoslynComponentAnalyzer>
{
	[Test]
	public async Task InternalGenerator_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			[Generator]
			internal sealed class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context) { }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(NonPublicRoslynComponentAnalyzer.Rule.Id);
	}

	[Test]
	public async Task InternalAnalyzer_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.Diagnostics;

			[DiagnosticAnalyzer(LanguageNames.CSharp)]
			internal sealed class MyAnalyzer : DiagnosticAnalyzer
			{
				public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [];
				public override void Initialize(AnalysisContext context) { }
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(NonPublicRoslynComponentAnalyzer.Rule.Id);
	}

	[Test]
	public async Task InternalCodeFixProvider_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.CodeFixes;

			[ExportCodeFixProvider(LanguageNames.CSharp)]
			internal sealed class MyFixer : CodeFixProvider
			{
				public override ImmutableArray<string> FixableDiagnosticIds => ["MY001"];
				public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(NonPublicRoslynComponentAnalyzer.Rule.Id);
	}

	[Test]
	public async Task NestedInInternalType_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			internal sealed class Container
			{
				[Generator]
				public sealed class MyGenerator : IIncrementalGenerator
				{
					public void Initialize(IncrementalGeneratorInitializationContext context) { }
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(NonPublicRoslynComponentAnalyzer.Rule.Id);
	}

	[Test]
	public async Task PublicGenerator_DoesNotReportDiagnostic(CancellationToken cancellationToken)
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
	public async Task UnrelatedInternalType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			internal sealed class NotAComponent
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
