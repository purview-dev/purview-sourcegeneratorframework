using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class PreferStructuredCodeWriterApiAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<PreferStructuredCodeWriterApiAnalyzer>
{
	[Test]
	public async Task WriteLine_WithClassDeclaration_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.WriteLine("public class C { }");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions { AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings)] },
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterApiAnalyzer.Rule.Id);
	}

	[Test]
	public async Task WriteLine_WithStatement_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.WriteLine("return value;");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions { AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings)] },
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task WriteLine_WithUsingStatement_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.WriteLine("using (var stream = Open())");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions { AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings)] },
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task WriteMethodCall_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.WriteMethodCall("Run", "value");
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions { AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings)] },
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}
}
