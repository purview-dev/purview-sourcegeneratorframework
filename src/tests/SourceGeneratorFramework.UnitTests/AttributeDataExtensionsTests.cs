using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Extensions;

namespace Purview.SourceGeneratorFramework;

public class AttributeDataExtensionsTests
{
	[SuppressMessage(
		"Design",
		"CA1506:Avoid excessive class coupling",
		Justification = "Test helper"
	)]
	static async Task<AttributeData> GetAttributeAsync(
		string source,
		string typeName,
		string attributeTypeName,
		CancellationToken cancellationToken
	)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			new[] { syntaxTree },
			[
				MetadataReference.CreateFromFile(typeof(CodeWriter).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(ITestOutput).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location),
			],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);
		var model = compilation.GetSemanticModel(syntaxTree);
		var root = await syntaxTree.GetRootAsync(cancellationToken);
		var typeDeclaration = root.DescendantNodes()
			.OfType<TypeDeclarationSyntax>()
			.First(t => t.Identifier.ValueText == typeName);
		var symbol = model.GetDeclaredSymbol(
			typeDeclaration,
			cancellationToken: cancellationToken
		)!;

		return symbol.GetAttributes().First(a => a.AttributeClass?.Name == attributeTypeName);
	}

	[Test]
	public async Task GetNamedArgument_StringValue_ReturnsValue(CancellationToken cancellationToken)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute
			{
				public string Name { get; set; } = "";
			}

			[Test(Name = "Hello")]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var value = attribute.GetNamedArgument<string>("Name");

		await Assert.That(value).IsEqualTo("Hello");
	}

	[Test]
	public async Task GetNamedArgument_Missing_ReturnsDefault(CancellationToken cancellationToken)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute { }

			[Test]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var value = attribute.GetNamedArgument("Missing", "default");

		await Assert.That(value).IsEqualTo("default");
	}

	[Test]
	public async Task TryGetNamedArgument_Present_ReturnsTrue(CancellationToken cancellationToken)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute
			{
				public int Count { get; set; }
			}

			[Test(Count = 42)]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var found = attribute.TryGetNamedArgument<int>("Count", out var value);

		await Assert.That(found).IsTrue();
		await Assert.That(value).IsEqualTo(42);
	}

	[Test]
	public async Task GetConstructorArgument_PositionalValue_ReturnsValue(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute
			{
				public TestAttribute(string name) { }
			}

			[Test("Hello")]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var value = attribute.GetConstructorArgument<string>(0);

		await Assert.That(value).IsEqualTo("Hello");
	}

	[Test]
	public async Task GetConstructorArgument_ParameterName_ReturnsValue(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute
			{
				public TestAttribute(string name) { }
			}

			[Test("Hello")]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var value = attribute.GetConstructorArgument<string>("name");

		await Assert.That(value).IsEqualTo("Hello");
	}

	[Test]
	[Arguments("missing")]
	[Arguments("unavailable")]
	[Arguments("doesntExist")]
	public async Task GetConstructorArgument_ParameterNameIsOutOfRange_ReturnsDefault(
		string parameterName,
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute { }

			[Test]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var value = attribute.GetConstructorArgument(parameterName, "default-value");

		await Assert.That(value).IsEqualTo("default-value");
	}

	[Test]
	[Arguments(0)]
	[Arguments(1)]
	[Arguments(3)]
	[Arguments(5)]
	[Arguments(10)]
	[Arguments(50)]
	[Arguments(100)]
	public async Task GetConstructorArgument_IndexIsOutOfRange_ReturnsDefault(
		int index,
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute { }

			[Test]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var value = attribute.GetConstructorArgument(index, "default");

		await Assert.That(value).IsEqualTo("default");
	}

	[Test]
	public async Task As_EnumValue_ReturnsEnumValue(CancellationToken cancellationToken)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute
			{
				public TestAttribute(AttributeTargets targets) { }
			}

			[Test(AttributeTargets.Class | AttributeTargets.Struct)]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var value = attribute.ConstructorArguments[0].As<AttributeTargets>();

		await Assert.That(value).IsEqualTo(AttributeTargets.Class | AttributeTargets.Struct);
	}

	[Test]
	public async Task As_ImmutableArrayTypedConstant_ReturnsValues(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System;
			using Microsoft.CodeAnalysis;
			using System.Collections.Immutable;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute
			{
				public TestAttribute(params object?[] values) { }
			}

			[Test("one", 2, true)]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var value = attribute.ConstructorArguments[0].As<ImmutableArray<TypedConstant>>();

		await Assert.That(value).Count().IsEqualTo(3);
		await Assert.That(value[0].Value).IsEqualTo("one");
		await Assert.That(value[1].Value).IsEqualTo(2);
		await Assert.That((bool?)value[2].Value).IsTrue();
	}

	[Test]
	public async Task TryGetGenericTypeArgument_ByIndex_ReturnsType(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute<T> : Attribute { }

			[Test<string>]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var found = attribute.TryGetGenericTypeArgument<INamedTypeSymbol>(0, out var value);

		await Assert.That(found).IsTrue();
		await Assert.That(value?.Name).IsEqualTo("String");
	}

	[Test]
	public async Task TryGetGenericTypeArgument_ByName_ReturnsType(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute<TValue> : Attribute { }

			[Test<int>]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var found = attribute.TryGetGenericTypeArgument<INamedTypeSymbol>("TValue", out var value);

		await Assert.That(found).IsTrue();
		await Assert.That(value?.Name).IsEqualTo("Int32");
	}

	[Test]
	public async Task TryGetGenericTypeArgument_NonGeneric_ReturnsNull(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute { }

			[Test]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var found = attribute.TryGetGenericTypeArgument<INamedTypeSymbol>(0, out var value);

		await Assert.That(found).IsFalse();
		await Assert.That(value).IsNull();
	}

	[Test]
	public async Task GetConstructorArgument_SystemType_ReturnsINamedTypeSymbol(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System;
			using Microsoft.CodeAnalysis;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute
			{
				public TestAttribute(Type type) { }
			}

			[Test(typeof(string))]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var value = attribute.GetConstructorArgument<INamedTypeSymbol>(0);

		await Assert.That(value).IsNotNull();
		await Assert.That(value?.Name).IsEqualTo("String");
	}

	[Test]
	public async Task GetNamedArgument_SystemType_ReturnsINamedTypeSymbol(
		CancellationToken cancellationToken
	)
	{
		var source = """
			using System;
			using Microsoft.CodeAnalysis;

			[AttributeUsage(AttributeTargets.Class)]
			public class TestAttribute : Attribute
			{
				public Type? TypeThing { get; set; }
			}

			[Test(TypeThing = typeof(string))]
			public class MyClass { }
			""";
		var attribute = await GetAttributeAsync(
			source,
			"MyClass",
			"TestAttribute",
			cancellationToken
		);

		var value = attribute.GetNamedArgument<INamedTypeSymbol>("TypeThing");

		await Assert.That(value).IsNotNull();
		await Assert.That(value?.Name).IsEqualTo("String");
	}
}
