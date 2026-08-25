using System.ComponentModel;
using TUnit.Assertions.Attributes;

namespace Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class TypeIdentityAssertions
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[GenerateAssertion(ExpectationMessage = "compilation result should contain symbol for {identity}")]
	public static bool HasSymbol(this DriverRunResult result, TypeIdentity identity)
	{
		if (result is null)
			throw new ArgumentNullException(nameof(result));

		var symbol = result.CompilationResult.Compilation.GetTypeByMetadataName(identity.MetadataFullName);

		return symbol is not null;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[GenerateAssertion(
		ExpectationMessage = "compilation result should contain symbol for {fullyQualifiedMetadataName}"
	)]
	public static bool HasSymbol(this DriverRunResult result, string fullyQualifiedMetadataName)
	{
		if (result is null)
			throw new ArgumentNullException(nameof(result));
		if (string.IsNullOrWhiteSpace(fullyQualifiedMetadataName))
			throw new ArgumentException(
				$"'{nameof(fullyQualifiedMetadataName)}' cannot be null or whitespace.",
				nameof(fullyQualifiedMetadataName)
			);

		var symbol = result.CompilationResult.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);

		return symbol is not null;
	}
}
