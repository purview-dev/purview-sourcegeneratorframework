using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

namespace Purview.SourceGeneratorFramework;

public class CodeQueryAssertionTests
{
	sealed class SimpleGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			context.RegisterPostInitializationOutput(static output =>
				output.AddSource(
					"Simple.g.cs",
					"""
					namespace Generated;

					public static class Simple
					{
						public const string Name = "simple";
						public static int Count { get; set; }

						public static void DoWork(int value, int? optional, object? context) { }
					}
					"""
				)
			);
		}
	}

	[Test]
	public async Task HasGeneratedMethod_ReturnsTheMethodNode(CancellationToken cancellationToken)
	{
		var runner = new SourceGeneratorTestRunner<SimpleGenerator>();
		var result = await runner.RunAsync("public sealed class Input { }", cancellationToken: cancellationToken);

		var method = await Assert.That(result).HasGeneratedMethod("DoWork");

		await Assert.That(method).IsNotNull();
		await Assert.That(method.Identifier.ValueText).IsEqualTo("DoWork");
	}

	[Test]
	public async Task HasGeneratedMethod_WithParameterTypes_ReturnsMatchingMethod(CancellationToken cancellationToken)
	{
		var runner = new SourceGeneratorTestRunner<SimpleGenerator>();
		var result = await runner.RunAsync("public sealed class Input { }", cancellationToken: cancellationToken);

		TypeReference[] parameters =
		[
			TypeReference.Create<int>(),
			TypeReference.Create<int>().Nullable(),
			TypeReference.Create<object>().Nullable(),
		];
		var method = await Assert.That(result).HasGeneratedMethod("DoWork", parameters);

		await Assert.That(method.ParameterList.Parameters.Count).IsEqualTo(3);
	}

	[Test]
	public async Task HasGeneratedClass_ReturnsTheClassNode(CancellationToken cancellationToken)
	{
		var runner = new SourceGeneratorTestRunner<SimpleGenerator>();
		var result = await runner.RunAsync("public sealed class Input { }", cancellationToken: cancellationToken);

		var @class = await Assert.That(result).HasGeneratedClass("Simple");

		await Assert.That(@class.Identifier.ValueText).IsEqualTo("Simple");
		await Assert.That(@class.Members).IsNotEmpty();
	}

	[Test]
	public async Task HasGeneratedField_ReturnsTheFieldNode(CancellationToken cancellationToken)
	{
		var runner = new SourceGeneratorTestRunner<SimpleGenerator>();
		var result = await runner.RunAsync("public sealed class Input { }", cancellationToken: cancellationToken);

		var field = await Assert.That(result).HasGeneratedField("Name");

		await Assert.That(field.Declaration.Variables[0].Identifier.ValueText).IsEqualTo("Name");
	}
}
