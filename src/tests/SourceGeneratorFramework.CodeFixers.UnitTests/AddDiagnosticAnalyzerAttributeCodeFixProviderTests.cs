using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class AddDiagnosticAnalyzerAttributeCodeFixProviderTests
	: TUnitCodeFixTestBase<MissingDiagnosticAnalyzerAttributeAnalyzer, AddDiagnosticAnalyzerAttributeCodeFixProvider>
{
	[Test]
	public async Task AddsDiagnosticAnalyzerAttribute(CancellationToken cancellationToken)
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

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions { EquivalenceKey = AddDiagnosticAnalyzerAttributeCodeFixProvider.EquivalenceKey },
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(MissingDiagnosticAnalyzerAttributeAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("[DiagnosticAnalyzer(LanguageNames.CSharp)]");
		await Assert.That(result.FixedSource).Contains("public sealed class MyAnalyzer : DiagnosticAnalyzer");
	}
}
