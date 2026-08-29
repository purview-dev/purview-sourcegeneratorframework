using Purview.SourceGeneratorFramework.Examples;
using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

public record ServiceRegistrationTestOptions : SourceGeneratorTestOptions
{
	public ServiceRegistrationTestOptions()
	{
		AdditionalAssemblyTypes = AdditionalAssemblyTypes.AddRange(
			typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection),
			typeof(ServiceLifetime)
		);
		AdditionalNamespaces = AdditionalNamespaces.Add("Purview.SourceGeneratorFramework.Examples");
		DisableSourceGeneratorPropertyName = PropertyLibrary.DisableServiceRegistrationGenerator;
	}
}
