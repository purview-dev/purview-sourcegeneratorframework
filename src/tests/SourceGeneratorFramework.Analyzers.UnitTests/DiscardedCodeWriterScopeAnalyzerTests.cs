using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class DiscardedCodeWriterScopeAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<DiscardedCodeWriterScopeAnalyzer>
{
	[Test]
	public async Task DiscardedScope_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.ClassScope(new TypeDeclarationOptions("C"));
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions
			{
				AdditionalAssemblyTypes =
				[
					typeof(CodeWriter),
					typeof(GenerationSettings),
					typeof(TypeDeclarationOptions),
				],
			},
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(DiscardedCodeWriterScopeAnalyzer.Rule.Id);
	}

	[Test]
	public async Task ScopeUsedInUsing_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					using (writer.ClassScope(new TypeDeclarationOptions("C")))
					{
						writer.Line("// body");
					}
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions
			{
				AdditionalAssemblyTypes =
				[
					typeof(CodeWriter),
					typeof(GenerationSettings),
					typeof(TypeDeclarationOptions),
				],
			},
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task NonScopeMethod_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Property(new PropertyDeclarationOptions("Name", TypeIdentity.Create<string>()));
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(
			source,
			new AnalyzerTestOptions
			{
				AdditionalAssemblyTypes =
				[
					typeof(CodeWriter),
					typeof(GenerationSettings),
					typeof(PropertyDeclarationOptions),
				],
			},
			cancellationToken
		);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}
}
