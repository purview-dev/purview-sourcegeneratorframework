using Microsoft.CodeAnalysis.CodeFixes;
using Purview.SourceGeneratorFramework.Analyzers;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.CodeFixers;

public sealed class MakeRoslynComponentPublicCodeFixProviderTests
	: TUnitCodeFixTestBase<NonPublicRoslynComponentAnalyzer, MakeRoslynComponentPublicCodeFixProvider>
{
	[Test]
	public async Task InternalGenerator_BecomesPublic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			[Generator]
			internal sealed class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context) { }
			}
			""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions { EquivalenceKey = MakeRoslynComponentPublicCodeFixProvider.EquivalenceKey },
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(NonPublicRoslynComponentAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("[Generator]");
		await Assert.That(result.FixedSource).Contains("public sealed class MyGenerator");
		await Assert.That(result.FixedSource).DoesNotContain("internal sealed class MyGenerator");
	}

	[Test]
	public async Task GeneratorWithoutModifier_BecomesPublic(CancellationToken cancellationToken)
	{
		const string source = """
			using Microsoft.CodeAnalysis;

			[Generator]
			sealed class MyGenerator : IIncrementalGenerator
			{
				public void Initialize(IncrementalGeneratorInitializationContext context) { }
			}
			""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions { EquivalenceKey = MakeRoslynComponentPublicCodeFixProvider.EquivalenceKey },
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(NonPublicRoslynComponentAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("public sealed class MyGenerator");
	}

	[Test]
	public async Task InternalCodeFixProvider_BecomesPublic(CancellationToken cancellationToken)
	{
		const string source = """
			using System.Collections.Immutable;
			using Microsoft.CodeAnalysis;
			using Microsoft.CodeAnalysis.CodeFixes;

			[ExportCodeFixProvider(LanguageNames.CSharp)]
			internal sealed class MyFixer : CodeFixProvider
			{
				public override ImmutableArray<string> FixableDiagnosticIds => ["MY001"];
				public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
			}
			""";

		var result = await ApplyCodeFixAsync(
			source,
			new CodeFixTestOptions
			{
				EquivalenceKey = MakeRoslynComponentPublicCodeFixProvider.EquivalenceKey,
				AdditionalAssemblyTypes = [typeof(CodeFixProvider)],
			},
			cancellationToken
		);

		await Assert.That(result).HasDiagnostic(NonPublicRoslynComponentAnalyzer.Rule.Id);
		await Assert.That(result.FixedSource).Contains("public sealed class MyFixer");
		await Assert.That(result.FixedSource).DoesNotContain("internal sealed class MyFixer");
	}
}
