using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class PreferNullableContextOverloadCodeFixProviderTests
	: TUnitCodeFixTestBase<PreferNullableContextOverloadAnalyzer, PreferNullableContextOverloadCodeFixProvider>
{
	[Test]
	public async Task MakeNullable_BareCall_PassesWriterArgument(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit(CodeWriter writer)
				{
					var nullable = TypeIdentity.Create<string>().MakeNullable();
					writer.Type(nullable);
				}
			}
			""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions
			{
				EquivalenceKey = PreferNullableContextOverloadCodeFixProvider.EquivalenceKey,
				AdditionalAssemblyTypes = [typeof(TypeIdentity)],
			},
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(PreferNullableContextOverloadAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("MakeNullable(writer)");
	}

	[Test]
	public async Task Nullable_BareCall_PassesSettingsArgument(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit(GenerationSettings settings)
				{
					TypeReference reference = TypeIdentity.Create<string>().AsTypeReference();
					_ = reference.Nullable();
				}
			}
			""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions
			{
				EquivalenceKey = PreferNullableContextOverloadCodeFixProvider.EquivalenceKey,
				AdditionalAssemblyTypes = [typeof(TypeIdentity)],
			},
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(PreferNullableContextOverloadAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("Nullable(settings)");
	}

	[Test]
	public async Task FixAll_GivenMultipleDocuments_FixesEveryInstance(CancellationToken cancellationToken)
	{
		string[] sources =
		[
			"""
				using Purview.SourceGeneratorFramework;

				class EmitterA
				{
					public void Emit(CodeWriter writer)
					{
						var one = TypeIdentity.Create<string>().MakeNullable();
						var two = TypeIdentity.Create<string>().MakeNullable();
						writer.Type(one);
						writer.Type(two);
					}
				}
				""",
			"""
				using Purview.SourceGeneratorFramework;

				class EmitterB
				{
					public void Emit(CodeWriter writer)
					{
						var three = TypeIdentity.Create<string>().MakeNullable();
						writer.Type(three);
					}
				}
				""",
		];

		var result = await ApplyFixAllAsync(
			sources,
			new CodeFixTestOptions
			{
				EquivalenceKey = PreferNullableContextOverloadCodeFixProvider.EquivalenceKey,
				AdditionalAssemblyTypes = [typeof(TypeIdentity)],
			},
			cancellationToken
		);

		await Assert.That(result.Diagnostics).IsNotEmpty();
		foreach (var fixedSource in result.FixedSources.Values)
		{
			await Assert.That(fixedSource).DoesNotContain(".MakeNullable()");
			await Assert.That(fixedSource).Contains(".MakeNullable(writer)");
		}
	}
}
