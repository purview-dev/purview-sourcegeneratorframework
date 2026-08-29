namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Contains the MSBuild properties used by the source-generator framework.
/// </summary>
public static class SourceGeneratorBuildProperties
{
	public const string BuildProperty = "build_property.";

	/// <summary>
	/// The MSBuild property that controls validation of undisposed code-writer scopes.
	/// </summary>
	public const string ValidateCodeWriterScopes =
		BuildProperty + "PurviewSourceGeneratorFrameworkValidateCodeWriterScopes";

	/// <summary>
	/// The MSBuild property that enables source-generator logging.
	/// </summary>
	public const string EnableLogging = BuildProperty + "PurviewSourceGeneratorFrameworkEnableLogging";

	/// <summary>
	/// The MSBuild property that identifies the registered logging sink for a generator run.
	/// </summary>
	public const string LoggingSessionId = BuildProperty + "PurviewSourceGeneratorFrameworkLoggingSessionId";
}
