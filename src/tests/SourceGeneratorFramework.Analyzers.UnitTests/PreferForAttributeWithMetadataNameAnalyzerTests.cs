namespace Purview.SourceGeneratorFramework.Analyzers;

public class PreferForAttributeWithMetadataNameAnalyzerTests
{
	[Test]
	public async Task CreateSyntaxProvider_ReportsDiagnostic()
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					context.SyntaxProvider.CreateSyntaxProvider(
						static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax,
						static (ctx, _) => ctx.Node
					);
				}
			}
			""";

		var diagnostics =
			await new PreferForAttributeWithMetadataNameAnalyzer().GetAnalyzerDiagnosticsAsync(
				source
			);

		await Assert.That(diagnostics).Count().IsEqualTo(1);
		await Assert.That(diagnostics.First().Id).IsEqualTo("PSGFR11");
	}

	[Test]
	public async Task ForAttributeWithMetadataName_DoesNotReportDiagnostic()
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context)
				{
					context.SyntaxProvider.ForAttributeWithMetadataName(
						"System.ObsoleteAttribute",
						static (node, _) => true,
						static (ctx, _) => ctx.TargetSymbol
					);
				}
			}
			""";

		var diagnostics =
			await new PreferForAttributeWithMetadataNameAnalyzer().GetAnalyzerDiagnosticsAsync(
				source
			);

		await Assert.That(diagnostics).IsEmpty();
	}
}
