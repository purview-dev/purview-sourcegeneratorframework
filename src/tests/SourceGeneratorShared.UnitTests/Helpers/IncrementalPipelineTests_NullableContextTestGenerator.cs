using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.SourceGeneratorFramework.TestGenerators;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Helpers;

public class IncrementalPipelineTests_NullableContextTestGenerator
	: TUnitSourceGeneratorTestBase<NullableContextTestGenerator>
{
	[Test]
	public async Task GenerationContext_GivenNullableEnabledCompilation_WritesDirectiveAndAnnotation(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(
			"public sealed class Sample { }",
			new() { NullableContextOptions = NullableContextOptions.Enable },
			cancellationToken
		);

		var source = result.GetSource();

		await Assert.That(source).Contains("#nullable enable");
		await Assert.That(source).Contains("string? Name");
	}

	[Test]
	public async Task GenerationContext_GivenNullableDisabledCompilation_OmitsDirectiveAndStripsAnnotation(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(
			"public sealed class Sample { }",
			new() { NullableContextOptions = NullableContextOptions.Disable },
			cancellationToken
		);

		var source = result.GetSource();

		await Assert.That(source).DoesNotContain("#nullable enable");
		await Assert.That(source).DoesNotContain("string? Name");
		await Assert.That(source).Contains("string Name");
	}
}

public class IncrementalPipelineTests_ExplicitNullableContextTestGenerator
	: TUnitSourceGeneratorTestBase<ExplicitNullableContextTestGenerator>
{
	[Test]
	public async Task GenerationContext_GivenExplicitNullableEnabled_OverridesDisabledCompilation(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(
			"public sealed class Sample { }",
			new() { NullableContextOptions = NullableContextOptions.Disable },
			cancellationToken
		);

		var source = result.GetSource();

		await Assert.That(source).Contains("#nullable enable");
		await Assert.That(source).Contains("string? Name");
	}
}

public class IncrementalPipelineTests_AlwaysNullableContextTestGenerator
	: TUnitSourceGeneratorTestBase<AlwaysNullableContextTestGenerator>
{
	[Test]
	public async Task GenerationContext_GivenAlwaysModeAndDisabledCompilation_WritesDirectiveAndAnnotation(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(
			"public sealed class Sample { }",
			new() { NullableContextOptions = NullableContextOptions.Disable },
			cancellationToken
		);

		var source = result.GetSource();

		await Assert.That(source).Contains("#nullable enable");
		await Assert.That(source).Contains("string? Name");
	}
}

public class IncrementalPipelineNullableDetectionTests
{
	[Test]
	public async Task IsNullableContextEnabled_GivenNullCompilation_ReturnsNull()
	{
		await Assert.That(IncrementalPipeline.IsNullableContextEnabled(null!)).IsNull();
	}

	[Test]
	public async Task IsNullableContextEnabled_GivenEnabledCompilation_ReturnsTrue()
	{
		var compilation = TestCompilation
			.Create("public sealed class Sample { }")
			.WithOptions(
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
					NullableContextOptions.Enable
				)
			);

		await Assert.That(IncrementalPipeline.IsNullableContextEnabled(compilation)).IsTrue();
	}

	[Test]
	public async Task IsNullableContextEnabled_GivenDisabledCompilation_ReturnsFalse()
	{
		var compilation = TestCompilation
			.Create("public sealed class Sample { }")
			.WithOptions(
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
					NullableContextOptions.Disable
				)
			);

		await Assert.That(IncrementalPipeline.IsNullableContextEnabled(compilation)).IsFalse();
	}
}
