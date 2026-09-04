using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class PreferHashDefinesAnalyzerTests : TUnitDiagnosticAnalyzerTestBase<PreferHashDefinesAnalyzer>
{
	static readonly AnalyzerTestOptions Options = new()
	{
		AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings)],
	};

	[Test]
	public async Task Line_WithIfDirective_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("#if NET");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferHashDefinesAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Line_WithEndIfDirective_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("#endif");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferHashDefinesAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Line_WithElseDirective_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("#else");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferHashDefinesAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Line_WithRegionDirective_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Line("#region Generated");
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
	public async Task HashDefines_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.HashDefines("NET", body => body.Line("// NET only"));
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
					writer.Line("#if NET");
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
