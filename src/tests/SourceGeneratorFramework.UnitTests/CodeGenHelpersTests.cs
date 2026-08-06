using Purview.SourceGeneratorFramework.Helpers;

namespace Purview.SourceGeneratorFramework;

public class CodeGenHelpersTests
{
	[Test]
	public async Task WriteMethod_WritesMethodBlock()
	{
		var writer = new CodeWriter();

		writer.WriteMethod("public void M()", w => w.WriteLine("return;"));

		var result = writer.ToString();

		await Assert.That(result).Contains("public void M()");
		await Assert.That(result).Contains("\treturn;");
		await Assert.That(result).Contains("}");
	}

	[Test]
	public async Task WriteConstructor_WritesConstructorBlock()
	{
		var writer = new CodeWriter();

		writer.WriteConstructor("public C(int value)", w => w.WriteLine("_value = value;"));

		var result = writer.ToString();

		await Assert.That(result).Contains("public C(int value)");
		await Assert.That(result).Contains("\t_value = value;");
		await Assert.That(result).Contains("}");
	}

	[Test]
	public async Task WriteProperty_WithGetterAndSetter_WritesProperty()
	{
		var writer = new CodeWriter();

		writer.WriteProperty("public int P { get; set; }");

		var result = writer.ToString();

		await Assert.That(result).Contains("public int P { get; set; }");
	}

	[Test]
	public async Task WriteProperty_ReadOnly_WritesProperty()
	{
		var writer = new CodeWriter();

		writer.WriteProperty("public int P { get; }", "get;", setter: null);

		var result = writer.ToString();

		await Assert.That(result).Contains("public int P { get; }");
		await Assert.That(result).Contains("\tget;");
		await Assert.That(result).DoesNotContain("set;");
	}

	[Test]
	public async Task WriteField_WritesFieldDeclaration()
	{
		var writer = new CodeWriter();

		writer.WriteField("private readonly int _value");

		await Assert.That(writer.ToString()).IsEqualTo("private readonly int _value;\r\n");
	}

	[Test]
	public async Task WriteField_AlreadyHasSemicolon_DoesNotDuplicate()
	{
		var writer = new CodeWriter();

		writer.WriteField("private int _value;");

		await Assert.That(writer.ToString()).IsEqualTo("private int _value;\r\n");
	}

	[Test]
	public async Task WriteAttribute_NoArguments_WritesAttribute()
	{
		var writer = new CodeWriter();

		writer.WriteAttribute("Serializable");

		await Assert.That(writer.ToString()).IsEqualTo("[Serializable]\r\n");
	}

	[Test]
	public async Task WriteAttribute_WithArguments_WritesAttribute()
	{
		var writer = new CodeWriter();

		writer.WriteAttribute("Obsolete", "\"Use NewMethod()\"", "true");

		await Assert.That(writer.ToString()).IsEqualTo("[Obsolete(\"Use NewMethod()\", true)]\r\n");
	}
}
