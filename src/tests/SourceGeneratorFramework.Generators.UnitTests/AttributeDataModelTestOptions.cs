namespace Purview.SourceGeneratorFramework.Generators;

public sealed record AttributeDataModelTestOptions : SourceGeneratorTestOptions
{
	public AttributeDataModelTestOptions()
	{
		AdditionalAssemblyTypes = AdditionalAssemblyTypes.Add(typeof(TypeIdentity));
	}
}
