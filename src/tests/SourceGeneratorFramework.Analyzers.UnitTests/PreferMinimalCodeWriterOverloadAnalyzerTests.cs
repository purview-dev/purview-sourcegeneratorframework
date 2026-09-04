using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class PreferMinimalCodeWriterOverloadAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<PreferMinimalCodeWriterOverloadAnalyzer>
{
	static readonly AnalyzerTestOptions Options = new()
	{
		AdditionalAssemblyTypes = [typeof(CodeWriter), typeof(GenerationSettings), typeof(PropertyDeclarationOptions)],
	};

	[Test]
	public async Task Property_WithBareOptions_WritesMinimalOverload(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Property(new PropertyDeclarationOptions("Name", TypeReference.Create<string>(), TypeDeclarationAccessibility.Public));
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferMinimalCodeWriterOverloadAnalyzer.Rule.Id);
		await Assert
			.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
			.Contains("Property(name, type, accessibility)");
	}

	[Test]
	public async Task Field_WithBareOptions_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Field(new FieldDeclarationOptions("_value", TypeReference.Create<int>(), TypeDeclarationAccessibility.Private));
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferMinimalCodeWriterOverloadAnalyzer.Rule.Id);
	}

	[Test]
	public async Task PartialMethod_WithBareOptions_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.PartialMethod(new MethodDeclarationOptions("OnChanged", TypeReference.Create<string>(), TypeDeclarationAccessibility.Public));
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		var diagnostic = await Assert.That(result).HasDiagnostic(PreferMinimalCodeWriterOverloadAnalyzer.Rule.Id);
		await Assert
			.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
			.Contains("PartialMethod(name, returnType, accessibility)");
	}

	[Test]
	public async Task ConstructorScope_WithBareOptions_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.ConstructorScope(new ConstructorDeclarationOptions("C", TypeDeclarationAccessibility.Public));
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferMinimalCodeWriterOverloadAnalyzer.Rule.Id);
	}

	[Test]
	public async Task EnumScope_WithBareOptions_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.EnumScope(new TypeDeclarationOptions("Status", TypeDeclarationAccessibility.Public));
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferMinimalCodeWriterOverloadAnalyzer.Rule.Id);
	}

	[Test]
	public async Task EnumField_WithBareOptions_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.EnumField(new EnumFieldDeclarationOptions("Ready", 1));
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferMinimalCodeWriterOverloadAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Property_WithTargetTypedNew_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Property(new("Name", TypeReference.Create<string>(), TypeDeclarationAccessibility.Public));
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(PreferMinimalCodeWriterOverloadAnalyzer.Rule.Id);
	}

	[Test]
	public async Task Property_WithObjectInitializer_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Property(new PropertyDeclarationOptions("Name", TypeReference.Create<string>(), TypeDeclarationAccessibility.Public)
					{
						HasSetter = true,
					});
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task Indexer_WithBareOptions_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Purview.SourceGeneratorFramework;

			class Emitter
			{
				public void Emit()
				{
					var writer = new CodeWriter(new GenerationSettings("G"));
					writer.Indexer(new IndexerDeclarationOptions(TypeReference.Create<string>(), new("index", TypeReference.Create<int>())));
				}
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task Property_OnOtherType_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			class Emitter
			{
				public void Emit()
				{
					var writer = new OtherWriter();
					writer.Property(new PropertyDeclarationOptions("Name", TypeReference.Create<string>(), TypeDeclarationAccessibility.Public));
				}
			}

			class OtherWriter
			{
				public void Property(PropertyDeclarationOptions declaration) { }
			}
			""";

		// Act
		var result = await AnalyzeAsync(source, Options, cancellationToken);

		// Assert
		await Assert.That(result).HasNoDiagnostics();
	}
}
