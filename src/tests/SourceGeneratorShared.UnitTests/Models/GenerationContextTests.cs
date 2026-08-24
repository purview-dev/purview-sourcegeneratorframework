using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.Models;

public class GenerationContextTests
{
	static CSharpCompilation CreateCompilation()
	{
		var syntaxTree = CSharpSyntaxTree.ParseText("class C { }");
		return CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]
		);
	}

	sealed class TestContext(Compilation compilation)
		: GenerationContext(compilation, new GenerationSettings("TestGenerator", "1.0.0"));

	sealed class TestLogger : ISourceGenLogger
	{
		public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) { }
	}

	[Test]
	public async Task GetTypeByMetadataName_KnownType_ReturnsSymbol()
	{
		var compilation = CreateCompilation();
		var context = new TestContext(compilation);

		var symbol = context.GetTypeByMetadataName("System.Object");

		await Assert.That(symbol).IsNotNull();
		await Assert.That(symbol!.Name).IsEqualTo("Object");
	}

	[Test]
	public async Task GetTypeByMetadataName_UnknownType_ReturnsNull()
	{
		var compilation = CreateCompilation();
		var context = new TestContext(compilation);

		var symbol = context.GetTypeByMetadataName("NonExistent.Type");

		await Assert.That(symbol).IsNull();
	}

	[Test]
	public async Task GetTypeByMetadataName_TypeValueObject_ReturnsSymbol()
	{
		var compilation = CreateCompilation();
		var context = new TestContext(compilation);
		var type = new TypeValueObject("Object", "System");

		var symbol = context.GetTypeByMetadataName(type);

		await Assert.That(symbol).IsNotNull();
		await Assert.That(symbol!.Name).IsEqualTo("Object");
	}

	[Test]
	public async Task CreateCodeWriter_GivenScopeValidationEnabled_ConfiguresEveryWriter()
	{
		// Arrange
		var compilation = CreateCompilation();
		var context = new GenerationContext(
			compilation,
			new GenerationSettings("TestGenerator", "1.0.0", validateCodeWriterScopes: true)
		);

		// Act
		var writer = context.CreateCodeWriter();

		// Assert
		await Assert.That(writer.ThrowOnUnclosedScopes).IsTrue();
		await Assert.That(ReferenceEquals(writer, context.CreateCodeWriter())).IsFalse();
	}

	[Test]
	public async Task CreateCodeWriter_GivenGeneratorIdentity_PropagatesIdentity()
	{
		var context = new GenerationContext(CreateCompilation(), new GenerationSettings("HostKitGenerator", "2.3.4"));

		var writer = context.CreateCodeWriter();

		await Assert.That(writer.GeneratorName).IsEqualTo("HostKitGenerator");
		await Assert.That(writer.GeneratorVersion).IsEqualTo("2.3.4");
	}

	[Test]
	public async Task Constructor_GivenLogger_ExposesLogger()
	{
		// Arrange
		var logger = new TestLogger();
		var context = new GenerationContext(
			CreateCompilation(),
			new GenerationSettings("TestGenerator", "1.0.0"),
			logger
		);

		// Act / Assert
		await Assert.That(context.Logger).IsSameReferenceAs(logger);
	}

	[Test]
	public async Task Contexts_GivenDifferentCompilations_AreDistinctReferences()
	{
		var first = CreateCompilation();
		var second = CSharpCompilation.Create(
			"TestAssembly",
			[CSharpSyntaxTree.ParseText("class D { }")],
			references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]
		);

		var settings = new GenerationSettings("TestGenerator", "1.0.0");
		var contextA = new GenerationContext(first, settings);
		var contextB = new GenerationContext(second, settings);

		await Assert.That(contextA).IsNotEqualTo(contextB);
	}

	[Test]
	public async Task GenerationSettings_UsesValueEquality()
	{
		var settingsA = new GenerationSettings("A", "1.0.0");
		var settingsB = new GenerationSettings("A", "1.0.0");
		var settingsC = settingsA with { ValidateCodeWriterScopes = true };

		await Assert.That(settingsA).IsEqualTo(settingsB);
		await Assert.That(settingsA).IsNotEqualTo(settingsC);
	}
}
