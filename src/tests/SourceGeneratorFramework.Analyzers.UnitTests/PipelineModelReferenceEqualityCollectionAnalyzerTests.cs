using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class PipelineModelReferenceEqualityCollectionAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<PipelineModelReferenceEqualityCollectionAnalyzer>
{
	[Test]
	public async Task ImmutableArrayProperty_InPipelineModel_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System.Collections.Immutable;
			using Purview.SourceGeneratorFramework;

			public sealed record MyModel
			{
				public ImmutableArray<string> Values { get; init; }
			}

			public class Generator
			{
				public GeneratorResult<MyModel> Transform() => default;
			}
			""";

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions().WithAdditionalAssemblyTypes(typeof(GeneratorResult<>)),
			cancellationToken
		);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PipelineModelReferenceEqualityCollectionAnalyzer.Rule.Id);
	}

	[Test]
	public async Task EquatableArrayProperty_InPipelineModel_DoesNotReportDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			public sealed record MyModel
			{
				public EquatableArray<string> Values { get; init; }
			}

			public class Generator
			{
				public GeneratorResult<MyModel> Transform() => default;
			}
			""";

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions().WithAdditionalAssemblyTypes(typeof(GeneratorResult<>)),
			cancellationToken
		);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task ImmutableArrayRecordParameter_InPipelineModel_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
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

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions().WithAdditionalAssemblyTypes(typeof(GeneratorResult<>)),
			cancellationToken
		);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PipelineModelReferenceEqualityCollectionAnalyzer.Rule.Id);
	}

	[Test]
	public async Task ListProperty_InPipelineModel_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System.Collections.Generic;
			using Purview.SourceGeneratorFramework;

			public sealed record MyModel
			{
				public List<string> Values { get; init; }
			}

			public class Generator
			{
				public GeneratorResult<MyModel> Transform() => default;
			}
			""";

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions().WithAdditionalAssemblyTypes(typeof(GeneratorResult<>)),
			cancellationToken
		);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PipelineModelReferenceEqualityCollectionAnalyzer.Rule.Id);
	}

	[Test]
	public async Task ArrayProperty_InPipelineModel_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			public sealed record MyModel
			{
				public string[] Values { get; init; }
			}

			public class Generator
			{
				public GeneratorResult<MyModel> Transform() => default;
			}
			""";

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions().WithAdditionalAssemblyTypes(typeof(GeneratorResult<>)),
			cancellationToken
		);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PipelineModelReferenceEqualityCollectionAnalyzer.Rule.Id);
	}

	[Test]
	public async Task ImmutableArrayProperty_InNonPipelineType_DoesNotReportDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System.Collections.Immutable;
			using Purview.SourceGeneratorFramework;

			public sealed record MyModel
			{
				public ImmutableArray<string> Values { get; init; }
			}
			""";

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions().WithAdditionalAssemblyTypes(typeof(GeneratorResult<>)),
			cancellationToken
		);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task NestedModelElement_WithImmutableArray_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using System.Collections.Immutable;
			using Purview.SourceGeneratorFramework;

			public sealed record ChildModel(ImmutableArray<string> Values);

			public sealed record ParentModel
			{
				public EquatableArray<ChildModel> Children { get; init; }
			}

			public class Generator
			{
				public GeneratorResult<ParentModel> Transform() => default;
			}
			""";

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions().WithAdditionalAssemblyTypes(typeof(GeneratorResult<>)),
			cancellationToken
		);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PipelineModelReferenceEqualityCollectionAnalyzer.Rule.Id);
	}

	[Test]
	public async Task ImmutableArrayProperty_InIncrementalValuesProviderModel_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			using System.Collections.Immutable;
			using Microsoft.CodeAnalysis;
			using Purview.SourceGeneratorFramework;

			public sealed record MyModel
			{
				public ImmutableArray<string> Values { get; init; }
			}

			public class Generator
			{
				public IncrementalValuesProvider<MyModel> Transform() => default!;
			}
			""";

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions().WithAdditionalAssemblyTypes(
				typeof(GeneratorResult<>),
				typeof(IncrementalValuesProvider<>)
			),
			cancellationToken
		);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PipelineModelReferenceEqualityCollectionAnalyzer.Rule.Id);
	}
}
