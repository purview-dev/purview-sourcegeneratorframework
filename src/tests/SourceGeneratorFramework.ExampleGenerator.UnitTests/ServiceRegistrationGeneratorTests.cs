using Purview.SourceGeneratorFramework.Examples;
using Purview.SourceGeneratorFramework.Helpers;
using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

public class ServiceRegistrationGeneratorTests
{
	[Test]
	public async Task GenerateService_GeneratesServiceCollectionExtensions()
	{
		var source = """
			using Purview.SourceGeneratorFramework.Examples;

			namespace Test
			{
				[GenerateService]
				public class MyService { }

				[GenerateService(ServiceLifetime.Scoped, Name = "NamedService")]
				public class OtherService { }
			}
			""";

		var runner = new SourceGeneratorTestRunner<ServiceRegistrationGenerator>();
		var result = await runner.RunAsync(
			source,
			new SourceGeneratorTestOptions
			{
				AdditionalAssemblyTypes = [typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection)],
			}
		);

		var tree = result.GetGeneratedTree("ServiceCollectionExtensions.g.cs");
		var generated = tree is null ? null : (await tree.GetTextAsync()).ToString();

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
	public async Task GenerateService_Disabled_ProducesNoOutput()
	{
		var source = """
			using Purview.SourceGeneratorFramework.Examples;

			namespace Test
			{
				[GenerateService]
				public class MyService { }
			}
			""";

		var runner = new SourceGeneratorTestRunner<ServiceRegistrationGenerator>();
		var result = await runner.RunAsync(
			source,
			new SourceGeneratorTestOptions
			{
				DisableSourceGeneratorPropertyName =
					ServiceRegistrationGeneratorPropertyLibrary.DisableServiceRegistrationGenerator,
				DisableSourceGeneratorValue = true,
				AdditionalAssemblyTypes = [typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection)],
			}
		);

		var tree = result.GetGeneratedTree("ServiceCollectionExtensions.g.cs");
		await Assert.That(tree).IsNull();
	}

	[Test]
	public async Task GenerateService_EmitServiceInfo_GeneratesServiceInfo()
	{
		var source = """
			using Purview.SourceGeneratorFramework.Examples;

			namespace Test
			{
				[GenerateService(ServiceLifetime.Transient, Name = "MyService")]
				public class MyService { }
			}
			""";

		var runner = new SourceGeneratorTestRunner<ServiceRegistrationGenerator>();
		var result = await runner.RunAsync(
			source,
			new SourceGeneratorTestOptions
			{
				AnalyzerConfigOptions =
				{
					[
						IncrementalPipeline.BuildProperty
							+ ServiceRegistrationGeneratorPropertyLibrary.EmitServiceRegistrationInfo
					] = "true",
				},
				AdditionalAssemblyTypes = [typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection)],
			}
		);

		var tree = result.GetGeneratedTree("ServiceInfo.g.cs");
		var generated = tree is null ? null : (await tree.GetTextAsync()).ToString();

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
	public async Task GenerateService_GeneratesAttributeAndEnum()
	{
		var source = """
			using Purview.SourceGeneratorFramework.Examples;

			namespace Test
			{
				[GenerateService]
				public class MyService { }
			}
			""";

		var runner = new SourceGeneratorTestRunner<ServiceRegistrationGenerator>();
		var result = await runner.RunAsync(
			source,
			new SourceGeneratorTestOptions
			{
				AdditionalAssemblyTypes = [typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection)],
			}
		);

		var attributeTree = result.GetGeneratedTree("GenerateServiceAttribute.g.cs");
		var attributeSource = attributeTree is null ? null : (await attributeTree.GetTextAsync()).ToString();

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
