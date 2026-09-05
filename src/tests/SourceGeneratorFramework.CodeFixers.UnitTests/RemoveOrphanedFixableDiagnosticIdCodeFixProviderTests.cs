using Microsoft.CodeAnalysis.CodeFixes;
using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class RemoveOrphanedFixableDiagnosticIdCodeFixProviderTests
	: TUnitCodeFixTestBase<OrphanedFixableDiagnosticIdAnalyzer, RemoveOrphanedFixableDiagnosticIdCodeFixProvider>
{
	const string Source = """
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
	public async Task RemovesOrphanedDiagnosticId(CancellationToken cancellationToken)
	{
		var result = await ApplyCodeFixAsync(
			Source,
			new CodeFixTestOptions
			{
				EquivalenceKey = RemoveOrphanedFixableDiagnosticIdCodeFixProvider.EquivalenceKey,
				AdditionalAssemblyTypes = [typeof(CodeFixProvider)],
			},
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(OrphanedFixableDiagnosticIdAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("\"MY001\"");
		await Assert.That(result.FixedSource).DoesNotContain("\"ORPHAN\"");
	}
}
