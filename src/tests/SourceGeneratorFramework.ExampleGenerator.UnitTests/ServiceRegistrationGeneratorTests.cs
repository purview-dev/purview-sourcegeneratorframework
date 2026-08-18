using Purview.SourceGeneratorFramework.Examples;
using Purview.SourceGeneratorFramework.Helpers;
using Purview.SourceGeneratorFramework.Testing.TUnit;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

public class ServiceRegistrationGeneratorTests
	: TUnitSourceGeneratorTestBase<ServiceRegistrationGenerator, ServiceRegistrationTestOptions>
{
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

		var tree = result.GetGeneratedTree("ServiceCollectionExtensions.g.cs");
		var generated = tree is null ? null : (await tree.GetTextAsync(cancellationToken)).ToString();

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static class ServiceCollectionExtensions");
		await Assert
			.That(generated)
			.Contains(
				"public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddExampleServices"
			);
		await Assert
			.That(generated)
			.Contains(
				"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::Test.MyService>(services);"
			);
		await Assert
			.That(generated)
			.Contains(
				"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<global::Test.OtherService>(services);"
			);
		await Assert.That(generated).Contains("// Service name: NamedService");
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

		var tree = result.GetGeneratedTree("ServiceCollectionExtensions.g.cs");
		await Assert.That(tree).IsNull();
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
				AnalyzerConfigOptions =
				{
					[
						IncrementalPipeline.BuildProperty
							+ ServiceRegistrationGeneratorPropertyLibrary.EmitServiceRegistrationInfo
					] = "true",
				},
			},
			cancellationToken
		);

		var tree = result.GetGeneratedTree("ServiceInfo.g.cs");
		var generated = tree is null ? null : (await tree.GetTextAsync(cancellationToken)).ToString();

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("public static class ServiceInfo");
		await Assert.That(generated).Contains("public static class MyService");
		await Assert.That(generated).Contains("public static string Name => \"MyService\";");
		await Assert.That(generated).Contains("public static string Lifetime => \"Transient\";");
		await Assert
			.That(generated)
			.Contains("public static global::System.Type Type => typeof(global::Test.MyService);");
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

		var attributeTree = result.GetGeneratedTree("GenerateServiceAttribute.g.cs");
		var attributeSource = attributeTree is null
			? null
			: (await attributeTree.GetTextAsync(cancellationToken)).ToString();

		await Assert.That(attributeSource).IsNotNull();
		await Assert.That(attributeSource).Contains("public enum ServiceLifetime");
		await Assert
			.That(attributeSource)
			.Contains("public sealed class GenerateServiceAttribute : global::System.Attribute");
		await Assert
			.That(attributeSource)
			.Contains("public global::Purview.SourceGeneratorFramework.Examples.ServiceLifetime Lifetime { get; }");
	}
}
