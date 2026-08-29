using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class PipelineModelReferenceEqualityCollectionCodeFixProviderTests
	: TUnitCodeFixTestBase<
		PipelineModelReferenceEqualityCollectionAnalyzer,
		PipelineModelReferenceEqualityCollectionCodeFixProvider
	>
{
	[Test]
	public async Task ImmutableArrayRecordParameter_ChangesToEquatableArray(CancellationToken cancellationToken)
	{
		const string source = """
			using System.Collections.Immutable;
			using Purview.SourceGeneratorFramework;

			public sealed record MyModel(ImmutableArray<string> Values);

			public class Generator
			{
				public GeneratorResult<MyModel> Transform() => default;
			}
			""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions
			{
				EquivalenceKey = PipelineModelReferenceEqualityCollectionCodeFixProvider.EquivalenceKey,
				AdditionalAssemblyTypes = [typeof(GeneratorResult<>)],
			},
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(PipelineModelReferenceEqualityCollectionAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("EquatableArray<string> Values");
		await Assert.That(result.FixedSource).DoesNotContain("ImmutableArray<string> Values");
	}
}
