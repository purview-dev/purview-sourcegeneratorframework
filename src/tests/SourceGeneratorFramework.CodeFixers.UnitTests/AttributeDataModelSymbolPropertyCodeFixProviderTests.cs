using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class AttributeDataModelSymbolPropertyCodeFixProviderTests
	: TUnitCodeFixTestBase<AttributeDataModelSymbolPropertyAnalyzer, AttributeDataModelSymbolPropertyCodeFixProvider>
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
	public async Task ISymbolProperty_ChangesToTypeIdentity(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					ISymbol SymbolProperty
				);
				""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions { EquivalenceKey = "TypeIdentity", AdditionalAssemblyTypes = [typeof(ISymbol)] },
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(AttributeDataModelSymbolPropertyAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("TypeIdentity SymbolProperty");
		await Assert.That(result.FixedSource).DoesNotContain("ISymbol SymbolProperty");
	}

	[Test]
	public async Task ISymbolProperty_ChangesToString(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					ISymbol SymbolProperty
				);
				""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions { EquivalenceKey = "string", AdditionalAssemblyTypes = [typeof(ISymbol)] },
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(AttributeDataModelSymbolPropertyAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("string SymbolProperty");
		await Assert.That(result.FixedSource).DoesNotContain("ISymbol SymbolProperty");
	}

	[Test]
	public async Task NullableISymbolProperty_ChangesToNullableString(CancellationToken cancellationToken)
	{
		var source =
			AttributeDefinition
			+ """

				[Generate]
				public readonly record struct MyModel(
					ISymbol? SymbolProperty
				);
				""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions { EquivalenceKey = "string", AdditionalAssemblyTypes = [typeof(ISymbol)] },
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(AttributeDataModelSymbolPropertyAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("string? SymbolProperty");
		await Assert.That(result.FixedSource).DoesNotContain("ISymbol? SymbolProperty");
	}
}
