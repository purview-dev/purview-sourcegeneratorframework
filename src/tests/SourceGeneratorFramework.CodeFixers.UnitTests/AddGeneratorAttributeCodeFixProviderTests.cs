using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class AddGeneratorAttributeCodeFixProviderTests
	: TUnitCodeFixTestBase<MissingGeneratorAttributeAnalyzer, AddGeneratorAttributeCodeFixProvider>
{
	[Test]
	public async Task AddsGeneratorAttribute(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			public sealed class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context) { }
			}
			""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions { EquivalenceKey = AddGeneratorAttributeCodeFixProvider.EquivalenceKey },
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(MissingGeneratorAttributeAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("[Generator]");
		await Assert.That(result.FixedSource).Contains("public sealed class MyGenerator : IIncrementalGenerator");
	}
}
