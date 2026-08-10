using Purview.SourceGeneratorFramework.Models;

namespace Purview.SourceGeneratorFramework;

public partial class TypeValueObjectTests
{
	[Test]
	public async Task Constructor_WithNamespace_RendersGlobalFullName()
	{
		var type = new TypeValueObject("MyType", "MyNamespace");

		await Assert.That(type.SymbolFullName).IsEqualTo("MyNamespace.MyType");
		await Assert.That(type.RenderFullName).IsEqualTo("global::MyNamespace.MyType");
	}

	[Test]
	public async Task Constructor_GlobalNamespace_RendersShortName()
	{
		var type = new TypeValueObject("MyType", null);

		await Assert.That(type.SymbolFullName).IsEqualTo("MyType");
		await Assert.That(type.RenderFullName).IsEqualTo("MyType");
	}

	[Test]
	public async Task MakeGeneric_ProducesExpectedTypeName()
	{
		var type = new TypeValueObject("MyType", "MyNamespace");

		var generic = type.MakeGeneric("string", "int");

		await Assert
			.That(generic.RenderFullName)
			.IsEqualTo("global::MyNamespace.MyType<string, int>");
	}

	[Test]
	public async Task MakeGenericXml_ProducesCurlyBracketTypeName()
	{
		var type = new TypeValueObject("MyType", "MyNamespace");

		var generic = type.MakeGenericXml("string", "int");

		await Assert
			.That(generic.RenderFullName)
			.IsEqualTo("global::MyNamespace.MyType{string, int}");
	}

	[Test]
	public async Task AttributeType_RendersAsAttribute()
	{
		var type = new TypeValueObject("MyAttribute", "MyNamespace");

		await Assert.That(type.RenderFullName).IsEqualTo("[global::MyNamespace.My]");
	}

	[Test]
	[MethodDataSource(nameof(SymbolTestData))]
	public async Task Constructor_GivenISymbol_PopulatesCorrectly(SymbolTestDataInfo testData)
	{
		// Act
		TypeValueObject sut = new(testData.Symbol);

		// Assert
		await Assert.That(sut.Namespace).IsEqualTo(testData.NamespaceInfo.Namespace);
		await Assert.That(sut.IsGlobalNamespace).IsEqualTo(!testData.NamespaceInfo.HasNamespace);
		await Assert.That(sut.TypeName).IsEqualTo(testData.TypeName);
	}
}
