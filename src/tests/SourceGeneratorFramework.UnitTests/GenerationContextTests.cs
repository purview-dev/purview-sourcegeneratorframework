using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.SourceGeneratorFramework.Models;

namespace Purview.SourceGeneratorFramework;

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

	sealed record TestContext(Compilation Compilation) : GenerationContext(Compilation, "TestGenerator", "1.0.0");

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
		var context = new GenerationContext(compilation, "TestGenerator", "1.0.0", validateCodeWriterScopes: true);

		// Act
		var writer = context.CreateCodeWriter();

		// Assert
		await Assert.That(context.CodeWriter.ThrowOnUnclosedScopes).IsTrue();
		await Assert.That(writer.ThrowOnUnclosedScopes).IsTrue();
		await Assert.That(ReferenceEquals(writer, context.CodeWriter)).IsTrue();
	}

	[Test]
	public async Task CreateCodeWriter_GivenGeneratorIdentity_PropagatesIdentity()
	{
		var context = new GenerationContext(CreateCompilation(), "HostKitGenerator", "2.3.4");

		var writer = context.CreateCodeWriter();

		await Assert.That(writer.GeneratorName).IsEqualTo("HostKitGenerator");
		await Assert.That(writer.GeneratorVersion).IsEqualTo("2.3.4");
	}

	[Test]
	public async Task ConfigureCodeWriterScopeValidation_GivenFactoryDefault_UpdatesOwnedWriter()
	{
		// Arrange
		var compilation = CreateCompilation();
		var context = new TestContext(compilation);

		// Act
		context.ConfigureCodeWriterScopeValidation(enabled: true);

		// Assert
		await Assert.That(context.ValidateCodeWriterScopes).IsTrue();
		await Assert.That(context.CodeWriter.ThrowOnUnclosedScopes).IsTrue();
	}

	[Test]
	public async Task Equality_IgnoresCompilationAndWriter()
	{
		var first = CreateCompilation();
		var second = CSharpCompilation.Create(
			"TestAssembly",
			[CSharpSyntaxTree.ParseText("class D { }")],
			references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]
		);

		var contextA = new GenerationContext(first, "TestGenerator", "1.0.0");
		var contextB = new GenerationContext(second, "TestGenerator", "1.0.0");

		await Assert.That(contextA).IsEqualTo(contextB);
		await Assert.That(contextA.GetHashCode()).IsEqualTo(contextB.GetHashCode());
	}

	[Test]
	public async Task Equality_DifferentiatesIdentityAndValidation()
	{
		var compilation = CreateCompilation();

		var contextA = new GenerationContext(compilation, "A", "1.0.0");
		var contextB = new GenerationContext(compilation, "B", "1.0.0");
		var contextC = new GenerationContext(compilation, "A", "1.0.0", validateCodeWriterScopes: true);

		await Assert.That(contextA).IsNotEqualTo(contextB);
		await Assert.That(contextA).IsNotEqualTo(contextC);
	}
}
