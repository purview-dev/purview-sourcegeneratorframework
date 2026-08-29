using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class GenerationCapabilitiesMustBeRecordAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<GenerationCapabilitiesMustBeRecordAnalyzer>
{
	[Test]
	public async Task GenerationContext_GivenClassCapabilities_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			public sealed class MyCapabilities
			{
			}

			public sealed class MyContext
				: GenerationContext<MyCapabilities>
			{
				public MyContext(
					MyCapabilities capabilities,
					GenerationSettings settings
				)
					: base(capabilities, settings)
				{
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(GenerationCapabilitiesMustBeRecordAnalyzer.Rule.Id);
	}

	[Test]
	public async Task GenerationContext_GivenRecordCapabilities_DoesNotReportDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			public sealed record MyCapabilities(
				bool HasEntityFrameworkCore
			);

			public sealed class MyContext
				: GenerationContext<MyCapabilities>
			{
				public MyContext(
					MyCapabilities capabilities,
					GenerationSettings settings
				)
					: base(capabilities, settings)
				{
				}
			}
			""";

		var diagnostics = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(diagnostics).HasNoDiagnostics();
	}

	[Test]
	public async Task GenerationContext_GivenRecordStructCapabilities_DoesNotReportDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			public readonly record struct MyCapabilities(
				bool HasEntityFrameworkCore
			);

			public sealed class MyContext
				: GenerationContext<MyCapabilities>
			{
				public MyContext(
					MyCapabilities capabilities,
					GenerationSettings settings
				)
					: base(capabilities, settings)
				{
				}
			}
			""";

		var diagnostics = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(diagnostics).HasNoDiagnostics();
	}

	[Test]
	public async Task UnrelatedGenericType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			public sealed class MyCapabilities
			{
			}

			public sealed class Wrapper<T>
			{
			}

			public sealed class Consumer
			{
				public Wrapper<MyCapabilities>? Value { get; }
			}
			""";

		var diagnostics = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(diagnostics).HasNoDiagnostics();
	}

	protected override AnalyzerTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		AnalyzerTestOptions options,
		CancellationToken cancellationToken
	) => base.OnBeforeRun(sources, options.WithAdditionalAssemblyTypes(typeof(GenerationContext<>)), cancellationToken);
}
