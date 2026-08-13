using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework.Testing.Generators;

public class AttributeDataModelGeneratorTests
{
	[Test]
	public async Task Generate_RequiredAttributeData_DefaultNamedAndNestedModel(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Microsoft.CodeAnalysis;
			using Purview.SourceGeneratorFramework.Testing.Generators;
			using System.ComponentModel.DataAnnotations;

			namespace Test
			{
				[GenerateAttributeDataModel(typeof(ValidationAttribute), MatchByInheritance = true)]
				public readonly partial record struct ValidationAttributeData(
					string? ErrorMessage,
					string? ErrorMessageResourceName,
					ITypeSymbol? ErrorMessageResourceType
				);

				[GenerateAttributeDataModel(typeof(RequiredAttribute))]
				public readonly partial record struct RequiredAttributeData(
					bool AllowEmptyStrings,
					[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
				);
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"RequiredAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("readonly partial record struct RequiredAttributeData");
		await Assert.That(generated).Contains("bool Exists");
		await Assert.That(generated).Contains("bool AllowEmptyStrings");
		await Assert
			.That(generated)
			.Contains("global::Test.ValidationAttributeData ValidationAttribute");
		await Assert
			.That(generated)
			.Contains(
				"attributeData.TryGetNamedArgument<bool>(\"AllowEmptyStrings\", out var allowEmptyStrings)"
			);
		await Assert
			.That(generated)
			.Contains(
				"var validationAttribute = global::Test.ValidationAttributeData.FromAttributeData(attributeData)"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly RequiredAttributeData Empty = new(false, default(bool), default(global::Test.ValidationAttributeData))"
			);
	}

	[Test]
	public async Task Generate_LengthAttributeData_CtorIndex(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Testing.Generators;
			using System.ComponentModel.DataAnnotations;

			namespace Test
			{
				[GenerateAttributeDataModel(typeof(LengthAttribute))]
				public readonly partial record struct LengthAttributeData(
					[AttributeCtorProperty(0)] int MinimumLength,
					[AttributeCtorProperty(1)] int MaximumLength
				);
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"LengthAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("readonly partial record struct LengthAttributeData");
		await Assert.That(generated).Contains("int MinimumLength");
		await Assert.That(generated).Contains("int MaximumLength");
		await Assert
			.That(generated)
			.Contains("attributeData.TryGetConstructorArgument<int>(0, out var minimumLength)");
		await Assert
			.That(generated)
			.Contains("attributeData.TryGetConstructorArgument<int>(1, out var maximumLength)");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly LengthAttributeData Empty = new(false, default(int), default(int))"
			);
	}

	[Test]
	public async Task Generate_StringLengthAttributeData_CtorNameAndDefaultValue(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Microsoft.CodeAnalysis;
			using Purview.SourceGeneratorFramework.Testing.Generators;
			using System.ComponentModel.DataAnnotations;

			namespace Test
			{
				[GenerateAttributeDataModel(typeof(ValidationAttribute), MatchByInheritance = true)]
				public readonly partial record struct ValidationAttributeData(
					string? ErrorMessage
				);

				[GenerateAttributeDataModel(typeof(StringLengthAttribute))]
				public readonly partial record struct StringLengthAttributeData(
					[AttributeCtorProperty("maximumLength", DefaultValue = int.MaxValue)] int MaximumLength,
					int MinimumLength,
					[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
				);
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"StringLengthAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("int MaximumLength");
		await Assert.That(generated).Contains("int MinimumLength");
		await Assert
			.That(generated)
			.Contains(
				"var maximumLength = attributeData.GetConstructorArgument<int>(\"maximumLength\", 2147483647)"
			);
		await Assert
			.That(generated)
			.Contains(
				"attributeData.TryGetNamedArgument<int>(\"MinimumLength\", out var minimumLength)"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly StringLengthAttributeData Empty = new(false, default(int), default(int), default(global::Test.ValidationAttributeData))"
			);
	}

	[Test]
	public async Task Generate_HostKitAttribute_CtorAndNamedCombined(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Testing.Generators;

			namespace Test
			{
				[GenerateAttributeDataModel("Test.HostKitAttribute")]
				public readonly partial record struct HostKitAttributeData(
					[AttributeCtorProperty("name")] string? Name,
					string? ExtensionMethodName,
					[AttributeCtorProperty("generateOptions")] [AttributeNamedProperty(DefaultValue = true)] bool GenerateOptions
				);

				public class HostKitAttribute : System.Attribute
				{
					public HostKitAttribute() { }
					public HostKitAttribute(string name, bool generateOptions = true) { }
					public HostKitAttribute(bool generateOptions) { }
				}
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"HostKitAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("readonly partial record struct HostKitAttributeData");
		await Assert.That(generated).Contains("string? Name");
		await Assert.That(generated).Contains("bool GenerateOptions");
		await Assert
			.That(generated)
			.Contains("attributeData.TryGetConstructorArgument<string>(\"name\", out var name)");
		await Assert
			.That(generated)
			.Contains(
				"if (!attributeData.TryGetConstructorArgument<bool>(\"generateOptions\", out generateOptions))"
			);
		await Assert
			.That(generated)
			.Contains(
				"if (!attributeData.TryGetNamedArgument<bool>(\"GenerateOptions\", out generateOptions))"
			);
		await Assert.That(generated).Contains("generateOptions = true");
	}

	[Test]
	public async Task Generate_AutoDiscover_DiscoversNamedArguments(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Testing.Generators;
			using System.ComponentModel.DataAnnotations;

			namespace Test
			{
				[GenerateAttributeDataModel(typeof(RequiredAttribute), AutoDiscover = true)]
				public readonly partial record struct RequiredAttributeData;
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"RequiredAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("readonly partial record struct RequiredAttributeData");
		await Assert.That(generated).Contains("bool AllowEmptyStrings");
		await Assert
			.That(generated)
			.Contains(
				"attributeData.TryGetNamedArgument<bool>(\"AllowEmptyStrings\", out var allowEmptyStrings)"
			);
		await Assert
			.That(generated)
			.Contains(
				"public static readonly RequiredAttributeData Empty = new(false, default(bool))"
			);
	}

	[Test]
	public async Task Generate_Exclude_SkipsProperty(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Testing.Generators;
			using System.ComponentModel.DataAnnotations;

			namespace Test
			{
				[GenerateAttributeDataModel(typeof(RequiredAttribute))]
				public readonly partial record struct RequiredAttributeData(
					bool AllowEmptyStrings,
					[AttributeExcludeProperty] int Ignored
				);
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"RequiredAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("bool AllowEmptyStrings");
		await Assert.That(generated).DoesNotContain("int Ignored");
		await Assert
			.That(generated)
			.Contains(
				"public static readonly RequiredAttributeData Empty = new(false, default(bool))"
			);
	}

	[Test]
	public async Task Generate_NestedModelNotGenerated_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Testing.Generators;
			using System.ComponentModel.DataAnnotations;

			namespace Test
			{
				[GenerateAttributeDataModel(typeof(RequiredAttribute))]
				public readonly partial record struct RequiredAttributeData(
					[AttributeNestedModelProperty] NotAModel NotAModel
				);

				public readonly partial record struct NotAModel;
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		await Assert.That(result.Result.Diagnostics).Contains(d => d.Id == "ADM0004");
	}

	[Test]
	public async Task Generate_StringTargetAttributeData_NamedArgument(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Testing.Generators;
			using System.ComponentModel.DataAnnotations;

			namespace Test
			{
				[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.RequiredAttribute")]
				public readonly partial record struct RequiredAttributeData(
					bool AllowEmptyStrings
				);
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"RequiredAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("readonly partial record struct RequiredAttributeData");
		await Assert.That(generated).Contains("bool AllowEmptyStrings");
		await Assert
			.That(generated)
			.Contains(
				"attributeData.TryGetNamedArgument<bool>(\"AllowEmptyStrings\", out var allowEmptyStrings)"
			);
		await Assert
			.That(generated)
			.Contains("new(\"RequiredAttribute\", \"System.ComponentModel.DataAnnotations\")");
	}

	[Test]
	public async Task Generate_StringTarget_WithAutoDiscover_ReportsDiagnostic(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Testing.Generators;
			using System.ComponentModel.DataAnnotations;

			namespace Test
			{
				[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.RequiredAttribute", AutoDiscover = true)]
				public readonly partial record struct RequiredAttributeData;
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		await Assert.That(result.Result.Diagnostics).Contains(d => d.Id == "ADM0007");
	}

	[Test]
	public async Task Generate_StringTarget_ConstructorArrayOfTypedConstant(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System.Collections.Immutable;
			using Microsoft.CodeAnalysis;
			using Purview.SourceGeneratorFramework.Testing.Generators;

			namespace Test
			{
				[GenerateAttributeDataModel("TestAttribute")]
				public readonly partial record struct TestAttributeData(
					[AttributeCtorProperty(0)] ImmutableArray<TypedConstant> Values
				);

				public class TestAttribute : System.Attribute
				{
					public TestAttribute(params object?[] values) { }
				}
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"TestAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("readonly partial record struct TestAttributeData");
		await Assert
			.That(generated)
			.Contains(
				"global::System.Collections.Immutable.ImmutableArray<global::Microsoft.CodeAnalysis.TypedConstant> Values"
			);
		await Assert
			.That(generated)
			.Contains(
				"attributeData.TryGetConstructorArgument<global::System.Collections.Immutable.ImmutableArray<global::Microsoft.CodeAnalysis.TypedConstant>>(0, out var values)"
			);
	}

	[Test]
	public async Task Generate_NonNullableReferenceType_GeneratesDefaultSuppress(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Testing.Generators;
			using System.ComponentModel.DataAnnotations;

			namespace Test
			{
				[GenerateAttributeDataModel(typeof(RequiredAttribute))]
				public readonly partial record struct RequiredAttributeData(
					string ErrorMessage
				);
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"RequiredAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains(
				"public static readonly RequiredAttributeData Empty = new(false, default(string)!)"
			);
	}

	[Test]
	[SuppressMessage("Design", "CA1506:Avoid excessive class coupling")]
	public async Task Generate_ResourceDefinitionAttributeData_PullsDataOut(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Microsoft.CodeAnalysis;
			using Purview.SourceGeneratorFramework.Testing.Generators;

			namespace Aspire.Hosting.ApplicationModel
			{
				public interface IResource { }
			}

			namespace Test
			{
				[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
				public class ResourceDefinitionAttribute : global::System.Attribute
				{
					public ResourceDefinitionAttribute() { }
					public ResourceDefinitionAttribute(string name, string propertyName) { Name = name; PropertyName = propertyName; }
					public ResourceDefinitionAttribute(string name) { Name = name; }
					public string? Name { get; set; }
					public string? PropertyName { get; set; }
				}

				[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
				public class ResourceDefinitionAttribute<TResource> : ResourceDefinitionAttribute
					where TResource : class, global::Aspire.Hosting.ApplicationModel.IResource
				{
					public ResourceDefinitionAttribute() { }
					public ResourceDefinitionAttribute(string name, string propertyName) : base(name, propertyName) { }
					public ResourceDefinitionAttribute(string name) : base(name) { }
				}

				[GenerateAttributeDataModel(typeof(ResourceDefinitionAttribute), MatchByInheritance = true)]
				public readonly partial record struct ResourceDefinitionAttributeData(
					[AttributeCtorProperty("name")] string? Name,
					[AttributeCtorProperty("propertyName")] string? PropertyName,
					[AttributeGenericTypeArgumentProperty] INamedTypeSymbol? AspireResourceType
				);

				[ResourceDefinition("myResource", "MyResource")]
				public class MyResource : global::Aspire.Hosting.ApplicationModel.IResource { }

				[ResourceDefinition<MyResource>("myGenericResource", "MyGenericResource")]
				public class MyGenericResource : global::Aspire.Hosting.ApplicationModel.IResource { }
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);
		result.AssertNoGenerationExceptions().AssertNoLogErrors();

		var generated = await GetGeneratedStringAsync(
			result,
			"ResourceDefinitionAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("readonly partial record struct ResourceDefinitionAttributeData");
		await Assert.That(generated).Contains("string? Name");
		await Assert.That(generated).Contains("string? PropertyName");
		await Assert.That(generated).Contains("INamedTypeSymbol? AspireResourceType");
		await Assert
			.That(generated)
			.Contains(
				"attributeData.TryGetGenericTypeArgument<global::Microsoft.CodeAnalysis.INamedTypeSymbol>(0, out var aspireResourceType)"
			);
		await Assert
			.That(generated)
			.Contains(
				"(!TargetAttribute.Equals(attributeData.AttributeClass) && !global::Purview.SourceGeneratorFramework.Helpers.TypeHelpers.InheritsFrom(attributeData.AttributeClass, TargetAttribute))"
			);

		var runtimeSource = """
			using Microsoft.CodeAnalysis;

			namespace Aspire.Hosting.ApplicationModel
			{
				public interface IResource { }
			}

			namespace Test
			{
				[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
				public class ResourceDefinitionAttribute : global::System.Attribute
				{
					public ResourceDefinitionAttribute() { }
					public ResourceDefinitionAttribute(string name, string propertyName) { Name = name; PropertyName = propertyName; }
					public ResourceDefinitionAttribute(string name) { Name = name; }
					public string? Name { get; set; }
					public string? PropertyName { get; set; }
				}

				[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
				public class ResourceDefinitionAttribute<TResource> : ResourceDefinitionAttribute
					where TResource : class, global::Aspire.Hosting.ApplicationModel.IResource
				{
					public ResourceDefinitionAttribute() { }
					public ResourceDefinitionAttribute(string name, string propertyName) : base(name, propertyName) { }
					public ResourceDefinitionAttribute(string name) : base(name) { }
				}

				public readonly partial record struct ResourceDefinitionAttributeData(
					string? Name,
					string? PropertyName,
					INamedTypeSymbol? AspireResourceType
				);

				[ResourceDefinition("myResource", "MyResource")]
				public class MyResource : global::Aspire.Hosting.ApplicationModel.IResource { }

				[ResourceDefinition<MyResource>("myGenericResource", "MyGenericResource")]
				public class MyGenericResource : global::Aspire.Hosting.ApplicationModel.IResource { }
			}
			""";

		var generatedTree = result.GetGeneratedTree(
			"ResourceDefinitionAttributeData.AttributeDataModel.g.cs"
		);
		var generatedText = (await generatedTree!.GetTextAsync(cancellationToken)).ToString();
		var runtimeSyntaxTree = CSharpSyntaxTree.ParseText(
			runtimeSource,
			cancellationToken: cancellationToken
		);
		var generatedSyntaxTree = CSharpSyntaxTree.ParseText(
			generatedText,
			cancellationToken: cancellationToken
		);

		var trustedAssemblies = (
			(string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? ""
		).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

		var references = trustedAssemblies
			.Where(path =>
				!path.EndsWith(
					"Purview.SourceGeneratorFramework.Testing.Generators.dll",
					StringComparison.OrdinalIgnoreCase
				)
			)
			.Select(path => MetadataReference.CreateFromFile(path))
			.ToArray();

		var runtimeCompilation = CSharpCompilation.Create(
			"RuntimeAssembly",
			[runtimeSyntaxTree, generatedSyntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		var runtimeCompilationDiagnostics = runtimeCompilation.GetDiagnostics(cancellationToken);
		var runtimeErrors = runtimeCompilationDiagnostics
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();
		await Assert.That(runtimeErrors).IsEmpty();

		await using var assemblyStream = new MemoryStream();
		var emitResult = runtimeCompilation.Emit(
			assemblyStream,
			cancellationToken: cancellationToken
		);
		await Assert.That(emitResult.Success).IsTrue();

		assemblyStream.Position = 0;
		var assembly = System.Reflection.Assembly.Load(assemblyStream.ToArray());

		var runtimeCompilationForAttributes = CSharpCompilation.Create(
			"AttributeAssembly",
			[runtimeSyntaxTree, generatedSyntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);
		var myResourceType = runtimeCompilationForAttributes.GetTypeByMetadataName(
			"Test.MyResource"
		);
		var myGenericResourceType = runtimeCompilationForAttributes.GetTypeByMetadataName(
			"Test.MyGenericResource"
		);
		await Assert.That(myResourceType).IsNotNull();
		await Assert.That(myGenericResourceType).IsNotNull();

		var resourceAttribute = myResourceType!
			.GetAttributes()
			.First(a => a.AttributeClass?.Name == "ResourceDefinitionAttribute");
		var genericResourceAttribute = myGenericResourceType!
			.GetAttributes()
			.First(a => a.AttributeClass?.Name == "ResourceDefinitionAttribute");

		var dataType = assembly.GetType("Test.ResourceDefinitionAttributeData");
		await Assert.That(dataType).IsNotNull();

		var fromAttributeDataMethod = dataType!
			.GetMethods()
			.First(m =>
				m.Name == "FromAttributeData"
				&& m.GetParameters().Length == 1
				&& m.GetParameters()[0].ParameterType == typeof(AttributeData)
			);

		var resourceData = fromAttributeDataMethod.Invoke(null, [resourceAttribute]);
		var genericResourceData = fromAttributeDataMethod.Invoke(null, [genericResourceAttribute]);

		var nameProperty = dataType.GetProperty("Name");
		var propertyNameProperty = dataType.GetProperty("PropertyName");
		var aspireResourceTypeProperty = dataType.GetProperty("AspireResourceType");

		await Assert.That(nameProperty!.GetValue(resourceData)).IsEqualTo("myResource");
		await Assert.That(propertyNameProperty!.GetValue(resourceData)).IsEqualTo("MyResource");
		await Assert.That(aspireResourceTypeProperty!.GetValue(resourceData)).IsNull();

		await Assert
			.That(nameProperty.GetValue(genericResourceData))
			.IsEqualTo("myGenericResource");
		await Assert
			.That(propertyNameProperty.GetValue(genericResourceData))
			.IsEqualTo("MyGenericResource");
		var aspireResourceType =
			aspireResourceTypeProperty.GetValue(genericResourceData) as INamedTypeSymbol;
		await Assert.That(aspireResourceType).IsNotNull();
		await Assert
			.That(SymbolEqualityComparer.Default.Equals(aspireResourceType, myResourceType))
			.IsTrue();
	}

	[Test]
	public async Task Generate_SystemTypeProperty_MapsToINamedTypeSymbol(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Microsoft.CodeAnalysis;
			using Purview.SourceGeneratorFramework.Testing.Generators;

			namespace Test
			{
			[GenerateAttributeDataModel(typeof(TestingAttribute))]
			public readonly partial record struct TestingAttributeData(
				[AttributeCtorProperty("typeThing")] [AttributeNamedProperty] INamedTypeSymbol? TypeThing
			);

				public class TestingAttribute : System.Attribute
				{
					public TestingAttribute(Type typeThing) { TypeThing = typeThing; }
					public Type? TypeThing { get; set; }
				}
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var options = new SourceGeneratorTestOptions { CompileToAssembly = false };
		var result = await runner.RunAsync(source, options, cancellationToken);
		result.AssertNoGenerationExceptions().AssertNoLogErrors();

		var generated = await GetGeneratedStringAsync(
			result,
			"TestingAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert
			.That(generated)
			.Contains("readonly partial record struct TestingAttributeData");
		await Assert.That(generated).Contains("INamedTypeSymbol? TypeThing");
		await Assert
			.That(generated)
			.Contains(
				"attributeData.TryGetConstructorArgument<global::Microsoft.CodeAnalysis.INamedTypeSymbol>(\"typeThing\", out typeThing)"
			);
		await Assert
			.That(generated)
			.Contains(
				"attributeData.TryGetNamedArgument<global::Microsoft.CodeAnalysis.INamedTypeSymbol>(\"TypeThing\", out typeThing)"
			);
	}

	[Test]
	public async Task Generate_NullableReferenceTypeWithMultipleSources_DoesNotEmitCS8600(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Testing.Generators;

			namespace Test
			{
				[GenerateAttributeDataModel("Test.HostKitAttribute")]
				public readonly partial record struct HostKitAttributeData(
					[AttributeNamedProperty]
					[AttributeCtorProperty("name")]
						string? Name,
					string? ExtensionMethodName,
					[AttributeNamedProperty]
					[AttributeCtorProperty("generateOptions", DefaultValue = true)]
						bool GenerateOptions
				);

				public class HostKitAttribute : System.Attribute
				{
					public HostKitAttribute() { }
					public HostKitAttribute(string name, bool generateOptions = true) { }
					public HostKitAttribute(bool generateOptions) { }
				}
			}
			""";

		var runner = new SourceGeneratorTestRunner<AttributeDataModelGenerator>();
		var result = await runner.RunAsync(source, cancellationToken: cancellationToken);

		var generated = await GetGeneratedStringAsync(
			result,
			"HostKitAttributeData.AttributeDataModel.g.cs",
			cancellationToken
		);

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("string? name;");
		await Assert.That(generated).DoesNotContain("string name;");
	}

	static async Task<string?> GetGeneratedStringAsync(
		DriverRunResult result,
		string fileName,
		CancellationToken cancellationToken
	)
	{
		var tree = result.GetGeneratedTree(fileName);
		return tree is null ? null : (await tree.GetTextAsync(cancellationToken)).ToString();
	}
}
