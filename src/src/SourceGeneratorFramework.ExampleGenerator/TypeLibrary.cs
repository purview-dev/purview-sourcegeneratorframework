namespace Purview.SourceGeneratorFramework.ExampleGenerator;

/// <summary>
/// Provides <see cref="TypeIdentity"/> instances used by the service registration generator.
/// </summary>
static class TypeLibrary
{
	/// <summary>
	/// Common types from the <c>Microsoft.Extensions.DependencyInjection</c> namespace.
	/// </summary>
	public static class Microsoft
	{
		/// <summary>
		/// Common types from the <c>Microsoft.Extensions.DependencyInjection</c> namespace.
		/// </summary>
		public static class Extensions
		{
			/// <summary>
			/// Common types from the <c>Microsoft.Extensions.DependencyInjection</c> namespace.
			/// </summary>
			public static class DependencyInjection
			{
				/// <summary>
				/// <c>Microsoft.Extensions.DependencyInjection.IServiceCollection</c>.
				/// </summary>
				public static readonly TypeIdentity IServiceCollection = new(
					"IServiceCollection",
					"Microsoft.Extensions.DependencyInjection"
				);

				/// <summary>
				/// <c>Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions</c>.
				/// </summary>
				public static readonly TypeIdentity ServiceCollectionServiceExtensions = new(
					"ServiceCollectionServiceExtensions",
					"Microsoft.Extensions.DependencyInjection"
				);
			}
		}
	}

	/// <summary>
	/// The <c>[GenerateService]</c> attribute type.
	/// </summary>
	public static readonly TypeIdentity GenerateServiceAttribute = new(
		"GenerateServiceAttribute",
		"Purview.SourceGeneratorFramework.Examples"
	);

	/// <summary>
	/// The <c>ServiceLifetime</c> enum type.
	/// </summary>
	public static readonly TypeIdentity ServiceLifetime = new(
		"ServiceLifetime",
		"Purview.SourceGeneratorFramework.Examples"
	);

	/// <summary>
	/// The static <c>ServiceCollectionExtensions</c> class.
	/// </summary>
	public static readonly TypeIdentity ServiceCollectionExtensions = new(
		"ServiceCollectionExtensions",
		"Purview.SourceGeneratorFramework.Examples"
	);

	/// <summary>
	/// The static <c>ServiceInfo</c> class.
	/// </summary>
	public static readonly TypeIdentity ServiceInfo = new("ServiceInfo", "Purview.SourceGeneratorFramework.Examples");
}
