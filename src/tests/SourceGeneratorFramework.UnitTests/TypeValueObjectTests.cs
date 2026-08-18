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
	public async Task TypeReferenceOptions_ComplexType_RendersAsCSharpSyntax()
	{
		var type = new TypeReferenceOptions("global::System.Collections.Generic.List")
			.MakeGeneric(new TypeReferenceOptions("global::ZodSharp.Core.ValidationError"))
			.MakeArray()
			.Nullable();

		string implicitValue = type;

		await Assert
			.That(implicitValue)
			.IsEqualTo("global::System.Collections.Generic.List<global::ZodSharp.Core.ValidationError>[]?");
		await Assert.That($"ref {type} errors").IsEqualTo($"ref {implicitValue} errors");
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
	public async Task DeclarationShapeHelpers_ReturnStructuredTypeReferences()
	{
		var type = new TypeValueObject("Widget", "Example");

		await Assert.That(type.AsTypeReference().Name).IsEqualTo("global::Example.Widget");
		await Assert.That(type.MakeNullable().IsNullable).IsTrue();
		await Assert.That(type.MakeArray(2).ArrayRanks).IsEquivalentTo([2]);
		await Assert.That(type.MakePointer().IsPointer).IsTrue();
	}

	[Test]
	public async Task StaticMember_ReturnsFullyQualifiedExpression()
	{
		var type = new TypeValueObject("Severity", "Example");

		var result = type.StaticMember("Inherit");

		await Assert.That(result).IsEqualTo("global::Example.Severity.Inherit");
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task StaticMember_GivenMissingName_Throws(string? memberName)
	{
		var type = new TypeValueObject("Severity", "Example");

		await Assert.That(() => type.StaticMember(memberName!)).Throws<ArgumentException>();
	}

	[Test]
	public async Task AttributeType_RendersAsTypeAndProvidesExplicitAttributeSyntax()
	{
		var type = new TypeValueObject("MyAttribute", "MyNamespace");

		await Assert.That(type.RenderFullName).IsEqualTo("global::MyNamespace.MyAttribute");
		await Assert.That(type.RenderTypeName).IsEqualTo("MyAttribute");
		await Assert.That(type.RenderAttributeName).IsEqualTo("[global::MyNamespace.My]");
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
			SourceGeneratorHelpers.ResolveReferences(options, typeof(TypeValueObjectTests).Assembly),
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
