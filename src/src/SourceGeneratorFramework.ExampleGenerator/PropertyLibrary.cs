namespace Purview.SourceGeneratorFramework.ExampleGenerator;

/// <summary>
/// Provides compiler-visible property names used by the service registration generator.
/// </summary>
static class PropertyLibrary
{
	/// <summary>
	/// When set to <see langword="true"/>, disables the service registration generator.
	/// </summary>
	public const string DisableServiceRegistrationGenerator =
		SourceGeneratorBuildProperties.BuildProperty + "DisableServiceRegistrationGenerator";

	/// <summary>
	/// When set to <see langword="true"/>, emits the optional <c>ServiceInfo</c> class.
	/// </summary>
	public const string EmitServiceRegistrationInfo =
		SourceGeneratorBuildProperties.BuildProperty + "EmitServiceRegistrationInfo";
}
