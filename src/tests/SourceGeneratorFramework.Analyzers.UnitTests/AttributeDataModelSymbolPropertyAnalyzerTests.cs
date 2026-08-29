using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.Analyzers;

public sealed class AttributeDataModelSymbolPropertyAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<AttributeDataModelSymbolPropertyAnalyzer>
{
	const string AttributeDefinition = """
		using System;
		using Microsoft.CodeAnalysis;

		namespace Purview.SourceGeneratorFramework.Generators;

		[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
		public sealed class GenerateAttribute : Attribute { }

		public readonly record struct TypeIdentity;
		""";

	[Test]
	public async Task ISymbolProperty_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					ISymbol SymbolProperty
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelSymbolPropertyAnalyzer.Rule.Id);
	}

	[Test]
	public async Task ITypeSymbolProperty_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					ITypeSymbol TypeSymbolProperty
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelSymbolPropertyAnalyzer.Rule.Id);
	}

	[Test]
	public async Task INamedTypeSymbolProperty_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					INamedTypeSymbol NamedTypeSymbolProperty
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelSymbolPropertyAnalyzer.Rule.Id);
	}

	[Test]
	public async Task SystemTypeProperty_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					Type TypeProperty
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelSymbolPropertyAnalyzer.Rule.Id);
	}

	[Test]
	public async Task NullableISymbolProperty_ReportsDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					ISymbol? SymbolProperty
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostics(1);
		await Assert.That(result).HasDiagnostic(AttributeDataModelSymbolPropertyAnalyzer.Rule.Id);
	}

	[Test]
	public async Task TypeIdentityProperty_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					TypeIdentity TypeIdentityProperty
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task StringProperty_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					string StringProperty
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	[Test]
	public async Task IntProperty_DoesNotReportDiagnostic(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					int IntProperty
				);
				""";

		var result = await AnalyzeAsync(source, cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}

	protected override AnalyzerTestOptions OnBeforeRun(
		IEnumerable<string> sources,
		AnalyzerTestOptions options,
		CancellationToken cancellationToken
	) => base.OnBeforeRun(sources, options.WithAdditionalAssemblyTypes(typeof(ISymbol)), cancellationToken);
}
