using System.Collections.Immutable;
using Purview.SourceGeneratorFramework.TestGenerators;
using StepReason = Microsoft.CodeAnalysis.IncrementalStepRunReason;

namespace Purview.SourceGeneratorFramework;

public class IncrementalPipelineCacheTests
{
	const string AttributedSource = """
		[TestAttribute]
		public partial class MyClass { }
		""";

	const string ChangedAttributedSource = """
		[TestAttribute]
		public partial class AnotherClass { }
		""";

	const string TestAttributeSource = """
		[System.AttributeUsage(System.AttributeTargets.Class)]
		public sealed class TestAttribute : System.Attribute { }
		""";

	static SourceGeneratorTestOptions CreateOptions() =>
		new SourceGeneratorTestOptions()
			.WithAdditionalSources(TestAttributeSource)
			.WithExcludeGeneratedSourceHintNames("TestAttribute");

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
		var runner = new SourceGeneratorTestRunner<TestGenerator>();

		var result = await runner.RunIncrementalAsync(
			[new IncrementalRunInput([AttributedSource])],
			CreateOptions(),
			cancellationToken
		);

		var reasons = StepReasons(result.Runs[0]);
		await Assert.That(reasons).IsNotEmpty();
		await Assert
			.That(reasons.Values.SelectMany(static reasons => reasons).All(static r => r == StepReason.New))
			.IsTrue();
	}

	[Test]
	public async Task IdenticalRerun_AllStagesCached(CancellationToken cancellationToken)
	{
		var runner = new SourceGeneratorTestRunner<TestGenerator>();

		var result = await runner.RunIncrementalAsync([AttributedSource], CreateOptions(), cancellationToken);

		var first = StepReasons(result.Runs[0]);
		var second = StepReasons(result.Runs[1]);

		await Assert.That(second).IsNotEmpty();
		await Assert
			.That(
				second
					.Values.SelectMany(static reasons => reasons)
					.All(static r => r is StepReason.Cached or StepReason.Unchanged)
			)
			.IsTrue();
		await Assert.That(first["ForAttribute_TestAttribute"].All(static r => r == StepReason.New)).IsTrue();
	}

	[Test]
	public async Task SourceChange_MarksAttributeStageModified_PropertyStagesStayCached(
		CancellationToken cancellationToken
	)
	{
		var runner = new SourceGeneratorTestRunner<TestGenerator>();

		var result = await runner.RunIncrementalAsync(
			[new IncrementalRunInput([AttributedSource]), new IncrementalRunInput([ChangedAttributedSource])],
			CreateOptions(),
			cancellationToken
		);

		var second = StepReasons(result.Runs[1]);

		await Assert.That(second["ForAttribute_TestAttribute"]).Contains(StepReason.Modified);
		await Assert
			.That(second["GetMSBuildPropertyValue_DisableTestGenerator"].All(static r => r == StepReason.Cached))
			.IsTrue();
	}

	[Test]
	public async Task PropertyChange_MarksPropertyStageModified_AttributeStageStaysCached(
		CancellationToken cancellationToken
	)
	{
		var runner = new SourceGeneratorTestRunner<TestGenerator>();

		var result = await runner.RunIncrementalAsync(
			[
				new IncrementalRunInput([AttributedSource]),
				new IncrementalRunInput([AttributedSource], [("build_property.DisableTestGenerator", "true")]),
			],
			CreateOptions(),
			cancellationToken
		);

		var second = StepReasons(result.Runs[1]);

		await Assert.That(second["GetMSBuildPropertyValue_DisableTestGenerator"]).Contains(StepReason.Modified);
		await Assert.That(second["ForAttribute_TestAttribute"].All(static r => r == StepReason.Cached)).IsTrue();
	}

	[Test]
	public async Task ConfigChange_MarksConfigurationStagesModified_AttributeStageStaysCached(
		CancellationToken cancellationToken
	)
	{
		var runner = new SourceGeneratorTestRunner<DiagnosticTestGenerator>();

		var result = await runner.RunIncrementalAsync(
			[
				new IncrementalRunInput([AttributedSource]),
				new IncrementalRunInput(
					[AttributedSource],
					[(SourceGeneratorBuildProperties.ValidateCodeWriterScopes, "false")]
				),
			],
			CreateOptions(),
			cancellationToken
		);

		var second = StepReasons(result.Runs[1]);

		await Assert.That(second["GetGenerationConfiguration"]).Contains(StepReason.Modified);
		await Assert.That(second["GetGenerationContext_EmptyCapabilities"]).Contains(StepReason.Modified);
		await Assert.That(second["ForAttribute_TestAttribute"].All(static r => r == StepReason.Cached)).IsTrue();
	}
}
