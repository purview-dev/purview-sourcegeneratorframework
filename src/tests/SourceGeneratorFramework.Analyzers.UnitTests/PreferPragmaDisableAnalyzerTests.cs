using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class PreferPragmaDisableAnalyzerTests : TUnitDiagnosticAnalyzerTestBase<PreferPragmaDisableAnalyzer>
{
	static readonly AnalyzerTestOptions Options = new()
	{
		AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings)],
	};

	[Test]
	public async Task Line_WithPragmaDisable_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("#pragma warning disable CS8625");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferPragmaDisableAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Line_WithPragmaRestore_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("#pragma warning restore CS8625");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferPragmaDisableAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Line_WithPragmaChecksum_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("#pragma checksum \"file.cs\" \"{00000000-0000-0000-0000-000000000000}\" \"{AAAA}\"");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task Line_WithStatement_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("return value;");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task PragmaDisable_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.PragmaDisable("CS8625");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task Line_OnOtherType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			class Emitter
			{
				public void Emit()
				{
					var writer = new OtherWriter();
					writer.Line("#pragma warning disable CS8625");
				}
			}

			class OtherWriter
			{
				public void Line(string value) { }
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}
}
