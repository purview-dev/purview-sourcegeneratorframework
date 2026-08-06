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

	sealed record TestContext(Compilation Compilation, CodeWriter Writer)
		: GenerationContext(Compilation, Writer);

	[Test]
	public async Task GetTypeByMetadataName_KnownType_ReturnsSymbol()
	{
		var compilation = CreateCompilation();
		var context = new TestContext(compilation, new CodeWriter());

		var symbol = context.GetTypeByMetadataName("System.Object");

		await Assert.That(symbol).IsNotNull();
		await Assert.That(symbol!.Name).IsEqualTo("Object");
	}

	[Test]
	public async Task GetTypeByMetadataName_UnknownType_ReturnsNull()
	{
		var compilation = CreateCompilation();
		var context = new TestContext(compilation, new CodeWriter());

		var symbol = context.GetTypeByMetadataName("NonExistent.Type");

		await Assert.That(symbol).IsNull();
	}

	[Test]
	public async Task GetTypeByMetadataName_TypeValueObject_ReturnsSymbol()
	{
		var compilation = CreateCompilation();
		var context = new TestContext(compilation, new CodeWriter());
		var type = new TypeValueObject("Object", "System");

		var symbol = context.GetTypeByMetadataName(type);

		await Assert.That(symbol).IsNotNull();
		await Assert.That(symbol!.Name).IsEqualTo("Object");
	}
}
