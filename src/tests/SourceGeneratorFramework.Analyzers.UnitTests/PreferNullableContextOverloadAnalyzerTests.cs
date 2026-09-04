using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class PreferNullableContextOverloadAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<PreferNullableContextOverloadAnalyzer>
{
	[Test]
	public async Task MakeNullable_BareCall_ReportsDiagnostic(CancellationToken cancellationToken)
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

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions { AdditionalAssemblyTypes = [typeof(TypeIdentity)] },
			cancellationToken
		);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferNullableContextOverloadAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Nullable_BareCall_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					TypeReference reference = TypeIdentity.Create<string>().AsTypeReference();
					var nullable = reference.Nullable();
				}
			}
			""";

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions { AdditionalAssemblyTypes = [typeof(TypeIdentity)] },
			cancellationToken
		);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferNullableContextOverloadAnalyzer.Rule.Id);
	}

	[Test]
	public async Task MakeNullable_WithContextArgument_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit(CodeWriter writer)
				{
					var nullable = TypeIdentity.Create<string>().MakeNullable(writer);
					writer.Type(nullable);
				}
			}
			""";

		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions { AdditionalAssemblyTypes = [typeof(TypeIdentity)] },
			cancellationToken
		);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task MakeNullable_OnUnrelatedType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		const string source = """
			class Other
			{
				public string MakeNullable() => string.Empty;
			}

			class Emitter
			{
				public void Emit()
				{
					var other = new Other();
					_ = other.MakeNullable();
				}
			}
			""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
