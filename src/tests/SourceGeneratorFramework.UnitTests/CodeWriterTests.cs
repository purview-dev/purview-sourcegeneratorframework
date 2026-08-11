namespace Purview.SourceGeneratorFramework;

public class CodeWriterTests
{
	[Test]
	public async Task WriteLine_AppendsLineWithIndent()
	{
		var writer = new CodeWriter();

		writer.WriteLine("public class C");
		using (writer.Block())
		{
			writer.WriteLine("public int P { get; set; }");
		}

		var result = writer.ToString();

		await Assert.That(result).Contains("public class C");
		await Assert.That(result).Contains("\tpublic int P { get; set; }");
		await Assert.That(result).Contains("}");
	}

	[Test]
	public async Task Quote_WrapsValueInDoubleQuotes()
	{
		var writer = new CodeWriter();

		writer.Quote("value");

		await Assert.That(writer.ToString()).IsEqualTo("\"value\"");
	}

	[Test]
	public async Task ToString_EmptyWriter_ReturnsEmpty()
	{
		var writer = new CodeWriter();

		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task Append_AliasForWrite()
	{
		var writer = new CodeWriter();

		writer.Append("value");

		await Assert.That(writer.ToString()).IsEqualTo("value");
	}

	[Test]
	public async Task AppendLine_AliasForWriteLine()
	{
		var writer = new CodeWriter();

		writer.AppendLine("value");

		await Assert.That(writer.ToString()).Contains("value");
	}

	[Test]
	public async Task WriteIf_True_WritesValue()
	{
		var writer = new CodeWriter();

		writer.WriteIf(true, "value");

		await Assert.That(writer.ToString()).IsEqualTo("value");
	}

	[Test]
	public async Task WriteIf_False_DoesNotWrite()
	{
		var writer = new CodeWriter();

		writer.WriteIf(false, "value");

		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task WriteLineIf_True_WritesLine()
	{
		var writer = new CodeWriter();

		writer.WriteLineIf(true, "value");

		await Assert.That(writer.ToString()).Contains("value");
	}

	[Test]
	public async Task WriteLineIf_False_DoesNotWrite()
	{
		var writer = new CodeWriter();

		writer.WriteLineIf(false, "value");

		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task EnsureNewLine_WhenNotAtLineStart_AppendsNewLine()
	{
		var writer = new CodeWriter();

		writer.Write("value");
		writer.EnsureNewLine();

		await Assert.That(writer.ToString()).EndsWith("value\n");
	}

	[Test]
	public async Task WriteLines_WritesMultipleLines()
	{
		var writer = new CodeWriter();

		writer.WriteLines(["line1", "line2"]);

		await Assert.That(writer.ToString()).Contains("line1");
		await Assert.That(writer.ToString()).Contains("line2");
	}

	[Test]
	public async Task WriteDelimited_WritesItemsWithDelimiter()
	{
		var writer = new CodeWriter();

		writer.WriteDelimited(["a", "b", "c"], ", ");

		await Assert.That(writer.ToString()).IsEqualTo("a, b, c");
	}

	[Test]
	public async Task Block_WithBody_WritesBodyInsideBlock()
	{
		var writer = new CodeWriter();

		writer.Block("public class C", w => w.WriteLine("public int P { get; set; }"));

		var result = writer.ToString();

		await Assert.That(result).Contains("public class C");
		await Assert.That(result).Contains("\tpublic int P { get; set; }");
		await Assert.That(result).Contains("}");
	}

	[Test]
	public async Task WriteUsing_WritesUsingDirective()
	{
		var writer = new CodeWriter();

		writer.WriteUsing("System");

		await Assert.That(writer.ToString()).IsEqualTo("using System;\n");
	}

	[Test]
	public async Task WriteBlockNamespace_WritesNamespaceBlock()
	{
		var writer = new CodeWriter();

		using (writer.WriteBlockNamespace("Test"))
		{
			writer.WriteLine("public class C { }");
		}

		var result = writer.ToString();

		await Assert.That(result).Contains("namespace Test");
		await Assert.That(result).Contains("\tpublic class C { }");
		await Assert.That(result).Contains("}");
	}

	[Test]
	public async Task WriteClass_WritesClassBlock()
	{
		var writer = new CodeWriter();

		using (writer.WriteClass("public class C"))
		{
			writer.WriteLine("public int P { get; set; }");
		}

		var result = writer.ToString();

		await Assert.That(result).Contains("public class C");
		await Assert.That(result).Contains("\tpublic int P { get; set; }");
		await Assert.That(result).Contains("}");
	}

	[Test]
	public async Task WriteClass_WithOptions_WritesModifiersInheritanceAndConstraints()
	{
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("Repository")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			BaseType = "RepositoryBase<T>",
			Interfaces = ["IRepository<T>", "IDisposable"],
			GenericTypes =
			[
				new GenericTypeParameterOptions("T") { Constraints = ["class", "new()"] },
			],
		};

		using (writer.WriteClass(declaration))
		{
			writer.WriteLine("public T Value { get; } = new();");
		}

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"public sealed partial class Repository<T> : RepositoryBase<T>, IRepository<T>, IDisposable\n"
					+ "where T : class, new()\n"
					+ "{\n"
					+ "\tpublic T Value { get; } = new();\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteRecordStruct_WithOptions_WritesReadonlyRecordStruct()
	{
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("Identifier")
		{
			Accessibility = TypeDeclarationAccessibility.Internal,
			IsReadOnly = true,
			Interfaces = ["IEquatable<Identifier>"],
		};

		using (writer.WriteRecordStruct(declaration))
		{
			// Here to prevent IDE0555
		}

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"internal readonly partial record struct Identifier : IEquatable<Identifier>\n"
					+ "{\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteType_WithoutAccessibility_OmitsAccessibility()
	{
		var writer = new CodeWriter();

		using (
			writer.WriteType(
				new TypeDeclarationOptions("State")
				{
					Kind = TypeDeclarationKind.RecordClass,
					IsPartial = false,
					IsSealed = false,
				}
			)
		)
		{
			// Here to prevent IDE0555
		}

		await Assert.That(writer.ToString()).StartsWith("record class State\n");
	}

	[Test]
	public async Task WriteClass_GivenStaticDeclaration_WritesStaticClass()
	{
		// Arrange
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("Extensions")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			IsStatic = true,
		};

		// Act
		using (writer.WriteClass(declaration))
		{
			// Intentionally empty.
		}

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo("public static partial class Extensions\n{\n}\n");
	}

	[Test]
	public async Task WriteType_GivenStaticStruct_ThrowsArgumentException()
	{
		// Arrange
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("Invalid")
		{
			Kind = TypeDeclarationKind.Struct,
			IsStatic = true,
		};

		// Act
		CodeWriter.BlockScope Action() => writer.WriteType(declaration);

		// Assert
		await Assert.That(Action).Throws<ArgumentException>();
	}

	[Test]
	public async Task WriteStruct_WithBaseType_Throws()
	{
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("Invalid") { BaseType = "BaseType" };

		await Assert.That(() => writer.WriteStruct(declaration)).Throws<ArgumentException>();
	}

	[Test]
	public async Task WriteClass_WithPrimaryConstructor_WritesParametersBeforeBaseType()
	{
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("Repository")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			PrimaryConstructorParameters = ["string connectionString", "ILogger logger"],
			BaseType = "RepositoryBase(connectionString)",
		};

		using (writer.WriteClass(declaration))
		{
			// To stop IDE0055
		}

		await Assert
			.That(writer.ToString())
			.StartsWith(
				"public sealed partial class Repository(string connectionString, ILogger logger) : RepositoryBase(connectionString)\n"
			);
	}

	[Test]
	public async Task WriteConstructor_WritesParametersInitializerAndBody()
	{
		var writer = new CodeWriter();
		var declaration = new ConstructorDeclarationOptions("Repository")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			Parameters = ["string connectionString", "ILogger logger"],
			Initializer = "base(connectionString)",
		};

		using (writer.WriteConstructor(declaration))
		{
			writer.WriteLine("_logger = logger;");
		}

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"public Repository(string connectionString, ILogger logger) : base(connectionString)\n"
					+ "{\n"
					+ "\t_logger = logger;\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteConstructor_StaticConstructor_WritesStaticConstructor()
	{
		var writer = new CodeWriter();

		using (
			writer.WriteConstructor(
				new ConstructorDeclarationOptions("Repository") { IsStatic = true }
			)
		)
		{
			// To stop IDE0055
		}

		await Assert.That(writer.ToString()).IsEqualTo("static Repository()\n{\n}\n");
	}

	[Test]
	public async Task Block_WithBodyAndCustomSeparators_WritesDelimitedBody()
	{
		// Arrange
		var writer = new CodeWriter();

		// Act
		writer.Block(
			"Create",
			body =>
			{
				body.Quote("value").WriteLine(",");
				body.WriteLine("EmptyPath");
			},
			separator: "(",
			closingSeparator: ");"
		);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo("Create\n(\n\t\"value\",\n\tEmptyPath\n);\n");
	}

	[Test]
	public async Task Block_WithBodyLast_WritesBodyInsideCustomSeparators()
	{
		// Arrange
		var writer = new CodeWriter();

		// Act
		writer.Block("Create", "(", ");", body => body.Quote("value").WriteLine());

		// Assert
		await Assert.That(writer.ToString()).IsEqualTo("Create\n(\n\t\"value\"\n);\n");
	}

	[Test]
	public async Task ToString_GivenOpenBlockAndValidationEnabled_ThrowsScopeValidationException()
	{
		// Arrange
		var writer = new CodeWriter(throwOnUnclosedScopes: true);
		var scope = writer.Block("public sealed class Example");

		// Act
		string Action() => writer.ToString();

		// Assert
		await Assert.That(writer.OpenScopeCount).IsEqualTo(1);
		var exception = await Assert.That(Action).Throws<CodeWriterScopeValidationException>();
		await Assert.That(exception!.OpenScopeCount).IsEqualTo(1);
		await Assert.That(exception.OpenScopes[0].Kind).IsEqualTo("block");
		await Assert.That(exception.OpenScopes[0].Header).IsEqualTo("public sealed class Example");
		await Assert
			.That(exception.OpenScopes[0].OpeningStackTrace)
			.Contains(
				nameof(ToString_GivenOpenBlockAndValidationEnabled_ThrowsScopeValidationException)
			);
		await Assert.That(exception.Message).Contains("public sealed class Example");

		scope.Dispose();
		await Assert.That(writer.OpenScopeCount).IsEqualTo(0);
		await Assert.That(writer.ToString()).Contains("public sealed class Example");
	}

	[Test]
	public async Task ToString_GivenOpenBlockAndValidationDisabled_ReturnsPartialSource()
	{
		// Arrange
		var writer = new CodeWriter();
		var scope = writer.Block("public sealed class Example");

		// Act
		var source = writer.ToString();

		// Assert
		await Assert.That(writer.OpenScopeCount).IsEqualTo(1);
		await Assert.That(source).Contains("public sealed class Example");

		scope.Dispose();
	}

	[Test]
	public async Task ToString_GivenOpenIndentScopeAndValidationEnabled_ThrowsScopeValidationException()
	{
		// Arrange
		var writer = new CodeWriter(throwOnUnclosedScopes: true);
		var scope = writer.Indented();

		// Act
		string Action() => writer.ToString();

		// Assert
		await Assert.That(writer.OpenScopeCount).IsEqualTo(1);
		var exception = await Assert.That(Action).Throws<CodeWriterScopeValidationException>();
		await Assert.That(exception!.OpenScopeCount).IsEqualTo(1);
		await Assert.That(exception.OpenScopes[0].Kind).IsEqualTo("indentation");
		await Assert
			.That(exception.OpenScopes[0].OpeningStackTrace)
			.Contains(
				nameof(
					ToString_GivenOpenIndentScopeAndValidationEnabled_ThrowsScopeValidationException
				)
			);

		scope.Dispose();
		await Assert.That(writer.OpenScopeCount).IsEqualTo(0);
	}

	[Test]
	public async Task WriteAutoGeneratedHeader_WritesHeader()
	{
		var writer = new CodeWriter();

		writer.WriteAutoGeneratedHeader("TestGenerator", "1.0");

		var result = writer.ToString();

		await Assert.That(result).Contains("// <auto-generated />");
		await Assert.That(result).Contains("TestGenerator");
		await Assert.That(result).Contains("version 1.0");
	}

	[Test]
	public async Task WriteGeneratedCodeAttribute_WritesAttribute()
	{
		var writer = new CodeWriter();

		writer.WriteGeneratedCodeAttribute("TestGenerator", "1.0.0.0");

		var result = writer.ToString();

		await Assert
			.That(result)
			.Contains(
				"[global::System.CodeDom.Compiler.GeneratedCode(\"TestGenerator\", \"1.0.0.0\")]"
			);
	}
}
