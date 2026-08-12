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
