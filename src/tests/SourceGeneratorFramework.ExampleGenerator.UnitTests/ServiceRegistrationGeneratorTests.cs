using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.SourceGeneratorFramework.Examples;
using Purview.SourceGeneratorFramework.Testing;
using Purview.SourceGeneratorFramework.Testing.TUnit;
using Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

public class ServiceRegistrationGeneratorTests
	: TUnitSourceGeneratorTestBase<ServiceRegistrationGenerator, ServiceRegistrationTestOptions>
{
	static readonly TypeReference IServiceCollection = new(
		new TypeIdentity("IServiceCollection", "Microsoft.Extensions.DependencyInjection")
	);

	[Test]
	public async Task GenerateAsync_DefaultOptions_CapturesFrameworkLoggingThroughTUnitSink(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync("public sealed class UnrelatedType { }", cancellationToken);

		await Assert.That(result.LogEntries).IsNotEmpty();
		await Assert
			.That(
				result.LogEntries.Any(entry => entry.Message.Contains("generation context", StringComparison.Ordinal))
			)
			.IsTrue();
	}

	[Test]
	public async Task GenerateAsync_LoggingDisabled_DoesNotCaptureEntriesThroughTUnitSink(
		CancellationToken cancellationToken
	)
	{
		var result = await GenerateAsync(
			"public sealed class UnrelatedType { }",
			new ServiceRegistrationTestOptions { EnableLogging = false },
			cancellationToken
		);

		await Assert.That(result.LogEntries).IsEmpty();
	}

	[Test]
	public async Task GenerateService_GeneratesServiceCollectionExtensions(CancellationToken cancellationToken)
	{
		var source = """
			namespace Test;

			[GenerateService]
			public class MyService { }

			[GenerateService(ServiceLifetime.Scoped, Name = "NamedService")]
			public class OtherService { }
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var query = result.Generated();
		var method = query.GetMethod("AddExampleServices");
		var @class = query.GetClass("ServiceCollectionExtensions");

		await Assert.That(@class.Identifier.ValueText).IsEqualTo("ServiceCollectionExtensions");
		await Assert.That(@class.Modifiers.Any(static m => m.IsKind(SyntaxKind.StaticKeyword))).IsTrue();
		await Assert.That(method.Identifier.ValueText).IsEqualTo("AddExampleServices");
		await Assert.That(method.HasParameters(query, IServiceCollection)).IsTrue();

		var methodText = method.ToString();
		await Assert
			.That(methodText)
			.Contains(
				"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::Test.MyService>(services);"
			);
		await Assert
			.That(methodText)
			.Contains(
				"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<global::Test.OtherService>(services);"
			);
		await Assert.That(methodText).Contains("// Service name: NamedService");
	}

	[Test]
	public async Task GenerateService_MethodSignature_MatchesThisParameter(CancellationToken cancellationToken)
	{
		var source = """
			namespace Test;

			[GenerateService]
			public class MyService { }
			""";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result.Generated().HasMethod("AddExampleServices", IServiceCollection)).IsTrue();
		await Assert.That(result.Generated().HasReturnType("AddExampleServices", IServiceCollection)).IsTrue();
	}

	[Test]
	public async Task GenerateService_GivenNullableDisabled_OmitsNullableDirective(CancellationToken cancellationToken)
	{
		var source = """
			namespace Test;

			[GenerateService]
			public class MyService { }
			""";

		var result = await GenerateAsync(
			source,
			new ServiceRegistrationTestOptions { NullableContextOptions = NullableContextOptions.Disable },
			cancellationToken
		);

		var generated = (
			await result.Generated().GetSyntaxTree("ServiceCollectionExtensions.g.cs").GetTextAsync(cancellationToken)
		).ToString();

		await Assert.That(generated).DoesNotContain("#nullable enable");
	}

	[Test]
	public async Task GenerateService_GivenNullableEnabled_WritesNullableDirective(CancellationToken cancellationToken)
	{
		var source = """
			namespace Test;

			[GenerateService]
			public class MyService { }
			""";

		var result = await GenerateAsync(
			source,
			new ServiceRegistrationTestOptions { NullableContextOptions = NullableContextOptions.Enable },
			cancellationToken
		);

		var generated = (
			await result.Generated().GetSyntaxTree("ServiceCollectionExtensions.g.cs").GetTextAsync(cancellationToken)
		).ToString();

		await Assert.That(generated).Contains("#nullable enable");
	}

	[Test]
	public async Task GenerateService_GivenNullableDisabled_PostInitializationOutputKeepsAnnotation(
		CancellationToken cancellationToken
	)
	{
		var source = """
			namespace Test;

			[GenerateService]
			public class MyService { }
			""";

		var result = await GenerateAsync(
			source,
			new ServiceRegistrationTestOptions { NullableContextOptions = NullableContextOptions.Disable },
			cancellationToken
		);

		// Post-initialization outputs have no compilation context, so the unknown nullable state falls back to
		// keeping the annotation and emitting the #nullable enable directive.
		var attribute = result.Generated().GetClass("GenerateServiceAttribute");
		await Assert.That(attribute.AttributeLists).IsNotEmpty();

		var nameProperty = result.Generated().GetProperty("Name");
		await Assert.That(nameProperty.Type.ToString()).IsEqualTo("string?");
	}

	[Test]
	public async Task GenerateService_Disabled_ProducesNoOutput(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Examples;

			namespace Test
			{
				[GenerateService]
				public class MyService { }
			}
			""";

		var result = await GenerateAsync(
			source,
			new ServiceRegistrationTestOptions { DisableSourceGeneratorValue = true },
			cancellationToken
		);

		await Assert.That(result.Generated().HasSyntaxTree("ServiceCollectionExtensions.g.cs")).IsFalse();
	}

	[Test]
	public async Task GenerateService_EmitServiceInfo_GeneratesServiceInfo(CancellationToken cancellationToken)
	{
		var source = """
			using Purview.SourceGeneratorFramework.Examples;

			namespace Test
			{
				[GenerateService(ServiceLifetime.Transient, Name = "MyService")]
				public class MyService { }
			}
			""";

		var result = await GenerateAsync(
			source,
			new ServiceRegistrationTestOptions
			{
				AnalyzerConfigOptions = new Dictionary<string, string>
				{
					{ PropertyLibrary.EmitServiceRegistrationInfo, "true" },
				}.ToImmutableDictionary(),
			},
			cancellationToken
		);

		var query = result.Generated();
		var serviceInfoTree = query.GetSyntaxTree("ServiceInfo.g.cs");
		var serviceInfoQuery = new CodeQuery([serviceInfoTree], result.CompilationResult.Compilation);

		await Assert.That(query.GetClass("ServiceInfo").Identifier.ValueText).IsEqualTo("ServiceInfo");
		await Assert.That(serviceInfoQuery.GetClass("MyService").Identifier.ValueText).IsEqualTo("MyService");
		await Assert.That(serviceInfoQuery.GetProperty("Name").ExpressionBody!.ToString()).Contains("MyService");
		await Assert.That(serviceInfoQuery.GetProperty("Lifetime").ExpressionBody!.ToString()).Contains("Transient");
		await Assert
			.That(serviceInfoQuery.GetProperty("Type").ExpressionBody!.ToString())
			.Contains("typeof(global::Test.MyService)");
	}

	[Test]
	public async Task GenerateService_GeneratesAttributeAndEnum(CancellationToken cancellationToken)
	{
		var source = """
			namespace Test;

			[GenerateService]
			public class MyService { }
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var query = result.Generated();
		var attribute = query.GetClass("GenerateServiceAttribute");
		var lifetime = query.GetEnum("ServiceLifetime");

		await Assert.That(attribute.BaseList!.ToString()).Contains("global::System.Attribute");
		await Assert.That(lifetime.Members.Count).IsEqualTo(3);
		await Assert.That(query.GetProperty("Lifetime").Type.ToString()).Contains("ServiceLifetime");
	}

	[Test]
	public async Task GenerateService_AssertionExtensions_ReturnSyntaxNodes(CancellationToken cancellationToken)
	{
		var source = """
			namespace Test;

			[GenerateService]
			public class MyService { }
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var method = await Assert.That(result).HasGeneratedMethod("AddExampleServices");
		await Assert.That(method.Identifier.ValueText).IsEqualTo("AddExampleServices");

		var @class = await Assert.That(result).HasGeneratedClass("ServiceCollectionExtensions");
		await Assert.That(@class.Identifier.ValueText).IsEqualTo("ServiceCollectionExtensions");

		var attribute = await Assert.That(result).HasGeneratedClass("GenerateServiceAttribute");
		await Assert.That(attribute.BaseList!.ToString()).Contains("global::System.Attribute");

		var nameProperty = await Assert.That(result).HasGeneratedProperty("Name");
		await Assert.That(nameProperty.Identifier.ValueText).IsEqualTo("Name");

		var lifetime = await Assert.That(result).HasGeneratedMethod("AddExampleServices", [IServiceCollection]);
		await Assert.That(lifetime.ParameterList.Parameters.Count).IsEqualTo(1);

		await Assert.That(result).HasGeneratedSyntaxTree("ServiceCollectionExtensions.g.cs");
	}
}
