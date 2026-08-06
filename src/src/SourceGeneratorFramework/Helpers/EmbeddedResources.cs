using System.Reflection;

namespace Purview.SourceGeneratorFramework.Helpers;

/// <summary>
/// Provides helpers for loading embedded resources from an assembly.
/// </summary>
public static class EmbeddedResources
{
	/// <summary>
	/// Loads the specified embedded resource as a string.
	/// </summary>
	public static string Load(string resourceName, Assembly? assembly = null)
	{
		assembly ??= Assembly.GetCallingAssembly();

		using var stream = LoadStream(resourceName, assembly);
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	/// <summary>
	/// Loads the specified embedded resource as a stream.
	/// </summary>
	public static Stream LoadStream(string resourceName, Assembly? assembly = null)
	{
		assembly ??= Assembly.GetCallingAssembly();

		var resolvedName = ResolveResourceName(resourceName, assembly);
		var stream =
			assembly.GetManifestResourceStream(resolvedName)
			?? throw new InvalidOperationException(
				$"Embedded resource '{resolvedName}' was not found in assembly '{assembly.FullName}'."
			);

		return stream;
	}

	/// <summary>
	/// Gets the names of all embedded resources in the assembly.
	/// </summary>
	public static IEnumerable<string> GetResourceNames(Assembly? assembly = null)
	{
		assembly ??= Assembly.GetCallingAssembly();
		return assembly.GetManifestResourceNames();
	}

	static string ResolveResourceName(string resourceName, Assembly assembly)
	{
		var names = assembly.GetManifestResourceNames();
		if (names.Contains(resourceName, StringComparer.Ordinal))
			return resourceName;

		var prefix = assembly.GetName().Name;
		if (!string.IsNullOrEmpty(prefix))
		{
			var candidate = prefix + "." + resourceName;
			if (names.Contains(candidate, StringComparer.Ordinal))
				return candidate;
		}

		var suffixMatch = names.FirstOrDefault(n =>
			n.EndsWith("." + resourceName, StringComparison.Ordinal)
		);
		if (suffixMatch != null)
			return suffixMatch;

		// If we reach here, the resource was not found. Throw an exception with available resources for debugging.
		throw new InvalidOperationException(
			$"Embedded resource '{resourceName}' was not found in assembly '{assembly.FullName}'. Available resources: {string.Join(", ", names)}."
		);
	}
}
