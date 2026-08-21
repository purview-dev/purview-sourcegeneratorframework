using Purview.SourceGeneratorFramework.Models;

namespace Purview.SourceGeneratorFramework;

public class CodeWriterTests
{
	static TypeReferenceOptions Type(string name) => new(new TypeValueObject(name, null));

	static string GeneratedAttributes(bool includeCoverageExclusion = true) =>
		(includeCoverageExclusion ? "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\n" : "")
		+ "[global::System.Runtime.CompilerServices.CompilerGenerated]\n"
		+ "[global::System.CodeDom.Compiler.GeneratedCode(\"TestGenerator\", \"1.0.0\")]\n";

	static string IndentedGeneratedAttributes(bool includeCoverageExclusion = true) =>
		"\t"
		+ GeneratedAttributes(includeCoverageExclusion).Replace("\n", "\n\t", StringComparison.Ordinal).TrimEnd('\t');

	[Test]
	public async Task MemberDeclarationOptions_AreValueTypes()
	{
		// Arrange / Act / Assert
		await Assert.That(typeof(MethodDeclarationOptions).IsValueType).IsTrue();
		await Assert.That(typeof(PropertyDeclarationOptions).IsValueType).IsTrue();
		await Assert.That(typeof(FieldDeclarationOptions).IsValueType).IsTrue();
		await Assert.That(typeof(EnumFieldDeclarationOptions).IsValueType).IsTrue();
		await Assert.That(typeof(ConstructorDeclarationOptions).IsValueType).IsTrue();
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task TypeReferenceOptions_GivenMissingName_Throws(string? name)
	{
		await Assert.That(() => new TypeReferenceOptions(new TypeValueObject(name!, null))).Throws<ArgumentException>();
	}

	[Test]
	public async Task EmptyTypeReference_IsIgnoredByMemberEmitters()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteField(new FieldDeclarationOptions("field", TypeReferenceOptions.Empty));
		writer.WriteProperty(new PropertyDeclarationOptions("Property", TypeReferenceOptions.Empty));
		writer.WriteMethodScope(new MethodDeclarationOptions("Method", TypeReferenceOptions.Empty)).Dispose();

		// Assert
		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task WriteLine_AppendsLineWithIndent()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteLine("public class C");
		using (writer.OpenBlockScope())
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
		var writer = CodeWriterFactory.ForTests();

		writer.Quote("value");

		await Assert.That(writer.ToString()).IsEqualTo("\"value\"");
	}

	[Test]
	public async Task ToString_EmptyWriter_ReturnsEmpty()
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task Append_AliasForWrite()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.Append("value");

		await Assert.That(writer.ToString()).IsEqualTo("value");
	}

	[Test]
	public async Task AppendLine_AliasForWriteLine()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.AppendLine("value");

		await Assert.That(writer.ToString()).Contains("value");
	}

	[Test]
	public async Task WriteIf_True_WritesValue()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteIf(true, "value");

		await Assert.That(writer.ToString()).IsEqualTo("value");
	}

	[Test]
	public async Task WriteIf_False_DoesNotWrite()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteIf(false, "value");

		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task WriteLineIf_True_WritesLine()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteLineIf(true, "value");

		await Assert.That(writer.ToString()).Contains("value");
	}

	[Test]
	public async Task WriteLineIf_False_DoesNotWrite()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteLineIf(false, "value");

		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task EnsureNewLine_WhenNotAtLineStart_AppendsNewLine()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.Write("value");
		writer.EnsureNewLine();

		await Assert.That(writer.ToString()).EndsWith("value\n");
	}

	[Test]
	public async Task EnsureBlankLine_AddsSeparatorAfterCompletedLine()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteMethodCall("Run").EnsureBlankLine().Comment("Explains the next member.");

		await Assert.That(writer.ToString()).IsEqualTo("Run();\n\n// Explains the next member.\n");
	}

	[Test]
	public async Task EnsureBlankLine_CompletesPartialLineAndIsIdempotent()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.Write("Run();").EnsureBlankLine().EnsureBlankLine().Write("Next");

		await Assert.That(writer.ToString()).IsEqualTo("Run();\n\nNext");
	}

	[Test]
	public async Task EnsureBlankLine_OnEmptyWriterDoesNotWrite()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.EnsureBlankLine();

		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task WriteLines_WritesMultipleLines()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteLines(["line1", "line2"]);

		await Assert.That(writer.ToString()).Contains("line1");
		await Assert.That(writer.ToString()).Contains("line2");
	}

	[Test]
	public async Task WriteDelimited_WritesItemsWithDelimiter()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteDelimited(["a", "b", "c"], ", ");

		await Assert.That(writer.ToString()).IsEqualTo("a, b, c");
	}

	[Test]
	public async Task Block_WithBody_WritesBodyInsideBlock()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteBlock("public class C", w => w.WriteLine("public int P { get; set; }"));

		var result = writer.ToString();

		await Assert.That(result).Contains("public class C");
		await Assert.That(result).Contains("\tpublic int P { get; set; }");
		await Assert.That(result).Contains("}");
	}

	[Test]
	public async Task WriteUsing_WritesUsingDirective()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteUsing("System");

		await Assert.That(writer.ToString()).IsEqualTo("using System;\n");
	}

	[Test]
	public async Task WriteBlockNamespace_WritesNamespaceBlock()
	{
		var writer = CodeWriterFactory.ForTests();

		using (writer.WriteBlockNamespaceScope("Test"))
		{
			writer.WriteLine("public class C { }");
		}

		var result = writer.ToString();

		await Assert.That(result).Contains("namespace Test");
		await Assert.That(result).Contains("\tpublic class C { }");
		await Assert.That(result).Contains("}");
	}

	[Test]
	public async Task WriteBlockNamespaces_GivenMultipleNamespaces_InsertsBlankLineBetweenThem()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteBlockNamespace("First", body => body.WriteLine("class A { }"));
		writer.WriteBlockNamespace("Second", body => body.WriteLine("class B { }"));

		await Assert
			.That(writer.ToString())
			.IsEqualTo("namespace First\n{\n\tclass A { }\n}\n\n" + "namespace Second\n{\n\tclass B { }\n}\n");
	}

	[Test]
	public async Task WriteBlockNamespaceAndTopLevelType_InsertsBlankLineBetweenDeclarations()
	{
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("TopLevel");

		writer.WriteBlockNamespace("First", body => body.WriteLine("class Nested { }"));
		writer.WriteClass(declaration, static _ => { });
		writer.WriteBlockNamespace("Second", body => body.WriteLine("class Other { }"));

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"namespace First\n{\n\tclass Nested { }\n}\n\n"
					+ GeneratedAttributes()
					+ "sealed partial class TopLevel\n{\n}\n\n"
					+ "namespace Second\n{\n\tclass Other { }\n}\n"
			);
	}

	[Test]
	public async Task WriteBlockNamespace_TypeValueObject_WritesNamespaceBlock()
	{
		var writer = CodeWriterFactory.ForTests();
		var typeValue = new TypeValueObject("C", "Test");

		using (writer.WriteBlockNamespaceScope(typeValue))
		{
			writer.WriteLine("public class C { }");
		}

		var result = writer.ToString();

		await Assert.That(result).Contains("namespace Test");
		await Assert.That(result).Contains("\tpublic class C { }");
		await Assert.That(result).Contains("}");
	}

	[Test]
	public async Task WriteBlockNamespace_TypeValueObjectWithGlobalNamespace_ReturnsNoOpScope()
	{
		var writer = CodeWriterFactory.ForTests();
		var typeValue = new TypeValueObject("C", null);

		using (var scope = writer.WriteBlockNamespaceScope(typeValue))
		{
			await Assert.That(scope).IsNull();
			writer.WriteLine("public class C { }");
		}

		var result = writer.ToString();

		await Assert.That(result).DoesNotContain("namespace");
		await Assert.That(result).Contains("public class C { }");
	}

	[Test]
	public async Task WriteFileScopedNamespace_TypeValueObject_WritesNamespace()
	{
		var writer = CodeWriterFactory.ForTests();
		var typeValue = new TypeValueObject("C", "Test");

		writer.WriteFileScopedNamespace(typeValue);

		var result = writer.ToString();

		await Assert.That(result).Contains("namespace Test;");
	}

	[Test]
	public async Task WriteFileScopedNamespace_TypeValueObjectWithGlobalNamespace_WritesNothing()
	{
		var writer = CodeWriterFactory.ForTests();
		var typeValue = new TypeValueObject("C", null);

		writer.WriteFileScopedNamespace(typeValue);

		var result = writer.ToString();

		await Assert.That(result).DoesNotContain("namespace");
	}

	[Test]
	public async Task WriteClass_WritesClassBlock()
	{
		var writer = CodeWriterFactory.ForTests();

		using (
			writer.WriteClassScope(
				new TypeDeclarationOptions("C")
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					IsPartial = false,
					IsSealed = false,
				}
			)
		)
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
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("Repository")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			BaseType = Type("RepositoryBase").Type.MakeGeneric(Type("T")),
			Interfaces = [Type("IRepository").Type.MakeGeneric(Type("T")), Type("IDisposable")],
			GenericTypes = [new GenericTypeParameterOptions("T") { Constraints = ["class", "new()"] }],
		};

		using (writer.WriteClassScope(declaration))
		{
			writer.WriteLine("public T Value { get; } = new();");
		}

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "public sealed partial class Repository<T> : RepositoryBase<T>, IRepository<T>, IDisposable\n"
					+ "where T : class, new()\n"
					+ "{\n"
					+ "\tpublic T Value { get; } = new();\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteRecordStruct_WithOptions_WritesReadonlyRecordStruct()
	{
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("Identifier")
		{
			Accessibility = TypeDeclarationAccessibility.Internal,
			IsReadOnly = true,
			Interfaces = [Type("IEquatable").Type.MakeGeneric(Type("Identifier"))],
		};

		using (writer.WriteRecordStructScope(declaration))
		{
			// Here to prevent IDE0555
		}

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "internal readonly partial record struct Identifier : IEquatable<Identifier>\n"
					+ "{\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteType_WithoutAccessibility_OmitsAccessibility()
	{
		var writer = CodeWriterFactory.ForTests();

		using (
			writer.WriteTypeScope(
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

		await Assert.That(writer.ToString()).StartsWith(GeneratedAttributes() + "record class State\n");
	}

	[Test]
	public async Task WriteClass_GivenStaticDeclaration_WritesStaticClass()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("Extensions")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			IsStatic = true,
		};

		// Act
		using (writer.WriteClassScope(declaration))
		{
			// Intentionally empty.
		}

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(GeneratedAttributes() + "public static partial class Extensions\n{\n}\n");
	}

	[Test]
	public async Task WriteClass_GivenAbstractDeclaration_WritesAbstractInsteadOfDefaultSealed()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("ServiceBase")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			IsAbstract = true,
		};

		// Act
		using (writer.WriteClassScope(declaration))
		{
			// Intentionally empty.
		}

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(GeneratedAttributes() + "public abstract partial class ServiceBase\n{\n}\n");
	}

	[Test]
	public async Task WriteStruct_GivenAbstractDeclaration_ThrowsArgumentException()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("Invalid") { IsAbstract = true };

		// Act
		CodeWriter.BlockScope Action() => writer.WriteStructScope(declaration);

		// Assert
		await Assert.That(Action).Throws<ArgumentException>();
	}

	[Test]
	public async Task WriteType_GivenStaticStruct_ThrowsArgumentException()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("Invalid") { Kind = TypeDeclarationKind.Struct, IsStatic = true };

		// Act
		CodeWriter.BlockScope Action() => writer.WriteTypeScope(declaration);

		// Assert
		await Assert.That(Action).Throws<ArgumentException>();
	}

	[Test]
	public async Task WriteStruct_WithBaseType_Throws()
	{
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("Invalid") { BaseType = Type("BaseType") };

		await Assert.That(() => writer.WriteStructScope(declaration)).Throws<ArgumentException>();
	}

	[Test]
	public async Task WriteClass_WithPrimaryConstructor_WritesParametersBeforeBaseType()
	{
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("Repository")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			PrimaryConstructorParameters = [new("connectionString", Type("string")), new("logger", Type("ILogger"))],
			BaseType = Type("RepositoryBase(connectionString)"),
		};

		using (writer.WriteClassScope(declaration))
		{
			// To stop IDE0055
		}

		await Assert
			.That(writer.ToString())
			.StartsWith(
				GeneratedAttributes()
					+ "public sealed partial class Repository(string connectionString, ILogger logger) : RepositoryBase(connectionString)\n"
			);
	}

	[Test]
	public async Task WriteClass_WithEmptyBaseType_DoesNotWriteBaseListColon()
	{
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("ResourceKit") { BaseType = TypeReferenceOptions.Empty };

		writer.WriteClass(declaration, static _ => { });

		await Assert
			.That(writer.ToString())
			.IsEqualTo(GeneratedAttributes() + "sealed partial class ResourceKit\n{\n}\n");
	}

	[Test]
	public async Task WriteClass_WithEmptyBaseAndInterfaces_WritesOnlyNonEmptyInterfaces()
	{
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("ResourceKit")
		{
			BaseType = TypeReferenceOptions.Empty,
			Interfaces =
			[
				TypeReferenceOptions.Empty,
				new TypeReferenceOptions(new TypeValueObject("IResourceKit", null)),
				TypeReferenceOptions.Empty,
			],
		};

		writer.WriteClass(declaration, static _ => { });

		await Assert
			.That(writer.ToString())
			.IsEqualTo(GeneratedAttributes() + "sealed partial class ResourceKit : IResourceKit\n{\n}\n");
	}

	[Test]
	public async Task WriteConstructor_WritesParametersInitializerAndBody()
	{
		var writer = CodeWriterFactory.ForTests();
		var declaration = new ConstructorDeclarationOptions("Repository")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			Parameters = [new("connectionString", Type("string")), new("logger", Type("ILogger"))],
			Initializer = "base(connectionString)",
		};

		using (writer.WriteConstructorScope(declaration))
		{
			writer.WriteLine("_logger = logger;");
		}

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "public Repository(string connectionString, ILogger logger)\n"
					+ "\t: base(connectionString)\n"
					+ "{\n"
					+ "\t_logger = logger;\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteConstructor_StaticConstructor_WritesStaticConstructor()
	{
		var writer = CodeWriterFactory.ForTests();

		using (writer.WriteConstructorScope(new ConstructorDeclarationOptions("Repository") { IsStatic = true }))
		{
			// To stop IDE0055
		}

		await Assert.That(writer.ToString()).IsEqualTo(GeneratedAttributes() + "static Repository()\n{\n}\n");
	}

	[Test]
	public async Task WriteMethod_GivenShortParameters_WritesSingleLineDeclaration()
	{
		var writer = CodeWriterFactory.ForTests();

		using (
			writer.WriteMethodScope(
				new MethodDeclarationOptions("Execute", Type("void"))
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					IsStatic = true,
					Parameters = [new("name", Type("string")), new("enabled", Type("bool"))],
				}
			)
		)
		{
			writer.WriteLine("Run(name, enabled);");
		}

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "public static void Execute(string name, bool enabled)\n"
					+ "{\n"
					+ "\tRun(name, enabled);\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteInterface_WithInheritanceAndConstraints_WritesInterface()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("IRepository")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			Interfaces = [Type("IAsyncDisposable")],
			GenericTypes = [new GenericTypeParameterOptions("T") { Constraints = ["class"] }],
		};

		// Act
		writer.WriteInterface(declaration, body => body.WriteLine("T Get();"));

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes(includeCoverageExclusion: false)
					+ "public partial interface IRepository<T> : IAsyncDisposable\n"
					+ "where T : class\n"
					+ "{\n"
					+ "\tT Get();\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteEnum_WithUnderlyingType_WritesEnum()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("Status")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			EnumUnderlyingType = Type("byte"),
		};

		// Act
		writer.WriteEnum(declaration, body => body.WriteLine("None = 0,").WriteLine("Ready = 1,"));

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes(includeCoverageExclusion: false)
					+ "public enum Status : byte\n{\n\tNone = 0,\n\tReady = 1,\n}\n"
			);
	}

	[Test]
	public async Task WriteAttributeClass_WithDefaults_WritesAttributeUsageAndSystemAttributeBase()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteAttributeClass(
			new TypeDeclarationOptions("RegistryAttribute")
			{
				Accessibility = TypeDeclarationAccessibility.Public,
				IsPartial = false,
			},
			AttributeTargets.Class,
			body => body.WriteLine("public string? Name { get; init; }")
		);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"[global::Microsoft.CodeAnalysis.Embedded]\n"
					+ GeneratedAttributes()
					+ "[global::System.AttributeUsage(global::System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]\n"
					+ "public sealed class RegistryAttribute : global::System.Attribute\n"
					+ "{\n"
					+ "\tpublic string? Name { get; init; }\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteAttributeClass_WithOptions_WritesCombinedTargetsFlagsAttributesAndCustomBase()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteAttributeClass(
			new TypeDeclarationOptions("KnownTypeAttribute")
			{
				Accessibility = TypeDeclarationAccessibility.Internal,
				IsPartial = false,
				BaseType = Type("CustomAttributeBase"),
				Attributes = [new(new TypeValueObject("Obsolete", null))],
			},
			AttributeTargets.Class | AttributeTargets.Property,
			_ => { },
			inherited: true,
			allowMultiple: true
		);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"[global::Microsoft.CodeAnalysis.Embedded]\n"
					+ GeneratedAttributes()
					+ "[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Property, Inherited = true, AllowMultiple = true)]\n"
					+ "[Obsolete]\n"
					+ "internal sealed class KnownTypeAttribute : CustomAttributeBase\n"
					+ "{\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteAttributeClass_WithEmbeddedAttributeDisabled_OmitsEmbeddedAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteAttributeClass(
			new TypeDeclarationOptions("LocalAttribute") { IsPartial = false, IncludeEmbeddedAttribute = false },
			AttributeTargets.Class,
			_ => { }
		);

		await Assert.That(writer.ToString()).DoesNotContain("[global::Microsoft.CodeAnalysis.Embedded]");
	}

	[Test]
	public async Task WriteAttributeClass_GivenNoTargets_ThrowsWithoutWriting()
	{
		var writer = CodeWriterFactory.ForTests();

		await Assert
			.That(() => writer.WriteAttributeClass(new("InvalidAttribute"), 0, _ => { }))
			.Throws<ArgumentOutOfRangeException>();
		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task WriteEnum_WithStructuredFields_WritesSummariesAttributesAndValues()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("Status") { Accessibility = TypeDeclarationAccessibility.Public };

		// Act
		writer.WriteEnum(
			declaration,
			new EnumFieldDeclarationOptions("None", 0)
			{
				XmlSummary = ["No status has been selected."],
				Attributes = [new(new TypeValueObject("Obsolete", null))],
			},
			new EnumFieldDeclarationOptions("Ready", (object)"1 << 0"),
			new EnumFieldDeclarationOptions("Unknown")
		);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes(includeCoverageExclusion: false)
					+ "public enum Status\n"
					+ "{\n"
					+ "\t/// <summary>No status has been selected.</summary>\n"
					+ "\t[Obsolete]\n"
					+ "\tNone = 0,\n"
					+ "\tReady = 1 << 0,\n"
					+ "\tUnknown,\n"
					+ "}\n"
			);
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task EnumFieldDeclarationOptions_GivenMissingName_Throws(string? fieldName)
	{
		await Assert.That(() => new EnumFieldDeclarationOptions(fieldName!)).Throws<ArgumentException>();
	}

	[Test]
	public async Task WriteEnumField_GivenDefaultOptions_ThrowsWithoutWriting()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act / Assert
		await Assert.That(() => writer.WriteEnumField(default)).Throws<ArgumentException>();
		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task WriteDelegate_WithGenericConstraints_WritesCompleteDeclaration()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new TypeDeclarationOptions("Factory")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			DelegateReturnType = Type("TResult"),
			DelegateParameters = [new("value", Type("T"))],
			GenericTypes =
			[
				new GenericTypeParameterOptions("T") { Constraints = ["class"] },
				new GenericTypeParameterOptions("TResult"),
			],
		};

		// Act
		writer.WriteDelegate(declaration);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes(includeCoverageExclusion: false)
					+ "public delegate TResult Factory<T, TResult>(T value)\nwhere T : class;\n"
			);
	}

	[Test]
	public async Task WriteMethod_GivenLongParameters_WritesOneParameterPerLine()
	{
		var writer = CodeWriterFactory.ForTests();

		using (
			writer.WriteMethodScope(
				new MethodDeclarationOptions(
					"AddAspireResourceKit",
					Type("global::Aspire.Hosting.IDistributedApplicationBuilder")
				)
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					Parameters =
					[
						new(
							"onBuilt",
							Type(
									"global::System.Action<global::Testing.HostKitNamespace.TestingHostKit, global::Aspire.Hosting.IDistributedApplicationBuilder>"
								)
								.Nullable()
						)
						{
							DefaultValue = "null",
						},
						new(
							"onConfigured",
							Type("global::System.Action<global::Testing.HostKitNamespace.TestingHostKit>").Nullable()
						)
						{
							DefaultValue = "null",
						},
						new(
							"configureOptions",
							Type(
									"global::System.Action<global::Microsoft.Extensions.Options.OptionsBuilder<global::Testing.HostKitNamespace.TestingHostKit.TestingHostKitOptions>>"
								)
								.Nullable()
						)
						{
							DefaultValue = "null",
						},
					],
				}
			)
		)
		{
			writer.WriteLine("return builder;");
		}

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "public global::Aspire.Hosting.IDistributedApplicationBuilder AddAspireResourceKit(\n"
					+ "\tglobal::System.Action<global::Testing.HostKitNamespace.TestingHostKit, global::Aspire.Hosting.IDistributedApplicationBuilder>? onBuilt = null,\n"
					+ "\tglobal::System.Action<global::Testing.HostKitNamespace.TestingHostKit>? onConfigured = null,\n"
					+ "\tglobal::System.Action<global::Microsoft.Extensions.Options.OptionsBuilder<global::Testing.HostKitNamespace.TestingHostKit.TestingHostKitOptions>>? configureOptions = null)\n"
					+ "{\n"
					+ "\treturn builder;\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteMethod_GivenStructuredOptions_WritesModifiersGenericsAndBody()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new MethodDeclarationOptions("CreateAsync", Type("Task").Type.MakeGeneric(Type("T")))
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			IsStatic = true,
			IsAsync = true,
			Parameters = [new("value", Type("T")), new("cancellationToken", Type("CancellationToken"))],
			GenericTypes = [new GenericTypeParameterOptions("T") { Constraints = ["class"] }],
		};

		// Act
		writer.WriteMethod(declaration, body => body.WriteLine("return await SaveAsync(value);"));

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "public static async Task<T> CreateAsync<T>(T value, CancellationToken cancellationToken)\n"
					+ "where T : class\n"
					+ "{\n"
					+ "\treturn await SaveAsync(value);\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WritePartialMethod_GivenPartialMethods_WritesDeclaration()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		MethodDeclarationOptions declaration = new("CreateAsync", Type("Task").Type.MakeGeneric(Type("T")))
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			IsAsync = true,
			IsPartial = true,
			Parameters = [new("value", Type("T")), new("cancellationToken", Type("CancellationToken"))],
			GenericTypes = [new("T") { Constraints = ["class"] }],
		};

		// Act
		writer.WritePartialMethod(declaration);

		// Assert
		await Assert
			.That(writer)
			.Generates(
				GeneratedAttributes()
					+ "public async partial Task<T> CreateAsync<T>(T value, CancellationToken cancellationToken)\n"
					+ "where T : class;\n"
			);
	}

	[Test]
	public async Task StructuredDeclarations_GivenAttributes_WritesTypeMemberReturnAndParameterAttributes()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var generatedCode = new AttributeDeclarationOptions(new TypeValueObject("GeneratedCode", null))
		{
			Arguments =
			[
				new("\"Generator\""),
				new("\"1.0\"") { Name = "version" },
				new(false) { Name = "Enabled", IsPropertyAssignment = true },
			],
		};
		var type = new TypeDeclarationOptions("Service")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			Attributes = [generatedCode],
		};
		var method = new MethodDeclarationOptions("TryGet", Type("bool"))
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			Attributes = [new(new TypeValueObject("Obsolete", null))],
			ReturnAttributes = [new(new TypeValueObject("NotNull", null))],
			Parameters =
			[
				new("value", Type("string").Nullable())
				{
					Modifier = ParameterModifier.Out,
					Attributes = [new(new TypeValueObject("NotNullWhen", null)) { Arguments = [new(true)] }],
				},
			],
		};

		// Act
		writer.WriteClass(type, body => body.WriteMethod(method, methodBody => methodBody.WriteLine("throw null;")));

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "[GeneratedCode(\"Generator\", version: \"1.0\", Enabled = false)]\n"
					+ "public sealed partial class Service\n"
					+ "{\n"
					+ IndentedGeneratedAttributes()
					+ "\t[Obsolete]\n"
					+ "\t[return: NotNull]\n"
					+ "\tpublic bool TryGet([NotNullWhen(true)] out string? value)\n"
					+ "\t{\n"
					+ "\t\tthrow null;\n"
					+ "\t}\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task AttributeDeclaration_RendersRetainedTypeReferenceSyntax()
	{
		var writer = CodeWriterFactory.ForTests();
		var attributeType = new TypeValueObject("MarkerAttribute", "Example")
			.MakeGeneric(TypeValueObject.Create<string>())
			.AsTypeReference()
			.Nullable();

		writer.WriteClass(
			new TypeDeclarationOptions("C") { Attributes = [new AttributeDeclarationOptions(attributeType)] },
			_ => { }
		);

		await Assert.That(writer.ToString()).Contains("[global::Example.Marker<string>?]");
	}

	[Test]
	public async Task PublicScopeReturningMethods_EndWithScope()
	{
		// Arrange / Act
		var invalidMethodNames = typeof(CodeWriter)
			.GetMethods()
			.Where(static method =>
				method.ReturnType == typeof(CodeWriter.BlockScope)
				|| method.ReturnType == typeof(CodeWriter.IndentScope)
			)
			.Where(static method => !method.Name.EndsWith("Scope", StringComparison.Ordinal))
			.Select(static method => method.Name)
			.ToArray();

		// Assert
		await Assert.That(invalidMethodNames).IsEmpty();
	}

	[Test]
	public async Task PublicScopeReturningMethods_HaveCallbackCounterparts()
	{
		// Arrange
		var methods = typeof(CodeWriter).GetMethods();

		// Act
		var missingCounterparts = methods
			.Where(static method =>
				method.ReturnType == typeof(CodeWriter.BlockScope)
				|| method.ReturnType == typeof(CodeWriter.IndentScope)
			)
			.Where(scopeMethod =>
			{
				var scopeParameters = scopeMethod.GetParameters();
				var counterpartName = scopeMethod.Name[..^"Scope".Length];
				return !methods.Any(method =>
				{
					if (method.Name != counterpartName || method.ReturnType != typeof(CodeWriter))
						return false;

					var parameters = method.GetParameters();
					if (
						parameters.Length != scopeParameters.Length + 1
						|| parameters[^1].ParameterType != typeof(Action<CodeWriter>)
					)
						return false;

					for (var index = 0; index < scopeParameters.Length; index++)
					{
						if (parameters[index].ParameterType != scopeParameters[index].ParameterType)
							return false;
					}
					return true;
				});
			})
			.Select(static method => method.Name)
			.ToArray();

		// Assert
		await Assert.That(missingCounterparts).IsEmpty();
	}

	[Test]
	public async Task WriteClass_GivenAttributeTypeValueObject_DoesNotDuplicateAttributeBrackets()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var attribute = new AttributeDeclarationOptions(
			new TypeValueObject("HostKitAttribute", "Purview.Aspire.ResourceKit")
		)
		{
			Arguments = [new AttributeArgumentOptions(true) { Name = "GenerateOptions", IsPropertyAssignment = true }],
		};

		// Act
		writer.WriteClassScope(new TypeDeclarationOptions("Host") { Attributes = [attribute] }).Dispose();

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "[global::Purview.Aspire.ResourceKit.HostKit(GenerateOptions = true)]\n"
					+ "sealed partial class Host\n"
					+ "{\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task AttributeTypeValueObject_GivenDeclarationContexts_RendersUnderlyingType()
	{
		// Arrange
		var attributeType = new TypeValueObject("RegistryAttribute", "Example");
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteAttributeClass(
			new TypeDeclarationOptions(attributeType) { IsPartial = false },
			AttributeTargets.Class,
			body =>
			{
				body.XmlSummary($"Creates a {CodeWriter.XmlSee(attributeType)} instance.");
				body.WriteConstructor(new ConstructorDeclarationOptions(attributeType), _ => { });
				body.WriteProperty(new PropertyDeclarationOptions("Parent", attributeType));
			}
		);

		// Assert
		var result = writer.ToString();
		await Assert.That(result).Contains("class RegistryAttribute : global::System.Attribute");
		await Assert.That(result).Contains("<see cref=\"global::Example.RegistryAttribute\" />");
		await Assert.That(result).Contains("RegistryAttribute()");
		await Assert.That(result).Contains("global::Example.RegistryAttribute Parent");
		await Assert.That(result).DoesNotContain("[global::Example.Registry]");
	}

	[Test]
	public async Task TypeReference_GivenNestedNullableGenericAndArray_RendersStructuredType()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var valueType = Type("global::System.Collections.Generic.Dictionary")
			.Type.MakeGeneric(Type("string"), Type("Widget").Nullable())
			.MakeArray()
			.Nullable();
		var method = new MethodDeclarationOptions("Load", valueType)
		{
			Parameters =
			[
				new(
					"items",
					Type("global::System.Collections.Generic.List")
						.Type.MakeGeneric(Type("Widget").Nullable())
						.AsTypeReference()
						.Nullable()
				),
			],
			ExpressionBody = "items.ToArray()",
		};

		// Act
		writer.WriteMethodScope(method);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "global::System.Collections.Generic.Dictionary<string, Widget?>[]? Load(\n"
					+ "\tglobal::System.Collections.Generic.List<Widget?>? items) => items.ToArray();\n"
			);
	}

	[Test]
	public async Task WriteMethod_GivenNullableParameterOption_WritesNullableTypeOnce()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var method = new MethodDeclarationOptions("Use", Type("void"))
		{
			Parameters =
			[
				new ParameterDeclarationOptions("value", Type("Widget").Nullable()) { DefaultValue = "null" },
			],
			ExpressionBody = "Consume(value)",
		};

		// Act
		writer.WriteMethodScope(method);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(GeneratedAttributes() + "void Use(Widget? value = null) => Consume(value);\n");
	}

	[Test]
	public async Task WriteProperty_GivenAutoAccessorsAndInitializer_WritesProperty()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new PropertyDeclarationOptions("Name", Type("string"))
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			HasSetter = true,
			IsInitOnly = true,
			Initializer = "string.Empty",
		};

		// Act
		writer.WriteProperty(declaration);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(GeneratedAttributes() + "public string Name { get; init; } = string.Empty;\n");
	}

	[Test]
	public async Task WriteProperty_GivenIsInitOnlyOnly_WritesInitAccessor()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new PropertyDeclarationOptions("Name", Type("string"))
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			IsInitOnly = true,
			Initializer = "string.Empty",
		};

		// Act
		writer.WriteProperty(declaration);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(GeneratedAttributes() + "public string Name { get; init; } = string.Empty;\n");
	}

	[Test]
	public async Task WriteProperty_GivenAccessorBodies_WritesScopedAccessors()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new PropertyDeclarationOptions("Value", Type("int"))
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			HasSetter = true,
			SetterAccessibility = TypeDeclarationAccessibility.Private,
		};

		// Act
		writer.WriteProperty(
			declaration,
			getter => getter.WriteLine("return _value;"),
			setter => setter.WriteLine("_value = value;")
		);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "public int Value\n"
					+ "{\n"
					+ "\tget\n\t{\n\t\treturn _value;\n\t}\n"
					+ "\tprivate set\n\t{\n\t\t_value = value;\n\t}\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteProperty_GivenIsInitOnlyOnlyWithAccessorBodies_WritesInitAccessor()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new PropertyDeclarationOptions("Value", Type("int"))
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			IsInitOnly = true,
		};

		// Act
		writer.WriteProperty(
			declaration,
			getter => getter.WriteLine("return _value;"),
			setter => setter.WriteLine("_value = value;")
		);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "public int Value\n"
					+ "{\n"
					+ "\tget\n\t{\n\t\treturn _value;\n\t}\n"
					+ "\tinit\n\t{\n\t\t_value = value;\n\t}\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteProperty_GivenExpressionBody_WritesExpressionProperty()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new PropertyDeclarationOptions("Count", Type("int"))
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			ExpressionBody = "_items.Count",
		};

		// Act
		writer.WriteProperty(declaration);

		// Assert
		await Assert.That(writer.ToString()).IsEqualTo(GeneratedAttributes() + "public int Count => _items.Count;\n");
	}

	[Test]
	public async Task WriteRecordStruct_GivenIsInitOnlyProperty_WritesReadonlyCompatibleProperty()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		using (
			writer.WriteRecordStructScope(
				new TypeDeclarationOptions("Sample")
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					IsReadOnly = true,
				}
			)
		)
		{
			writer.WriteProperty(
				new PropertyDeclarationOptions("Name", Type("string"))
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					IsInitOnly = true,
				}
			);
		}

		// Assert
		var result = writer.ToString();
		await Assert
			.That(result)
			.Contains("public string Name { get; init; }")
			.Because("init-only properties are valid in readonly structs");
	}

	[Test]
	public async Task WriteField_GivenReadonlyStaticField_WritesField()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var declaration = new FieldDeclarationOptions("Empty", Type("Example"))
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			IsStatic = true,
			IsReadOnly = true,
			Initializer = "new()",
		};

		// Act
		writer.WriteField(declaration);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes(includeCoverageExclusion: false) + "public static readonly Example Empty = new();\n"
			);
	}

	[Test]
	public async Task StructuredMembers_GivenConsecutiveFields_DoesNotAddBlankLine()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer
			.WriteField(new FieldDeclarationOptions("_first", Type("int")))
			.WriteField(new FieldDeclarationOptions("_second", Type("int")));

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes(includeCoverageExclusion: false)
					+ "int _first;\n"
					+ GeneratedAttributes(includeCoverageExclusion: false)
					+ "int _second;\n"
			);
	}

	[Test]
	public async Task StructuredMembers_GivenDifferentMemberKinds_AddsBlankLine()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteField(new FieldDeclarationOptions("_value", Type("int")));
		writer.WriteProperty(
			new PropertyDeclarationOptions("Value", Type("int")) { Accessibility = TypeDeclarationAccessibility.Public }
		);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes(includeCoverageExclusion: false)
					+ "int _value;\n"
					+ "\n"
					+ GeneratedAttributes()
					+ "public int Value { get; }\n"
			);
	}

	[Test]
	public async Task StructuredMembers_GivenScopedMethods_AddsBlankLineAfterScopeCloses()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		var first = new MethodDeclarationOptions("First", Type("void"));
		var second = new MethodDeclarationOptions("Second", Type("void"));

		// Act
		using (writer.WriteMethodScope(first))
		{
			writer.WriteLine("Execute();");
		}
		using (writer.WriteMethodScope(second))
		{
			writer.WriteLine("Execute();");
		}

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "void First()\n{\n\tExecute();\n}\n"
					+ "\n"
					+ GeneratedAttributes()
					+ "void Second()\n{\n\tExecute();\n}\n"
			);
	}

	[Test]
	public async Task StructuredMembers_GivenDocumentationTrivia_InsertsSeparatorBeforeTrivia()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteField(new("_value", Type("int")));
		writer.XmlSummary("Gets the value.");
		writer.WriteProperty(new("Value", Type("int")));

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes(includeCoverageExclusion: false)
					+ "int _value;\n"
					+ "\n"
					+ "/// <summary>Gets the value.</summary>\n"
					+ GeneratedAttributes()
					+ "int Value { get; }\n"
			);
	}

	[Test]
	public async Task StructuredMembers_GivenExistingBlankLine_DoesNotAddAnother()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteField(new FieldDeclarationOptions("_value", Type("int"))).NewLine();
		writer.WriteProperty(new PropertyDeclarationOptions("Value", Type("int")));

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes(includeCoverageExclusion: false)
					+ "int _value;\n"
					+ "\n"
					+ GeneratedAttributes()
					+ "int Value { get; }\n"
			);
	}

	[Test]
	public async Task Block_WithBodyAndCustomSeparators_WritesDelimitedBody()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteDelimitedBlock(
			"Create",
			"(",
			");",
			body =>
			{
				body.Quote("value").WriteLine(",");
				body.WriteLine("EmptyPath");
			}
		);

		// Assert
		await Assert.That(writer.ToString()).IsEqualTo("Create\n(\n\t\"value\",\n\tEmptyPath\n);\n");
	}

	[Test]
	public async Task Block_WithBodyLast_WritesBodyInsideCustomSeparators()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteDelimitedBlock("Create", "(", ");", body => body.Quote("value").WriteLine());

		// Assert
		await Assert.That(writer.ToString()).IsEqualTo("Create\n(\n\t\"value\"\n);\n");
	}

	[Test]
	public async Task WriteMethodCall_WritesSimpleInvocation()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteMethodCall("Run", "value", "cancellationToken");

		await Assert.That(writer.ToString()).IsEqualTo("Run(value, cancellationToken);\n");
	}

	[Test]
	public async Task WriteAwaitedMethodCall_WritesAwaitPrefix()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteAwaitedMethodCall("LoadAsync", "cancellationToken");

		await Assert.That(writer.ToString()).IsEqualTo("await LoadAsync(cancellationToken);\n");
	}

	[Test]
	public async Task WriteAwaitedMethodCall_WithStructuredArguments_WritesReceiverAndModifiers()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteAwaitedMethodCall(
			"LoadAsync",
			new MethodCallArgumentOptions[]
			{
				new("token"),
				new("result") { Modifier = ParameterModifier.Out },
			},
			receiver: "service",
			writeArgumentsOnSeparateLines: true
		);

		await Assert.That(writer.ToString()).IsEqualTo("await service.LoadAsync(\n\ttoken,\n\tout result);\n");
	}

	[Test]
	public async Task WriteMethodCall_WritesReceiverGenericArgumentsAndMultilineArguments()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteMethodCall(
			"Create",
			["firstArgumentWithANameThatMakesTheCallLong", "secondArgumentWithANameThatMakesTheCallLong"],
			receiver: "factory",
			genericArguments: [Type("string")]
		);

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"factory.Create<string>(\n"
					+ "\tfirstArgumentWithANameThatMakesTheCallLong,\n"
					+ "\tsecondArgumentWithANameThatMakesTheCallLong);\n"
			);
	}

	[Test]
	public async Task WriteMethodCall_WithStructuredArguments_WritesModifiersAndMultilineArguments()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteMethodCall(
			"AMethodCallWithLotsOfParams",
			new MethodCallArgumentOptions[]
			{
				new("a-long-a-param") { Modifier = ParameterModifier.Ref },
				new("another-long-param") { Modifier = ParameterModifier.Out },
			},
			writeArgumentsOnSeparateLines: true
		);

		await Assert
			.That(writer.ToString())
			.IsEqualTo("AMethodCallWithLotsOfParams(\n" + "\tref a-long-a-param,\n" + "\tout another-long-param);\n");
	}

	[Test]
	public async Task WriteMethodCall_WithStructuredArgument_WritesNamedArgument()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteMethodCall("Configure", new MethodCallArgumentOptions[] { new("value") { Name = "option" } });

		await Assert.That(writer.ToString()).IsEqualTo("Configure(option: value);\n");
	}

	[Test]
	public async Task WriteAssignment_WithObjectCreationOptions_WritesOptionalVarAndMixedArguments()
	{
		var writer = CodeWriterFactory.ForTests();
		var creation = new ObjectCreationOptions(
			Type("ASpecificType"),
			"propVal1",
			new MethodCallArgumentOptions("propVal2") { Name = "second" }
		)
		{
			WriteArgumentsOnSeparateLines = true,
		};

		writer.WriteAssignment("var", "@event", creation);
		writer.WriteAssignment("existingEvent", creation);

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"var @event = new ASpecificType(\n"
					+ "\tpropVal1,\n"
					+ "\tsecond: propVal2);\n"
					+ "existingEvent = new ASpecificType(\n"
					+ "\tpropVal1,\n"
					+ "\tsecond: propVal2);\n"
			);
	}

	[Test]
	public async Task ToString_GivenOpenBlockAndValidationEnabled_ThrowsScopeValidationException()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests(throwOnUnclosedScopes: true);
		var scope = writer.OpenBlockScope("public sealed class Example");

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
			.Contains(nameof(ToString_GivenOpenBlockAndValidationEnabled_ThrowsScopeValidationException));
		await Assert.That(exception.Message).Contains("public sealed class Example");

		scope.Dispose();
		await Assert.That(writer.OpenScopeCount).IsEqualTo(0);
		await Assert.That(writer.ToString()).Contains("public sealed class Example");
	}

	[Test]
	public async Task ToString_GivenOpenBlockAndValidationDisabled_ReturnsPartialSource()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests(throwOnUnclosedScopes: false);
		var scope = writer.OpenBlockScope("public sealed class Example");

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
		var writer = CodeWriterFactory.ForTests(throwOnUnclosedScopes: true);
		var scope = writer.IndentedScope();

		// Act
		string Action() => writer.ToString();

		// Assert
		await Assert.That(writer.OpenScopeCount).IsEqualTo(1);
		var exception = await Assert.That(Action).Throws<CodeWriterScopeValidationException>();
		await Assert.That(exception!.OpenScopeCount).IsEqualTo(1);
		await Assert.That(exception.OpenScopes[0].Kind).IsEqualTo("indentation");
		await Assert
			.That(exception.OpenScopes[0].OpeningStackTrace)
			.Contains(nameof(ToString_GivenOpenIndentScopeAndValidationEnabled_ThrowsScopeValidationException));

		scope.Dispose();
		await Assert.That(writer.OpenScopeCount).IsEqualTo(0);
	}

	[Test]
	public async Task WriteAutoGeneratedHeader_WritesHeader()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteAutoGeneratedHeader("TestGenerator", "1.0");

		var result = writer.ToString();

		await Assert.That(result).Contains("// <auto-generated />");
		await Assert.That(result).Contains("TestGenerator");
		await Assert.That(result).Contains("version 1.0");
		await Assert.That(result).DoesNotContain("// Generated at ");
	}

	[Test]
	public async Task GeneratorIdentity_GivenNoHeaderArguments_UsesDefaultsAndDecoratesDeclarations()
	{
		var writer = new CodeWriter("HostKitGenerator", "2.3.4", throwOnUnclosedScopes: false);

		writer.WriteAutoGeneratedHeader();
		writer.WriteClass(
			new TypeDeclarationOptions("GeneratedType"),
			body => body.WriteProperty(new PropertyDeclarationOptions("Value", TypeValueObject.Create<string>()))
		);

		var result = writer.ToString();
		await Assert.That(result).Contains("HostKitGenerator (version 2.3.4)");
		await Assert.That(result).DoesNotContain("// Generated at ");
		await Assert.That(result).DoesNotContain("[global::Microsoft.CodeAnalysis.Embedded]");
		await Assert.That(result).Contains("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
		await Assert.That(result).Contains("[global::System.Runtime.CompilerServices.CompilerGenerated]");
		await Assert
			.That(result)
			.Contains("[global::System.CodeDom.Compiler.GeneratedCode(\"HostKitGenerator\", \"2.3.4\")]");
	}

	[Test]
	public async Task WriteConstructor_WithMultilineParameters_WritesInitializerOnNewLine()
	{
		var writer = CodeWriterFactory.ForTests();
		var declaration = new ConstructorDeclarationOptions("Repository")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			WriteParametersOnSeparateLines = true,
			Parameters = [new("connectionString", Type("string")), new("logger", Type("ILogger"))],
			Initializer = "this(connectionString, logger, true)",
		};

		writer.WriteConstructor(declaration, static _ => { });

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "public Repository(\n"
					+ "\tstring connectionString,\n"
					+ "\tILogger logger\n"
					+ ")\n"
					+ "\t: this(connectionString, logger, true)\n"
					+ "{\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task GeneratorIdentity_GivenConstField_DoesNotWriteInvalidCoverageAttribute()
	{
		var writer = new CodeWriter("HostKitGenerator", "2.3.4", throwOnUnclosedScopes: false);

		writer.WriteField(
			new FieldDeclarationOptions("SectionName", TypeValueObject.Create<string>())
			{
				Accessibility = TypeDeclarationAccessibility.Public,
				IsConst = true,
				Initializer = "\"TestingHostKit\"",
			}
		);

		var result = writer.ToString();
		await Assert.That(result).DoesNotContain("ExcludeFromCodeCoverage");
		await Assert.That(result).Contains("CompilerGenerated");
		await Assert.That(result).Contains("GeneratedCode(");
	}

	[Test]
	public async Task WriteGeneratedCodeAttribute_WritesAttribute()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteGeneratedCodeAttribute("TestGenerator", "1.0.0.0");

		var result = writer.ToString();

		await Assert
			.That(result)
			.Contains("[global::System.CodeDom.Compiler.GeneratedCode(\"TestGenerator\", \"1.0.0.0\")]");
	}

	[Test]
	public async Task Determinism_SameInputProducesIdenticalOutputAndNoTimestamp()
	{
		// Arrange
		static string Generate()
		{
			var writer = CodeWriterFactory.ForTests();
			writer.WriteAutoGeneratedHeader();
			writer.WriteClass(
				new TypeDeclarationOptions("Sample") { Accessibility = TypeDeclarationAccessibility.Public },
				body =>
					body.WriteMethod(
						new MethodDeclarationOptions("M", Type("void"))
						{
							Accessibility = TypeDeclarationAccessibility.Public,
						},
						methodBody => methodBody.WriteLine("return;")
					)
			);
			return writer.ToString();
		}

		// Act
		var first = Generate();
		var second = Generate();

		// Assert
		await Assert.That(first).IsEqualTo(second);
		await Assert.That(first).DoesNotContain("// Generated at ");
	}

	[Test]
	public async Task ScopeBalance_ThrowsWhenScopeLeftOpen()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests(throwOnUnclosedScopes: true);
		writer.OpenBlockScope("public class Example");

		// Act
		string Action() => writer.ToString();

		// Assert
		await Assert.That(Action).Throws<CodeWriterScopeValidationException>();
	}

	[Test]
	public async Task PragmaScope_EmitsDisableAndRestore()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteLine("// before");
		using (writer.OpenPragmasScope("CS0618", "CS1591"))
		{
			writer.WriteLine("// inside");
		}
		writer.WriteLine("// after");

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"// before\n"
					+ "\n"
					+ "#pragma warning disable CS0618\n"
					+ "#pragma warning disable CS1591\n"
					+ "// inside\n"
					+ "\n"
					+ "#pragma warning restore CS0618\n"
					+ "#pragma warning restore CS1591\n"
					+ "// after\n"
			);
	}

	[Test]
	public async Task OpenScope_CapturesOpeningStackTrace()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests(throwOnUnclosedScopes: true);
		writer.OpenBlockScope("public class Example");

		// Act
		string Action() => writer.ToString();

		// Assert
		var exception = await Assert.That(Action).Throws<CodeWriterScopeValidationException>();
		await Assert
			.That(exception!.OpenScopes[0].OpeningStackTrace)
			.Contains(nameof(OpenScope_CapturesOpeningStackTrace));
	}

	[Test]
	public async Task MemberDeclarations_GivenIncludeGeneratedAttributesFalse_OmitsGeneratedAttributes()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteClass(
			new TypeDeclarationOptions("Sample")
			{
				Accessibility = TypeDeclarationAccessibility.Public,
				IncludeGeneratedAttributes = false,
			},
			body =>
			{
				body.WriteField(
					new FieldDeclarationOptions("_field", Type("int")) { IncludeGeneratedAttributes = false }
				);
				body.WriteProperty(
					new PropertyDeclarationOptions("Property", Type("int")) { IncludeGeneratedAttributes = false }
				);
				body.WriteMethod(
					new MethodDeclarationOptions("Method", Type("void")) { IncludeGeneratedAttributes = false },
					methodBody => methodBody.WriteLine("return;")
				);
				body.WriteConstructor(
					new ConstructorDeclarationOptions("Sample") { IncludeGeneratedAttributes = false },
					constructorBody => constructorBody.WriteLine("// ctor")
				);
			}
		);

		// Assert
		var result = writer.ToString();
		await Assert
			.That(result)
			.DoesNotContain("[global::System.CodeDom.Compiler.GeneratedCode")
			.Because("no generated attributes should be emitted");
		await Assert.That(result).Contains("int _field;");
		await Assert.That(result).Contains("int Property { get; }");
		await Assert.That(result).Contains("void Method()");
		await Assert.That(result).Contains("Sample()");
	}

	[Test]
	public async Task ClassDeclaration_GivenDefaultCodeWriterAndNoOverride_EmitsGeneratedAttributes()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();

		// Act
		writer.WriteClass(new("Sample", TypeDeclarationAccessibility.Public), body => body.Comment("Empty"));

		// Assert
		var result = writer.ToString();
		await Assert
			.That(result)
			.Contains(GeneratedAttributes())
			.Because("default CodeWriter emits generated attributes when not overridden");
	}

	[Test]
	public async Task ClassDeclaration_GivenDefaultIncludeGeneratedAttributesFalseAndNoOverride_OmitsGeneratedAttributes()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		writer.DefaultIncludeGeneratedAttributes = false;

		// Act
		writer.WriteClass(
			new TypeDeclarationOptions("Sample") { Accessibility = TypeDeclarationAccessibility.Public },
			body => body.Comment("Empty")
		);

		// Assert
		var result = writer.ToString();
		await Assert
			.That(result)
			.DoesNotContain("[global::System.CodeDom.Compiler.GeneratedCode")
			.Because("CodeWriter default set to false suppresses generated attributes");
	}

	[Test]
	public async Task ClassDeclaration_GivenDefaultIncludeGeneratedAttributesFalseAndOverrideTrue_EmitsGeneratedAttributes()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		writer.DefaultIncludeGeneratedAttributes = false;

		// Act
		writer.WriteClass(
			new TypeDeclarationOptions("Sample")
			{
				Accessibility = TypeDeclarationAccessibility.Public,
				IncludeGeneratedAttributes = true,
			},
			body => body.Comment("Empty")
		);

		// Assert
		var result = writer.ToString();
		await Assert
			.That(result)
			.Contains(GeneratedAttributes())
			.Because("explicit override on the declaration takes precedence");
	}

	[Test]
	public async Task MemberDeclarations_GivenDefaultIncludeGeneratedAttributesFalseAndNoOverride_OmitsGeneratedAttributes()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		writer.DefaultIncludeGeneratedAttributes = false;

		// Act
		writer.WriteClass(
			new TypeDeclarationOptions("Sample") { Accessibility = TypeDeclarationAccessibility.Public },
			body =>
			{
				body.WriteField(new FieldDeclarationOptions("_field", Type("int")));
				body.WriteProperty(new PropertyDeclarationOptions("Property", Type("int")));
				body.WriteMethod(
					new MethodDeclarationOptions("Method", Type("void")),
					methodBody => methodBody.WriteLine("return;")
				);
				body.WriteConstructor(
					new ConstructorDeclarationOptions("Sample"),
					constructorBody => constructorBody.WriteLine("// ctor")
				);
			}
		);

		// Assert
		var result = writer.ToString();
		await Assert
			.That(result)
			.DoesNotContain("[global::System.CodeDom.Compiler.GeneratedCode")
			.Because("members inherit the CodeWriter default");
		await Assert.That(result).Contains("int _field;");
		await Assert.That(result).Contains("int Property { get; }");
		await Assert.That(result).Contains("void Method()");
		await Assert.That(result).Contains("Sample()");
	}

	[Test]
	public async Task CreateTestWriter_WithDefaultParameters_SetsDefaultIncludeGeneratedAttributesFalse()
	{
		// Arrange / Act
		var writer = CodeWriter.CreateTestWriter();

		// Assert
		await Assert.That(writer.DefaultIncludeGeneratedAttributes).IsFalse();
	}

	[Test]
	public async Task CreateTestWriter_WithTrueParameters_SetsDefaultIncludeGeneratedAttributesTrue()
	{
		// Arrange / Act
		var writer = CodeWriter.CreateTestWriter(includeGeneratedAttributes: true);

		// Assert
		await Assert.That(writer.DefaultIncludeGeneratedAttributes).IsTrue();
	}

	[Test]
	public async Task WriteIfBlock_WritesSingleLineConditionAndScopedBody()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteIfBlock("enabled", body => body.WriteReturn());

		await Assert.That(writer.ToString()).IsEqualTo("if (enabled)\n{\n\treturn;\n}\n");
	}

	[Test]
	public async Task WriteIfBlock_WritesMultilineConditionWithContinuationIndent()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteIfBlock("value != null\n&& value.IsValid", body => body.WriteReturn("value"));

		await Assert
			.That(writer.ToString())
			.IsEqualTo("if (value != null\n\t&& value.IsValid)\n{\n\treturn value;\n}\n");
	}

	[Test]
	public async Task WriteAssignment_WritesDeclarationAndMultilineInitializer()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteAssignment(
			"var value",
			value =>
			{
				value.WriteLine("new()");
				value.OpenBlock(
					null,
					block =>
					{
						block.WriteLine("X = 1,");
						block.WriteLine("Y = 2,");
						block.WriteLine("Z = 3");
					}
				);
			}
		);

		await Assert
			.That(writer.ToString())
			.IsEqualTo("var value = new()\n\t{\n\t\tX = 1,\n\t\tY = 2,\n\t\tZ = 3\n\t};\n");
	}

	[Test]
	public async Task WriteReturnAndThrow_WritesStatements()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteReturn("value");
		writer.WriteThrow(throwExpression => throwExpression.WriteLine("new InvalidOperationException()"));

		await Assert.That(writer.ToString()).IsEqualTo("return value;\nthrow new InvalidOperationException();\n");
	}

	[Test]
	public async Task ExpressionMembers_WritesMultilineExpressions()
	{
		var writer = CodeWriterFactory.ForTests();

		writer.WriteMethodScope(
			new MethodDeclarationOptions("Load", Type("Value")) { ExpressionBody = "Create()\n.Configure()" }
		);
		writer.WritePropertyExpression(
			new PropertyDeclarationOptions("Current", Type("Value")),
			property => property.WriteLine("GetCurrent()")
		);

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				GeneratedAttributes()
					+ "Value Load() => Create()\n\t.Configure();\n"
					+ "\n"
					+ GeneratedAttributes()
					+ "Value Current => GetCurrent();\n"
			);
	}

	[Test]
	public async Task WriteMethod_WithPartialDeclarationWithBody_WritesBodyOutsideMethod()
	{
		// Arrange
		var writer = CodeWriterFactory.ForTests();
		writer.DefaultIncludeGeneratedAttributes = false;

		// Act
		writer.WriteClass(
			new("Example") { IsPartial = true },
			body => body.WriteMethod(new("Apply") { IsPartial = true }, methodBody => methodBody.WriteReturn())
		);

		// Assert
		await Assert
			.That(writer)
			.ContainsGenerated(
				"""
partial void Apply()
{
	return;
}
"""
			);
	}
}
