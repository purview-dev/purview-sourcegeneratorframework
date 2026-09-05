using Microsoft.CodeAnalysis.CodeFixes;
using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class AddExportCodeFixProviderAttributeCodeFixProviderTests
	: TUnitCodeFixTestBase<
		MissingExportCodeFixProviderAttributeAnalyzer,
		AddExportCodeFixProviderAttributeCodeFixProvider
	>
{
	[Test]
	public async Task AddsExportCodeFixProviderAttribute(CancellationToken cancellationToken)
	{
		const string source = """
			using System.Collections.Immutable;
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.CodeFixes;

			public sealed class MyFixer : CodeFixProvider
			{
				public override ImmutableArray<string> FixableDiagnosticIds => ["MY001"];
				public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
			}
			""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions
			{
				EquivalenceKey = AddExportCodeFixProviderAttributeCodeFixProvider.EquivalenceKey,
				AdditionalAssemblyTypes = [typeof(CodeFixProvider)],
			},
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(MissingExportCodeFixProviderAttributeAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("[ExportCodeFixProvider(LanguageNames.CSharp)]");
		await Assert.That(result.FixedSource).Contains("public sealed class MyFixer : CodeFixProvider");
	}
}
