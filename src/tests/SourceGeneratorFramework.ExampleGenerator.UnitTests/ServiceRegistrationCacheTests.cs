using System.Collections.Immutable;
using Purview.SourceGeneratorFramework.Examples;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;
using StepReason = Microsoft.CodeAnalysis.IncrementalStepRunReason;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

/// <summary>
/// Proves the <see cref="ServiceRegistrationGenerator"/> pipeline caches correctly stage-by-stage. Other
/// generator projects should mirror this pattern using <c>SourceGeneratorTestRunner.RunIncrementalAsync</c> or
/// <c>GenerateIncrementalAsync</c>.
/// </summary>
public class ServiceRegistrationCacheTests
	: TUnitSourceGeneratorTestBase<ServiceRegistrationGenerator, ServiceRegistrationTestOptions>
{
	const string Source = """
		namespace Test;

		[GenerateService]
		public class MyService { }
		""";

	static ImmutableDictionary<string, ImmutableArray<StepReason>> StepReasons(IncrementalCacheRun run)
	{
		var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<StepReason>>();
		foreach (var pair in run.Steps)
		{
			builder[pair.Key] = [.. pair.Value.SelectMany(step => step.Outputs.Select(static output => output.Reason))];
		}

		return builder.ToImmutable();
	}

	[Test]
	public async Task FirstRun_AllStagesAreNew(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync(
			[new IncrementalRunInput([Source])],
			cancellationToken: cancellationToken
		);

		var reasons = StepReasons(result.Runs[0]);
		await Assert.That(reasons).IsNotEmpty();
		await Assert.That(reasons.Values.SelectMany(static r => r).All(static r => r == StepReason.New)).IsTrue();
	}

	[Test]
	public async Task IdenticalRerun_AllStagesCached(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync([Source], cancellationToken: cancellationToken);

		var second = StepReasons(result.Runs[1]);
		await Assert.That(second).IsNotEmpty();

		// The generator's own pipeline stages must all be cached or unchanged. (Roslyn's internal
		// ForAttributeWithMetadataName steps can report Modified on rerun because the post-initialization
		// attribute source is regenerated as a new tree.)
		string[] frameworkStages =
		[
			"GetMSBuildPropertyValue_EmitServiceRegistrationInfo",
			"GetGenerationConfiguration",
			"GetGenerationContext_EmptyCapabilities",
			"ForAttribute_GenerateServiceAttribute",
		];

		await Assert
			.That(
				frameworkStages.All(stage =>
					second.TryGetValue(stage, out var reasons)
					&& reasons.All(static r => r is StepReason.Cached or StepReason.Unchanged)
				)
			)
			.IsTrue();
	}

	[Test]
	public async Task PropertyChange_MarksPropertyStageModified_AttributeStageStaysCached(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateIncrementalAsync(
			[
				new IncrementalRunInput([Source]),
				new IncrementalRunInput([Source], [(PropertyLibrary.EmitServiceRegistrationInfo, "true")]),
			],
			cancellationToken: cancellationToken
		);

		var second = StepReasons(result.Runs[1]);

		await Assert.That(second["GetMSBuildPropertyValue_EmitServiceRegistrationInfo"]).Contains(StepReason.Modified);
		await Assert
			.That(
				second["ForAttribute_GenerateServiceAttribute"]
					.All(static r => r is StepReason.Cached or StepReason.Unchanged)
			)
			.IsTrue();
	}

	[Test]
	public async Task SourceChange_MarksAttributeStageModified_PropertyStageStaysCached(
		CancellationToken cancellationToken
	)
	{
		const string changedSource = """
			namespace Test;

			[GenerateService(ServiceLifetime.Transient, Name = "Other")]
			public class OtherService { }
			""";

		var result = await GenerateIncrementalAsync(
			[new IncrementalRunInput([Source]), new IncrementalRunInput([changedSource])],
			cancellationToken: cancellationToken
		);

		var second = StepReasons(result.Runs[1]);

		await Assert.That(second["ForAttribute_GenerateServiceAttribute"]).Contains(StepReason.Modified);
		await Assert
			.That(second["GetMSBuildPropertyValue_EmitServiceRegistrationInfo"].All(static r => r == StepReason.Cached))
			.IsTrue();
	}
}
