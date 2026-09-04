using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework.ExampleGenerator;

public record CodeWriterSampleTestOptions : SourceGeneratorTestOptions
{
	public CodeWriterSampleTestOptions()
	{
		AdditionalNamespaces = AdditionalNamespaces.Add("Purview.SourceGeneratorFramework.Examples");
	}
}
