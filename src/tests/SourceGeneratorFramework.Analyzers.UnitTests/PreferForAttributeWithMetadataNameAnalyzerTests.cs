using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class PreferForAttributeWithMetadataNameAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<PreferForAttributeWithMetadataNameAnalyzer>
{
	[Test]
	public async Task CreateSyntaxProvider_ReportsDiagnostic(CancellationToken cancellationToken)
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

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferForAttributeWithMetadataNameAnalyzer.Rule.Id);
	}

	[Test]
	public async Task ForAttributeWithMetadataName_DoesNotReportDiagnostic(CancellationToken cancellationToken)
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

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
