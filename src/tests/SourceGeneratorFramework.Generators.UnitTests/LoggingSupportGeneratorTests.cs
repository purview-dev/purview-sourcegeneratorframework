using Purview.SourceGeneratorFramework.Generators.Model;
using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework.Generators;

public class LoggingSupportGeneratorTests
{
	[Test]
	public async Task Generate_FindsGeneratorAndEmitsLogSupport()
	{
		var source = """
			using Microsoft.CodeAnalysis;

			namespace Test
			{
				public partial class MyGenerator : IIncrementalGenerator
				{
					public void Initialize(IncrementalGeneratorInitializationContext context) { }
				}
			}
			""";

		var runner = new SourceGeneratorTestRunner<LoggingSupportGenerator>();
		var result = await runner.RunAsync(source);

		var tree = result.GetGeneratedTree("MyGenerator.LogSupport.g.cs");
		var generated = tree is null ? null : (await tree.GetTextAsync()).ToString();

		await Assert.That(generated).IsNotNull();
		await Assert.That(generated).Contains("partial class MyGenerator");
		await Assert
			.That(generated)
			.Contains("global::Purview.SourceGeneratorFramework.Logging.ISupportsSourceGenLogging");
		await Assert
			.That(generated)
			.Contains("void global::Purview.SourceGeneratorFramework.Logging.ISupportsSourceGenLogging.SetOutput");
	}

	[Test]
	public async Task Generate_Disabled_ProducesNoOutput()
	{
		var source = """
			using Microsoft.CodeAnalysis;

			namespace Test
			{
				public partial class MyGenerator : IIncrementalGenerator
				{
					public void Initialize(IncrementalGeneratorInitializationContext context) { }
				}
			}
			""";

		var runner = new SourceGeneratorTestRunner<LoggingSupportGenerator>();
		var result = await runner.RunAsync(
			source,
			new SourceGeneratorTestOptions
			{
				DisableSourceGeneratorPropertyName = PropertyLibrary.DisableLoggingSourceGenerator,
				DisableSourceGeneratorValue = true,
			}
		);

		var tree = result.GetGeneratedTree("MyGenerator.LogSupport.g.cs");
		await Assert.That(tree).IsNull();
	}
}
