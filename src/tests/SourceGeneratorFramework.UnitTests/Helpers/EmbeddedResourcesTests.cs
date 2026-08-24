namespace Purview.SourceGeneratorFramework.Helpers;

public class EmbeddedResourcesTests
{
	[Test]
	public async Task Load_ExistingResource_ReturnsContent()
	{
		var content = EmbeddedResourceHelper.Load(
			"Resources.TestResource.txt",
			typeof(EmbeddedResourcesTests).Assembly
		);

		await Assert.That(content).IsEqualTo("Hello from embedded resource!");
	}

	[Test]
	public async Task Load_FullResourceName_ReturnsContent()
	{
		var content = EmbeddedResourceHelper.Load(
			"Purview.SourceGeneratorFramework.Resources.TestResource.txt",
			typeof(EmbeddedResourcesTests).Assembly
		);

		await Assert.That(content).IsEqualTo("Hello from embedded resource!");
	}

	[Test]
	public async Task Load_MissingResource_Throws()
	{
		await Assert
			.That(() => EmbeddedResourceHelper.Load("Missing.Resource.txt", typeof(EmbeddedResourcesTests).Assembly))
			.ThrowsException()
			.WithMessageContaining("Missing.Resource.txt", StringComparison.Ordinal);
	}

	[Test]
	public async Task Load_SourceFileResourceWithoutExtension_ReturnsContent()
	{
		var content = EmbeddedResourceHelper.Load("TestResource", typeof(EmbeddedResourcesTests).Assembly);

		await Assert.That(content).IsEqualTo("Hello from embedded C# resource!");
	}

	[Test]
	public async Task GetResourceNames_ReturnsResourceName()
	{
		var names = EmbeddedResourceHelper.GetResourceNames(typeof(EmbeddedResourcesTests).Assembly);

		await Assert.That(names).Contains("Purview.SourceGeneratorFramework.Resources.TestResource.txt");
	}
}
