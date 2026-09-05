using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class PreferStructuredCodeWriterIfBlockAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<PreferStructuredCodeWriterIfBlockAnalyzer>
{
	static readonly AnalyzerTestOptions Options = new()
	{
		AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings)],
	};

	[Test]
	public async Task OpenBlockScope_WithIfHeader_SuggestsIfBlockScope(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					using (writer.OpenBlockScope("if (enabled)"))
						writer.Return();
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterIfBlockAnalyzer.Rule.Id);
		await Assert
			.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
			.Contains("IfBlockScope");
	}

	[Test]
	public async Task OpenBlock_WithIfHeader_SuggestsIfBlock(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.OpenBlock("if (enabled)", body => body.Return());
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterIfBlockAnalyzer.Rule.Id);
		await Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Contains("IfBlock");
	}

	[Test]
	public async Task OpenBlockScope_WithElseIfHeader_SuggestsElseIfScope(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					using (writer.OpenBlockScope("else if (retry)"))
						writer.Return();
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterIfBlockAnalyzer.Rule.Id);
		await Assert
			.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
			.Contains("ElseIfScope");
	}

	[Test]
	public async Task OpenBlock_WithElseHeader_SuggestsElse(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.OpenBlock("else", body => body.Return());
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterIfBlockAnalyzer.Rule.Id);
		await Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Contains("Else");
	}

	[Test]
	public async Task DelimitedBlock_WithIfHeader_SuggestsIfBlock(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.DelimitedBlock("if (enabled)", "{", "}", body => body.Return());
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterIfBlockAnalyzer.Rule.Id);
		await Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Contains("IfBlock");
	}

	[Test]
	public async Task OpenDelimitedBlockScope_WithIfHeader_SuggestsIfBlockScope(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					using (writer.OpenDelimitedBlockScope("if (enabled)", "{", "}"))
						writer.Return();
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferStructuredCodeWriterIfBlockAnalyzer.Rule.Id);
		await Assert
			.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
			.Contains("IfBlockScope");
	}

	[Test]
	public async Task OpenBlockScope_WithForHeader_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					using (writer.OpenBlockScope("for (int i = 0; i < 3; i++)"))
						writer.Return();
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task OpenBlockScope_WithNullHeader_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					using (writer.OpenBlockScope(null))
						writer.Return();
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task IfBlock_StructuredCall_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.IfBlock("enabled", body => body.Return());
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task OpenBlockScope_OnOtherType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			class Emitter
			{
				public void Emit()
				{
					var writer = new OtherWriter();
					using (writer.OpenBlockScope("if (enabled)"))
					{
					}
				}
			}

			class OtherWriter
			{
				public System.IDisposable OpenBlockScope(string header) => null!;
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}
}
