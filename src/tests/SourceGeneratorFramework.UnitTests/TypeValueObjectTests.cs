using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.SourceGeneratorFramework.Models;

namespace Purview.SourceGeneratorFramework;

public partial class TypeValueObjectTests
{
	[Test]
	public async Task TypeReferenceOptions_WithEmpty_ReturnsEmptySentinel()
	{
		TypeReferenceOptions sut = TypeValueObject.Empty;

		await Assert.That(sut).IsEqualTo(TypeReferenceOptions.Empty);
		await Assert.That(sut.IsEmpty).IsTrue();
	}

	[Test]
	public async Task TypeReferenceOptions_WithNull_ReturnsNull()
	{
		TypeReferenceOptions? sut = (TypeValueObject?)null;

		await Assert.That(sut).IsNull();
	}

	[Test]
	public async Task Constructor_WithNamespace_RendersGlobalFullName()
	{
		var type = new TypeValueObject("MyType", "MyNamespace");

		await Assert.That(type.MetadataFullName).IsEqualTo("MyNamespace.MyType");
		await Assert.That(type.RenderFullName).IsEqualTo("global::MyNamespace.MyType");
	}

	[Test]
	public async Task Constructor_GlobalNamespace_RendersShortName()
	{
		var type = new TypeValueObject("MyType", null);

		await Assert.That(type.MetadataFullName).IsEqualTo("MyType");
		await Assert.That(type.RenderFullName).IsEqualTo("MyType");
	}

	[Test]
	public async Task MakeGeneric_ProducesExpectedTypeName()
	{
		var type = new TypeValueObject("MyType", "MyNamespace");

		var generic = type.MakeGeneric("string", "int");

		await Assert.That(generic.RenderFullName).IsEqualTo("global::MyNamespace.MyType<string, int>");
	}

	[Test]
	public async Task MakeGenericXml_ProducesCurlyBracketTypeName()
	{
		var type = new TypeValueObject("MyType", "MyNamespace");

		var generic = type.MakeGenericXml("string", "int");

		await Assert.That(generic.RenderFullName).IsEqualTo("global::MyNamespace.MyType{string, int}");
	}

	[Test]
	public async Task AttributeType_RendersAsAttribute()
	{
		var type = new TypeValueObject("MyAttribute", "MyNamespace");

		await Assert.That(type.RenderFullName).IsEqualTo("[global::MyNamespace.My]");
	}

	[Test]
	public async Task Constructor_GivenOpenGenericReflectionType_PreservesDefinition()
	{
		// Arrange and Act
		TypeValueObject type = new(typeof(List<>));

		// Assert
		await Assert.That(type.TypeName).IsEqualTo("List");
		await Assert.That(type.GenericArity).IsEqualTo(1);
		await Assert.That(type.IsGenericTypeDefinition).IsTrue();
		await Assert.That(type.MetadataFullName).IsEqualTo("System.Collections.Generic.List`1");
		await Assert.That(type.RenderFullName).IsEqualTo("global::System.Collections.Generic.List<>");
	}

	[Test]
	public async Task Constructor_GivenClosedGenericReflectionType_PreservesArguments()
	{
		// Arrange and Act
		TypeValueObject type = new(typeof(Dictionary<string, List<int>>));

		// Assert
		await Assert.That(type.GenericArity).IsEqualTo(2);
		await Assert.That(type.IsGenericTypeDefinition).IsFalse();
		await Assert.That(type.TypeArguments).Count().IsEqualTo(2);
		await Assert
			.That(type.RenderFullName)
			.IsEqualTo(
				"global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<int>>"
			);
	}

	[Test]
	public async Task Constructor_GivenClosedGenericSymbol_PreservesArguments(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using System.Collections.Generic;
			public sealed class Holder
			{
				public Dictionary<string, List<int>> Value = new();
			}
			""";
		SourceGeneratorTestOptions options = new();
		var syntax = CSharpSyntaxTree.ParseText(
			source,
			options: new CSharpParseOptions(LanguageVersion.Preview),
			cancellationToken: cancellationToken
		);
		var compilation = SourceGeneratorHelpers.CreateCompilation(
			[syntax],
			SourceGeneratorHelpers.ResolveReferences(options),
			options
		);
		var holder = compilation.GetTypeByMetadataName("Holder");
		var field = holder?.GetMembers("Value").OfType<IFieldSymbol>().Single();

		// Act
		TypeValueObject type = new(field!.Type);

		// Assert
		await Assert.That(type.TypeName).IsEqualTo("Dictionary");
		await Assert.That(type.GenericArity).IsEqualTo(2);
		await Assert.That(type.TypeArguments).Count().IsEqualTo(2);
		await Assert
			.That(type.RenderFullName)
			.IsEqualTo(
				"global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<int>>"
			);
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
		await Assert.That(sut.Keyword).IsNull();
		await Assert.That(sut.SpecialType).IsEqualTo(SpecialType.None);
	}

	[Test]
	public async Task Constructor_GivenKnownLangTypeSpecialType_PopulatesCorrectly()
	{
		// Arrange/ Act
		TypeValueObject sut = new(SpecialType.System_String);

		// Assert
		await Assert.That(sut.Keyword).IsEqualTo("string");
		await Assert.That(sut.SpecialType).IsEqualTo(SpecialType.System_String);
		await Assert.That(sut.TypeName).IsEqualTo("String");
		await Assert.That(sut.Namespace).IsEqualTo("System");
		await Assert.That(sut.IsGlobalNamespace).IsFalse();
		await Assert.That(sut.RenderFullName).IsEqualTo("string");
		await Assert.That(sut.RenderTypeName).IsEqualTo("string");
		await Assert.That(sut.MetadataFullName).IsEqualTo("System.String");
	}

	[Test]
	public async Task Constructor_GivenKnownLangTypeSystemType_PopulatesCorrectly()
	{
		// Arrange/ Act
		TypeValueObject sut = new(typeof(string));

		// Assert
		await Assert.That(sut.Keyword).IsEqualTo("string");
		await Assert.That(sut.SpecialType).IsEqualTo(SpecialType.System_String);
		await Assert.That(sut.TypeName).IsEqualTo("String");
		await Assert.That(sut.Namespace).IsEqualTo("System");
		await Assert.That(sut.IsGlobalNamespace).IsFalse();
		await Assert.That(sut.RenderFullName).IsEqualTo("string");
		await Assert.That(sut.RenderTypeName).IsEqualTo("string");
		await Assert.That(sut.MetadataFullName).IsEqualTo("System.String");
	}

	[Test]
	public async Task Constructor_GivenKnownLangTypeITypeSymbol_PopulatesCorrectly()
	{
		// Arrange/ Act
		var symbol = ITypeSymbol.Mock();
		symbol.SpecialType.Returns(SpecialType.System_String);

		TypeValueObject sut = new(symbol);

		// Assert
		await Assert.That(sut.Keyword).IsEqualTo("string");
		await Assert.That(sut.SpecialType).IsEqualTo(SpecialType.System_String);
		await Assert.That(sut.TypeName).IsEqualTo("String");
		await Assert.That(sut.Namespace).IsEqualTo("System");
		await Assert.That(sut.IsGlobalNamespace).IsFalse();
		await Assert.That(sut.RenderFullName).IsEqualTo("string");
		await Assert.That(sut.RenderTypeName).IsEqualTo("string");
		await Assert.That(sut.MetadataFullName).IsEqualTo("System.String");
	}
}
