using Purview.SourceGeneratorFramework.Models;

namespace Purview.SourceGeneratorFramework;

public class XmlCommentWriterTests
{
	[Test]
	public async Task XmlComment_WritesEachCommentLine()
	{
		var writer = CodeWriterFactory.ForTests();

		var result = writer.XmlComment("first", "second");

		await Assert.That(result).IsSameReferenceAs(writer);
		await Assert.That(writer.ToString()).IsEqualTo("/// first\n/// second\n");
	}

	[Test]
	public async Task Xml_SingleLine_WritesOpeningContentAndClosingTags()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.Xml("remarks", "content");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <remarks>\n/// content\n/// </remarks>\n");
	}

	[Test]
	public async Task Xml_MultipleLines_PutsContentOnSeparateLines()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.Xml("remarks", "first", "second");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <remarks>\n/// first\n/// second\n/// </remarks>\n");
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task Xml_MissingTag_Throws(string? tag)
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert.That(() => writer.Xml(tag!, "content")).Throws<ArgumentException>();
	}

	[Test]
	public async Task XmlSummary_WritesSummaryBlock()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlSummary("first", "second");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <summary>\n/// first\n/// second\n/// </summary>\n");
	}

	[Test]
	public async Task XmlReturn_WritesReturnsBlock()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlReturn("value");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <returns>\n/// value\n/// </returns>\n");
	}

	[Test]
	public async Task XmlCref_String_WritesCrefAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlCref("MyType", "type description");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <cref cref=\"MyType\">\n/// type description\n/// </cref>\n");
	}

	[Test]
	public async Task XmlCref_TypeValueObject_WritesRenderedCrefAndMultipleContentLines()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlCref(new TypeValueObject(typeof(string)), "first", "second");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <cref cref=\"string\">\n/// first\n/// second\n/// </cref>\n");
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task XmlCref_String_MissingTypeName_Throws(string? typeName)
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert.That(() => writer.XmlCref(typeName!, "content")).Throws<ArgumentException>();
	}

	[Test]
	public async Task XmlException_String_WritesCrefAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlException("InvalidOperationException", "reason");

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <exception cref=\"InvalidOperationException\">\n/// reason\n/// </exception>\n"
			);
	}

	[Test]
	public async Task XmlException_TypeValueObject_WritesRenderedCref()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlException(new TypeValueObject(typeof(ArgumentException)), "reason");

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <exception cref=\"global::System.ArgumentException\">\n/// reason\n/// </exception>\n"
			);
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task XmlException_String_MissingType_Throws(string? exceptionType)
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert
			.That(() => writer.XmlException(exceptionType!, "content"))
			.Throws<ArgumentException>();
	}

	[Test]
	[Arguments("c", "/// <c>\n/// code\n/// </c>\n")]
	[Arguments("example", "/// <example>\n/// sample\n/// </example>\n")]
	[Arguments("para", "/// <para>\n/// paragraph\n/// </para>\n")]
	public async Task SimpleXmlHelpers_WriteExpectedTag(string tag, string expected)
	{
		var writer = CodeWriterFactory.ForTests();

		switch (tag)
		{
			case "c":
				writer.XmlCode("code");
				break;
			case "example":
				writer.XmlExample("sample");
				break;
			default:
				writer.XmlPara("paragraph");
				break;
		}

		await Assert.That(writer.ToString()).IsEqualTo(expected);
	}

	[Test]
	public async Task XmlParam_WritesNameAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlParam("value", "description");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <param name=\"value\">\n/// description\n/// </param>\n");
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task XmlParam_MissingName_Throws(string? parameterName)
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert
			.That(() => writer.XmlParam(parameterName!, "content"))
			.Throws<ArgumentException>();
	}

	[Test]
	public async Task XmlList_DefaultsToBulletItems()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlList(["first", "second"]);

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <list type=\"bullet\">\n"
					+ "/// <item>first</item>\n"
					+ "/// <item>second</item>\n"
					+ "/// </list>\n"
			);
	}

	[Test]
	public async Task XmlList_WithDescription_WritesDescriptionBeforeItems()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlList("bullet", "A description", "item");

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"/// <list type=\"bullet\">\n"
					+ "/// <description>\n"
					+ "/// A description\n"
					+ "/// </description>\n"
					+ "/// <item>item</item>\n"
					+ "/// </list>\n"
			);
	}

	[Test]
	public async Task XmlList_CustomType_WritesTypeAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlList("number", null, "item");

		await Assert
			.That(writer.ToString())
			.IsEqualTo("/// <list type=\"number\">\n/// <item>item</item>\n/// </list>\n");
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task XmlList_MissingType_Throws(string? listType)
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert
			.That(() => writer.XmlList(listType!, null, "item"))
			.Throws<ArgumentException>();
	}

	[Test]
	public async Task XmlHelpers_WithNoContent_DoNothing()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.XmlSummary([]);
		writer.XmlCode([]);
		writer.XmlList([]);

		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task BuildXmlTag_WithoutAttributes_WritesTag()
	{
		await Assert.That(XmlCommentWriter.BuildXmlTag("summary")).IsEqualTo("<summary>");
	}

	[Test]
	public async Task BuildXmlTag_WithAttributes_WritesValuesAndLowercaseBooleans()
	{
		var result = XmlCommentWriter.BuildXmlTag(
			"list",
			("type", "bullet"),
			("enabled", true),
			("count", 2)
		);

		await Assert.That(result).IsEqualTo("<list type=\"bullet\" enabled=\"true\" count=\"2\">");
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task BuildXmlTag_MissingTag_Throws(string? tag)
	{
		await Assert.That(() => XmlCommentWriter.BuildXmlTag(tag!)).Throws<ArgumentException>();
	}
}
