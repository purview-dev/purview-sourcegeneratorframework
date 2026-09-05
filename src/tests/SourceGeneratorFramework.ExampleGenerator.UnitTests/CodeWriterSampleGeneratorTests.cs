using Purview.SourceGeneratorFramework.Examples;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;
using Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

public class CodeWriterSampleGeneratorTests
	: TUnitSourceGeneratorTestBase<CodeWriterSampleGenerator, CodeWriterSampleTestOptions>
{
	[Test]
	public async Task GenerateSample_GeneratesDemonstrativeClass(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			[GenerateCodeWriterSample]
			public class SampleTarget { }
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		await Assert.That(result).HasGeneratedClass("SampleTargetCodeWriterSample");
		await Assert.That(result).HasGeneratedField("_value");
		await Assert.That(result).HasGeneratedProperty("Value");
		await Assert.That(result).HasGeneratedProperty("DefaultAccessibility");
		await Assert.That(result).HasGeneratedMethod("Describe");
		await Assert.That(result).HasGeneratedMethod("Format");
		await Assert.That(result).HasGeneratedMethod("Categorize");
		await Assert.That(result).HasGeneratedMethod("Configure");

		var defaultAccessibility = await Assert.That(result).HasGeneratedProperty("DefaultAccessibility");
		await Assert.That(defaultAccessibility.Modifiers.ToString()).IsEqualTo("public");

		var classText = (
			await result.Generated().GetSyntaxTree("SampleTarget.CodeWriterSample.g.cs").GetTextAsync(cancellationToken)
		).ToString();
		await Assert.That(classText).Contains("#if NET\n\t// This member is emitted only for .NET targets.\n#endif");
		await Assert.That(classText).Contains("#pragma warning disable CS8625");
		await Assert.That(classText).Contains("#pragma warning disable CS0618");
		await Assert.That(classText).Contains("#pragma warning restore CS0618");
	}

	[Test]
	public async Task GenerateSample_UsesStructuredStatements(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			[GenerateCodeWriterSample]
			public class SampleTarget { }
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		var describe = await Assert.That(result).HasGeneratedMethod("Describe");
		var describeText = describe.ToString();
		await Assert.That(describeText).Contains("global::System.Console.WriteLine(\"Describe\");");
		await Assert.That(describeText).Contains("return value.ToString();");

		var constructor = result.Generated().GetConstructor("SampleTargetCodeWriterSample");
		await Assert.That(constructor.ToString()).Contains("_value = value;");
	}

	[Test]
	public async Task GenerateSample_EmitsChainedInvocationAndNullConditional(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			[GenerateCodeWriterSample]
			public class SampleTarget { }
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		var configure = await Assert.That(result).HasGeneratedMethod("Configure");
		var configureText = configure.ToString();
		await Assert.That(configureText).Contains("var hostKitOptions = source.Trim().ToUpper() ?? string.Empty;");
		await Assert.That(configureText).Contains("onBuilt?.Invoke();");
	}

	[Test]
	public async Task GenerateSample_EmitsConditionalBranches(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			[GenerateCodeWriterSample]
			public class SampleTarget { }
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		var categorize = await Assert.That(result).HasGeneratedMethod("Categorize");
		var categorizeText = categorize.ToString();
		await Assert.That(categorizeText).Contains("if (value < 0)\n\t\t{\n\t\t\treturn \"negative\";\n\t\t}");
		await Assert.That(categorizeText).Contains("else if (value == 0)");
		await Assert.That(categorizeText).Contains("return \"zero\";");
		await Assert.That(categorizeText).Contains("return \"positive\";");
	}

	[Test]
	public async Task GenerateSample_EmitsNetConditionalReturn(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			[GenerateCodeWriterSample]
			public class SampleTarget { }
			""";

		// Act
		var result = await GenerateAsync(source, cancellationToken);

		// Assert
		var format = await Assert.That(result).HasGeneratedMethod("Format");
		var formatText = format.ToString();
		await Assert.That(formatText).Contains("#if NET");
		await Assert
			.That(formatText)
			.Contains(
				"return string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $\"Value: {_value}\");"
			);
		await Assert.That(formatText).Contains("#else");
		await Assert
			.That(formatText)
			.Contains("return global::System.FormattableString.Invariant($\"Value: {_value}\");");
		await Assert.That(formatText).Contains("#endif");
	}
}
