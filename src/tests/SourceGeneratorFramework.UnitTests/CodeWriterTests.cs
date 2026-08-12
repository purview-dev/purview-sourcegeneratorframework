namespace Purview.SourceGeneratorFramework;

public class CodeWriterTests
{
	static TypeReferenceOptions Type(string name) => new(name);

	[Test]
	public async Task MemberDeclarationOptions_AreValueTypes()
	{
		// Arrange / Act / Assert
		await Assert.That(typeof(MethodDeclarationOptions).IsValueType).IsTrue();
		await Assert.That(typeof(PropertyDeclarationOptions).IsValueType).IsTrue();
		await Assert.That(typeof(FieldDeclarationOptions).IsValueType).IsTrue();
		await Assert.That(typeof(ConstructorDeclarationOptions).IsValueType).IsTrue();
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("   ")]
	public async Task TypeReferenceOptions_GivenMissingName_Throws(string? name)
	{
		await Assert.That(() => new TypeReferenceOptions(name!)).Throws<ArgumentException>();
	}

	[Test]
	public async Task EmptyTypeReference_IsIgnoredByMemberEmitters()
	{
		// Arrange
		var writer = new CodeWriter();

		// Act
		writer.WriteField(new FieldDeclarationOptions("field", TypeReferenceOptions.Empty));
		writer.WriteProperty(
			new PropertyDeclarationOptions("Property", TypeReferenceOptions.Empty)
		);
		writer
			.WriteMethodScope(new MethodDeclarationOptions("Method", TypeReferenceOptions.Empty))
			.Dispose();

		// Assert
		await Assert.That(writer.ToString()).IsEmpty();
	}

	[Test]
	public async Task WriteLine_AppendsLineWithIndent()
	{
		var writer = new CodeWriter();

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

		writer.WriteBlock("public class C", w => w.WriteLine("public int P { get; set; }"));

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
		var writer = new CodeWriter();

		writer.WriteBlockNamespace("First", body => body.WriteLine("class A { }"));
		writer.WriteBlockNamespace("Second", body => body.WriteLine("class B { }"));

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"namespace First\n{\n\tclass A { }\n}\n\n"
					+ "namespace Second\n{\n\tclass B { }\n}\n"
			);
	}

	[Test]
	public async Task WriteBlockNamespaceAndTopLevelType_InsertsBlankLineBetweenDeclarations()
	{
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("TopLevel");

		writer.WriteBlockNamespace("First", body => body.WriteLine("class Nested { }"));
		writer.WriteClass(declaration, static _ => { });
		writer.WriteBlockNamespace("Second", body => body.WriteLine("class Other { }"));

		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"namespace First\n{\n\tclass Nested { }\n}\n\n"
					+ "sealed partial class TopLevel\n{\n}\n\n"
					+ "namespace Second\n{\n\tclass Other { }\n}\n"
			);
	}

	[Test]
	public async Task WriteClass_WritesClassBlock()
	{
		var writer = new CodeWriter();

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
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("Repository")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			BaseType = Type("RepositoryBase").MakeGeneric(Type("T")),
			Interfaces = [Type("IRepository").MakeGeneric(Type("T")), Type("IDisposable")],
			GenericTypes =
			[
				new GenericTypeParameterOptions("T") { Constraints = ["class", "new()"] },
			],
		};

		using (writer.WriteClassScope(declaration))
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
			Interfaces = [Type("IEquatable").MakeGeneric(Type("Identifier"))],
		};

		using (writer.WriteRecordStructScope(declaration))
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
		using (writer.WriteClassScope(declaration))
		{
			// Intentionally empty.
		}

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo("public static partial class Extensions\n{\n}\n");
	}

	[Test]
	public async Task WriteClass_GivenAbstractDeclaration_WritesAbstractInsteadOfDefaultSealed()
	{
		// Arrange
		var writer = new CodeWriter();
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
			.IsEqualTo("public abstract partial class ServiceBase\n{\n}\n");
	}

	[Test]
	public async Task WriteStruct_GivenAbstractDeclaration_ThrowsArgumentException()
	{
		// Arrange
		var writer = new CodeWriter();
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
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("Invalid")
		{
			Kind = TypeDeclarationKind.Struct,
			IsStatic = true,
		};

		// Act
		CodeWriter.BlockScope Action() => writer.WriteTypeScope(declaration);

		// Assert
		await Assert.That(Action).Throws<ArgumentException>();
	}

	[Test]
	public async Task WriteStruct_WithBaseType_Throws()
	{
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("Invalid") { BaseType = Type("BaseType") };

		await Assert.That(() => writer.WriteStructScope(declaration)).Throws<ArgumentException>();
	}

	[Test]
	public async Task WriteClass_WithPrimaryConstructor_WritesParametersBeforeBaseType()
	{
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("Repository")
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			PrimaryConstructorParameters =
			[
				new("connectionString", Type("string")),
				new("logger", Type("ILogger")),
			],
			BaseType = Type("RepositoryBase(connectionString)"),
		};

		using (writer.WriteClassScope(declaration))
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
	public async Task WriteClass_WithEmptyBaseType_DoesNotWriteBaseListColon()
	{
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("ResourceKit")
		{
			BaseType = TypeReferenceOptions.Empty,
		};

		writer.WriteClass(declaration, static _ => { });

		await Assert
			.That(writer.ToString())
			.IsEqualTo("sealed partial class ResourceKit\n{\n}\n");
	}

	[Test]
	public async Task WriteClass_WithEmptyBaseAndInterfaces_WritesOnlyNonEmptyInterfaces()
	{
		var writer = new CodeWriter();
		var declaration = new TypeDeclarationOptions("ResourceKit")
		{
			BaseType = TypeReferenceOptions.Empty,
			Interfaces =
			[
				TypeReferenceOptions.Empty,
				new TypeReferenceOptions("IResourceKit"),
				TypeReferenceOptions.Empty,
			],
		};

		writer.WriteClass(declaration, static _ => { });

		await Assert
			.That(writer.ToString())
			.IsEqualTo("sealed partial class ResourceKit : IResourceKit\n{\n}\n");
	}

	[Test]
	public async Task WriteConstructor_WritesParametersInitializerAndBody()
	{
		var writer = new CodeWriter();
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
				"public Repository(string connectionString, ILogger logger)\n"
					+ "\t: base(connectionString)\n"
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
			writer.WriteConstructorScope(
				new ConstructorDeclarationOptions("Repository") { IsStatic = true }
			)
		)
		{
			// To stop IDE0055
		}

		await Assert.That(writer.ToString()).IsEqualTo("static Repository()\n{\n}\n");
	}

	[Test]
	public async Task WriteMethod_GivenShortParameters_WritesSingleLineDeclaration()
	{
		var writer = new CodeWriter();

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
				"public static void Execute(string name, bool enabled)\n"
					+ "{\n"
					+ "\tRun(name, enabled);\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteInterface_WithInheritanceAndConstraints_WritesInterface()
	{
		// Arrange
		var writer = new CodeWriter();
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
				"public partial interface IRepository<T> : IAsyncDisposable\n"
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
		var writer = new CodeWriter();
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
			.IsEqualTo("public enum Status : byte\n{\n\tNone = 0,\n\tReady = 1,\n}\n");
	}

	[Test]
	public async Task WriteDelegate_WithGenericConstraints_WritesCompleteDeclaration()
	{
		// Arrange
		var writer = new CodeWriter();
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
			.IsEqualTo("public delegate TResult Factory<T, TResult>(T value)\nwhere T : class;\n");
	}

	[Test]
	public async Task WriteMethod_GivenLongParameters_WritesOneParameterPerLine()
	{
		var writer = new CodeWriter();

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
							Type(
									"global::System.Action<global::Testing.HostKitNamespace.TestingHostKit>"
								)
								.Nullable()
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
				"public global::Aspire.Hosting.IDistributedApplicationBuilder AddAspireResourceKit(\n"
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
		var writer = new CodeWriter();
		var declaration = new MethodDeclarationOptions(
			"CreateAsync",
			Type("Task").MakeGeneric(Type("T"))
		)
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			IsStatic = true,
			IsAsync = true,
			Parameters =
			[
				new("value", Type("T")),
				new("cancellationToken", Type("CancellationToken")),
			],
			GenericTypes = [new GenericTypeParameterOptions("T") { Constraints = ["class"] }],
		};

		// Act
		writer.WriteMethod(declaration, body => body.WriteLine("return await SaveAsync(value);"));

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"public static async Task<T> CreateAsync<T>(T value, CancellationToken cancellationToken)\n"
					+ "where T : class\n"
					+ "{\n"
					+ "\treturn await SaveAsync(value);\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task StructuredDeclarations_GivenAttributes_WritesTypeMemberReturnAndParameterAttributes()
	{
		// Arrange
		var writer = new CodeWriter();
		var generatedCode = new AttributeDeclarationOptions("GeneratedCode")
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
			Attributes = [new("Obsolete")],
			ReturnAttributes = [new("NotNull")],
			Parameters =
			[
				new("value", Type("string").Nullable())
				{
					Modifier = ParameterModifier.Out,
					Attributes = [new("NotNullWhen") { Arguments = [new(true)] }],
				},
			],
		};

		// Act
		writer.WriteClass(
			type,
			body => body.WriteMethod(method, methodBody => methodBody.WriteLine("throw null;"))
		);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"[GeneratedCode(\"Generator\", version: \"1.0\", Enabled = false)]\n"
					+ "public sealed partial class Service\n"
					+ "{\n"
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
		var writer = new CodeWriter();
		var attribute = new AttributeDeclarationOptions(
			new Models.TypeValueObject("HostKitAttribute", "Purview.Aspire.ResourceKit")
		)
		{
			Arguments =
			[
				new AttributeArgumentOptions(true)
				{
					Name = "GenerateOptions",
					IsPropertyAssignment = true,
				},
			],
		};

		// Act
		writer
			.WriteClassScope(new TypeDeclarationOptions("Host") { Attributes = [attribute] })
			.Dispose();

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"[global::Purview.Aspire.ResourceKit.HostKit(GenerateOptions = true)]\n"
					+ "sealed partial class Host\n"
					+ "{\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task TypeReference_GivenNestedNullableGenericAndArray_RendersStructuredType()
	{
		// Arrange
		var writer = new CodeWriter();
		var valueType = Type("global::System.Collections.Generic.Dictionary")
			.MakeGeneric(Type("string"), Type("Widget").Nullable())
			.MakeArray()
			.Nullable();
		var method = new MethodDeclarationOptions("Load", valueType)
		{
			Parameters =
			[
				new(
					"items",
					Type("global::System.Collections.Generic.List")
						.MakeGeneric(Type("Widget").Nullable())
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
				"global::System.Collections.Generic.Dictionary<string, Widget?>[]? Load(\n"
					+ "\tglobal::System.Collections.Generic.List<Widget?>? items) => items.ToArray();\n"
			);
	}

	[Test]
	public async Task WriteMethod_GivenNullableParameterOption_WritesNullableTypeOnce()
	{
		// Arrange
		var writer = new CodeWriter();
		var method = new MethodDeclarationOptions("Use", Type("void"))
		{
			Parameters =
			[
				new ParameterDeclarationOptions("value", Type("Widget").Nullable())
				{
					IsNullable = true,
					DefaultValue = "null",
				},
			],
			ExpressionBody = "Consume(value)",
		};

		// Act
		writer.WriteMethodScope(method);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo("void Use(Widget? value = null) => Consume(value);\n");
	}

	[Test]
	public async Task WriteProperty_GivenAutoAccessorsAndInitializer_WritesProperty()
	{
		// Arrange
		var writer = new CodeWriter();
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
			.IsEqualTo("public string Name { get; init; } = string.Empty;\n");
	}

	[Test]
	public async Task WriteProperty_GivenAccessorBodies_WritesScopedAccessors()
	{
		// Arrange
		var writer = new CodeWriter();
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
				"public int Value\n"
					+ "{\n"
					+ "\tget\n\t{\n\t\treturn _value;\n\t}\n"
					+ "\tprivate set\n\t{\n\t\t_value = value;\n\t}\n"
					+ "}\n"
			);
	}

	[Test]
	public async Task WriteProperty_GivenExpressionBody_WritesExpressionProperty()
	{
		// Arrange
		var writer = new CodeWriter();
		var declaration = new PropertyDeclarationOptions("Count", Type("int"))
		{
			Accessibility = TypeDeclarationAccessibility.Public,
			ExpressionBody = "_items.Count",
		};

		// Act
		writer.WriteProperty(declaration);

		// Assert
		await Assert.That(writer.ToString()).IsEqualTo("public int Count => _items.Count;\n");
	}

	[Test]
	public async Task WriteField_GivenReadonlyStaticField_WritesField()
	{
		// Arrange
		var writer = new CodeWriter();
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
			.IsEqualTo("public static readonly Example Empty = new();\n");
	}

	[Test]
	public async Task StructuredMembers_GivenConsecutiveFields_DoesNotAddBlankLine()
	{
		// Arrange
		var writer = new CodeWriter();

		// Act
		writer
			.WriteField(new FieldDeclarationOptions("_first", Type("int")))
			.WriteField(new FieldDeclarationOptions("_second", Type("int")));

		// Assert
		await Assert.That(writer.ToString()).IsEqualTo("int _first;\nint _second;\n");
	}

	[Test]
	public async Task StructuredMembers_GivenDifferentMemberKinds_AddsBlankLine()
	{
		// Arrange
		var writer = new CodeWriter();

		// Act
		writer.WriteField(new FieldDeclarationOptions("_value", Type("int")));
		writer.WriteProperty(
			new PropertyDeclarationOptions("Value", Type("int"))
			{
				Accessibility = TypeDeclarationAccessibility.Public,
			}
		);

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo("int _value;\n\npublic int Value { get; }\n");
	}

	[Test]
	public async Task StructuredMembers_GivenScopedMethods_AddsBlankLineAfterScopeCloses()
	{
		// Arrange
		var writer = new CodeWriter();
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
				"void First()\n{\n\tExecute();\n}\n" + "\n" + "void Second()\n{\n\tExecute();\n}\n"
			);
	}

	[Test]
	public async Task StructuredMembers_GivenDocumentationTrivia_InsertsSeparatorBeforeTrivia()
	{
		// Arrange
		var writer = new CodeWriter();

		// Act
		writer.WriteField(new FieldDeclarationOptions("_value", Type("int")));
		writer.WriteXmlSummary("Gets the value.");
		writer.WriteProperty(new PropertyDeclarationOptions("Value", Type("int")));

		// Assert
		await Assert
			.That(writer.ToString())
			.IsEqualTo(
				"int _value;\n"
					+ "\n"
					+ "/// <summary>\n"
					+ "/// Gets the value.\n"
					+ "/// </summary>\n"
					+ "int Value { get; }\n"
			);
	}

	[Test]
	public async Task StructuredMembers_GivenExistingBlankLine_DoesNotAddAnother()
	{
		// Arrange
		var writer = new CodeWriter();

		// Act
		writer.WriteField(new FieldDeclarationOptions("_value", Type("int"))).NewLine();
		writer.WriteProperty(new PropertyDeclarationOptions("Value", Type("int")));

		// Assert
		await Assert.That(writer.ToString()).IsEqualTo("int _value;\n\nint Value { get; }\n");
	}

	[Test]
	public async Task Block_WithBodyAndCustomSeparators_WritesDelimitedBody()
	{
		// Arrange
		var writer = new CodeWriter();

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
		writer.WriteDelimitedBlock("Create", "(", ");", body => body.Quote("value").WriteLine());

		// Assert
		await Assert.That(writer.ToString()).IsEqualTo("Create\n(\n\t\"value\"\n);\n");
	}

	[Test]
	public async Task ToString_GivenOpenBlockAndValidationEnabled_ThrowsScopeValidationException()
	{
		// Arrange
		var writer = new CodeWriter(throwOnUnclosedScopes: true);
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
		var writer = new CodeWriter(throwOnUnclosedScopes: true);
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
		await Assert.That(result).Contains("// Generated at ");
	}

	[Test]
	public async Task GeneratorIdentity_GivenNoHeaderArguments_UsesDefaultsAndDecoratesDeclarations()
	{
		var writer = new CodeWriter(
			throwOnUnclosedScopes: false,
			generatorName: "HostKitGenerator",
			generatorVersion: "2.3.4"
		);

		writer.WriteAutoGeneratedHeader();
		writer.WriteClass(
			new TypeDeclarationOptions("GeneratedType"),
			body =>
				body.WriteProperty(
					new PropertyDeclarationOptions("Value", new TypeReferenceOptions("string"))
				)
		);

		var result = writer.ToString();
		await Assert.That(result).Contains("HostKitGenerator (version 2.3.4)");
		await Assert.That(result).Contains("// Generated at ");
		await Assert.That(result).Contains("[global::Microsoft.CodeAnalysis.EmbeddedAttribute]");
		await Assert
			.That(result)
			.Contains("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]");
		await Assert
			.That(result)
			.Contains("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]");
		await Assert
			.That(result)
			.Contains(
				"[global::System.CodeDom.Compiler.GeneratedCode(\"HostKitGenerator\", \"2.3.4\")]"
			);
	}

	[Test]
	public async Task WriteConstructor_WithMultilineParameters_WritesInitializerOnNewLine()
	{
		var writer = new CodeWriter();
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
				"public Repository(\n"
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
		var writer = new CodeWriter(
			throwOnUnclosedScopes: false,
			generatorName: "HostKitGenerator",
			generatorVersion: "2.3.4"
		);

		writer.WriteField(
			new FieldDeclarationOptions("SectionName", new TypeReferenceOptions("string"))
			{
				Accessibility = TypeDeclarationAccessibility.Public,
				IsConst = true,
				Initializer = "\"TestingHostKit\"",
			}
		);

		var result = writer.ToString();
		await Assert.That(result).DoesNotContain("ExcludeFromCodeCoverageAttribute");
		await Assert.That(result).Contains("CompilerGeneratedAttribute");
		await Assert.That(result).Contains("GeneratedCode(");
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
