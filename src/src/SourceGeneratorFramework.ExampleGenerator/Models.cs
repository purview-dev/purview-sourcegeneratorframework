namespace Purview.SourceGeneratorFramework.Examples;

/// <summary>
/// Defines the lifetime of a generated service registration.
/// </summary>
public enum ServiceLifetime
{
	/// <summary>
	/// A single instance is created and reused for the lifetime of the application.
	/// </summary>
	Singleton = 0,

	/// <summary>
	/// A new instance is created once per scope.
	/// </summary>
	Scoped = 1,

	/// <summary>
	/// A new instance is created each time the service is requested.
	/// </summary>
	Transient = 2,
}

/// <summary>
/// Marks a type for service registration generation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GenerateServiceAttribute"/> class.
/// </remarks>
/// <param name="lifetime">The service lifetime.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Singleton) : Attribute
{
	/// <summary>
	/// Gets the service lifetime.
	/// </summary>
	public ServiceLifetime Lifetime { get; } = lifetime;

	/// <summary>
	/// Gets or sets the optional service name.
	/// </summary>
	public string? Name { get; set; }
}

/// <summary>
/// Attribute data model for <see cref="GenerateServiceAttribute"/>.
/// </summary>
[Generate(typeof(GenerateServiceAttribute))]
public readonly partial record struct GenerateServiceAttributeData(
	[Argument(
		"lifetime",
		IsEnum = true,
		DefaultValue = "Purview.SourceGeneratorFramework.Examples.ServiceLifetime.Singleton"
	)]
		string? Lifetime,
	[Property] string? Name
);

/// <summary>
/// Describes a discovered service target.
/// </summary>
readonly record struct ServiceTarget(string TypeName, string ClassName, string Name, string LifetimeMemberName)
{
	/// <summary>
	/// An empty <see cref="ServiceTarget"/>.
	/// </summary>
	public static readonly ServiceTarget Empty;
}

/// <summary>
/// Aggregated generation inputs for the service registration generator.
/// </summary>
readonly record struct ServiceRegistrationGenerationModel(
	GenerationContext Context,
	EquatableArray<ServiceTarget> Targets,
	bool EmitServiceInfo = false
);
