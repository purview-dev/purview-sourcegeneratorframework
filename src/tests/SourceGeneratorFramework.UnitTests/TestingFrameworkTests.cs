using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework;

public class TestingFrameworkTests
{
	sealed record CustomSourceGeneratorTestOptions : SourceGeneratorTestOptions
	{
		public string CustomValue { get; init; } = "custom";
	}

	const string GenerateAttributeSource = """
		namespace Test
		{
			[System.AttributeUsage(System.AttributeTargets.Class)]
			public sealed class GenerateAttribute : System.Attribute { }
		}
		""";

	internal sealed class TestGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
				"Test.GenerateAttribute",
				(node, _) => node is ClassDeclarationSyntax,
				(ctx, _) => ctx.TargetSymbol.Name
			);

			context.RegisterSourceOutput(
				provider,
				static (spc, name) =>
					spc.AddSource(
						$"{name}.g.cs",
						$@"
namespace Test
{{
	public static class Generated_{name}
	{{
		public const string Name = ""{name}"";
	}}
}}
"
					)
			);
		}
	}

	internal sealed class InvalidSourceGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			context.RegisterPostInitializationOutput(static output =>
				output.AddSource(
					"Broken.g.cs",
					"""
					namespace Generated;
					public sealed class Broken
					{
						public MissingType Value { get; }
					}
					"""
				)
			);
		}
	}

	internal sealed class OptionsGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var validationValue = context.AnalyzerConfigOptionsProvider.Select(
				static (options, _) =>
				{
					options.GlobalOptions.TryGetValue(
						"build_property.PurviewSourceGeneratorFrameworkValidateCodeWriterScopes",
						out var value
					);
					return value;
				}
			);
			var customValue = context.AnalyzerConfigOptionsProvider.Select(
				static (options, _) =>
				{
					options.GlobalOptions.TryGetValue("build_property.CustomOption", out var value);
					return value;
				}
			);

			context.RegisterSourceOutput(
				validationValue,
				static (output, value) =>
					output.AddSource(
						"ScopeValidation.g.cs",
						$"internal static class ScopeValidation {{ internal const string Value = \"{value}\"; }}"
					)
			);
			context.RegisterSourceOutput(
				customValue,
				static (output, value) =>
					output.AddSource(
						"CustomOption.g.cs",
						$"internal static class CustomOption {{ internal const string Value = \"{value}\"; }}"
					)
			);
		}
	}

	[Test]
	public async Task RunAsync_MultipleSources_GeneratesForBoth()
	{
		var source1 =
			"""
				using Test;
				[Generate]
				public class A { }
				"""
			+ "\n"
			+ GenerateAttributeSource;
		var source2 =
			"""
				using Test;
				[Generate]
				public class B { }
				"""
			+ "\n"
			+ GenerateAttributeSource;

		var runner = new SourceGeneratorTestRunner<TestGenerator>();
		var result = await runner.RunAsync([source1, source2]);

		result.AssertGeneratedSourceCount(2);
	}

	[Test]
	public async Task AssertSingleGeneratedSource_ReturnsGeneratedSource()
	{
		var source =
			"""
				using Test;
				[Generate]
				public class A { }
				"""
			+ "\n"
			+ GenerateAttributeSource;

		var runner = new SourceGeneratorTestRunner<TestGenerator>();
		var result = await runner.RunAsync(source);

		var generated = result.AssertSingleGeneratedSource();

		await Assert.That(generated).Contains("public static class Generated_A");
	}

	[Test]
	public async Task AssertGeneratedSourceContains_MatchesGeneratedText()
	{
		var source =
			"""
				using Test;
				[Generate]
				public class A { }
				"""
			+ "\n"
			+ GenerateAttributeSource;

		var runner = new SourceGeneratorTestRunner<TestGenerator>();
		var result = await runner.RunAsync(source);

		result.AssertGeneratedSourceContains("public static class Generated_A");
	}

	[Test]
	public async Task AssertNoCompilationErrors_PassingRun_DoesNotThrow()
	{
		var source =
			"""
				using Test;
				[Generate]
				public class A { }
				"""
			+ "\n"
			+ GenerateAttributeSource;

		var runner = new SourceGeneratorTestRunner<TestGenerator>();
		var result = await runner.RunAsync(source);

		result.AssertNoCompilationErrors();
	}

	[Test]
	public async Task EnsureValid_GivenGeneratedCompilationError_ReportsSourceContext(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var runner = new SourceGeneratorTestRunner<InvalidSourceGenerator>();
		var result = await runner.RunAsync("public sealed class Input { }", cancellationToken: cancellationToken);
		DriverRunValidationException? exception = null;

		// Act
		try
		{
			result.EnsureValid();
		}
		catch (DriverRunValidationException caught)
		{
			exception = caught;
		}

		// Assert
		await Assert.That(exception).IsNotNull();
		await Assert.That(exception!.CompilationErrors).IsNotEmpty();
		await Assert.That(exception.Message).Contains("Broken.g.cs (generated)");
		await Assert.That(exception.Message).Contains("CS0246");
		await Assert.That(exception.Message).Contains("public MissingType Value");
		await Assert.That(exception.Message).Contains("^");
	}

	[Test]
	public async Task RunAsync_DefaultOptions_PassesEnabledCodeWriterScopeValidationProperty(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var runner = new SourceGeneratorTestRunner<OptionsGenerator>();
		var options = new SourceGeneratorTestOptions();

		// Act
		var result = await runner.RunAsync("public sealed class Input { }", options, cancellationToken);

		// Assert
		await Assert.That(options.ValidateCodeWriterScopes).IsTrue();
		await Assert.That(result.GetSource()).Contains("Value = \"true\"");
	}

	[Test]
	public async Task RunAsync_UnprefixedAnalyzerOption_IsAlsoExposedAsBuildProperty(
		CancellationToken cancellationToken
	)
	{
		var runner = new SourceGeneratorTestRunner<OptionsGenerator>();
		var options = new SourceGeneratorTestOptions { AnalyzerConfigOptions = { ["CustomOption"] = "enabled" } };

		var result = await runner.RunAsync("public sealed class Input { }", options, cancellationToken);

		var tree = result.GetGeneratedTree("CustomOption.g.cs");
		await Assert.That(tree).IsNotNull();
		await Assert.That((await tree!.GetTextAsync(cancellationToken)).ToString()).Contains("Value = \"enabled\"");
	}

	[Test]
	public async Task RunAsync_ReferencesGeneratorAssemblyThatContainsPublicContracts()
	{
		var runner = new SourceGeneratorTestRunner<TestGenerator>();

		var result = await runner.RunAsync("public sealed class Input { }");

		await Assert
			.That(
				result.CompilationResult.Compilation.References.Any(reference =>
					string.Equals(
						reference.Display,
						typeof(TestGenerator).Assembly.Location,
						StringComparison.OrdinalIgnoreCase
					)
				)
			)
			.IsTrue();
	}

	[Test]
	[NotInParallel]
	public async Task Constructor_DerivedOptions_CopiesConfiguredDefaultWithoutSharingMutableCollections()
	{
		var originalDefault = SourceGeneratorTestOptions.Default;
		try
		{
			SourceGeneratorTestOptions.Default = originalDefault with
			{
				AnalyzerConfigOptions = new Dictionary<string, string> { ["Shared"] = "default" },
			};

			var first = new CustomSourceGeneratorTestOptions();
			var second = new CustomSourceGeneratorTestOptions();
			first.AnalyzerConfigOptions["OnlyFirst"] = "value";

			await Assert.That(first.AnalyzerConfigOptions["Shared"]).IsEqualTo("default");
			await Assert.That(second.AnalyzerConfigOptions.ContainsKey("OnlyFirst")).IsFalse();
			await Assert.That(first.CustomValue).IsEqualTo("custom");
		}
		finally
		{
			SourceGeneratorTestOptions.Default = originalDefault;
		}
	}
}
