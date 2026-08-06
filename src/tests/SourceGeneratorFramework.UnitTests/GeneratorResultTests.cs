using Purview.SourceGeneratorFramework.Models;

namespace Purview.SourceGeneratorFramework;

public class GeneratorResultTests
{
	[Test]
	public async Task Ok_WithValue_IsSuccess()
	{
		var result = GeneratorResult<string>.Ok("value");

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.IsEmpty).IsFalse();
		await Assert.That(result.IsFatal).IsFalse();
		await Assert.That(result.HasDiagnostics).IsFalse();
		await Assert.That(result.Value).IsEqualTo("value");
	}

	[Test]
	public async Task Ok_WithValueAndDiagnostics_HasDiagnostics()
	{
		var diagnostic = DiagnosticInfo.Create(
			new Microsoft.CodeAnalysis.DiagnosticDescriptor(
				"TEST001",
				"Test",
				"Message",
				"Test",
				Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
				true
			),
			null
		);
		var result = GeneratorResult<string>.Ok("value", diagnostic);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.HasDiagnostics).IsTrue();
		await Assert.That(result.Value).IsEqualTo("value");
	}

	[Test]
	public async Task Fail_WithDiagnostics_IsFatal()
	{
		var diagnostic = DiagnosticInfo.Create(
			new Microsoft.CodeAnalysis.DiagnosticDescriptor(
				"TEST001",
				"Test",
				"Message",
				"Test",
				Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
				true
			),
			null
		);
		var result = GeneratorResult<string>.Fail(diagnostic);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.IsFatal).IsTrue();
		await Assert.That(result.IsEmpty).IsFalse();
		await Assert.That(result.HasDiagnostics).IsTrue();
		await Assert.That(result.Value).IsNull();
	}

	[Test]
	public void Fail_WithoutDiagnostics_Throws()
	{
		Assert.Throws<ArgumentException>(() => GeneratorResult<string>.Fail());
	}

	[Test]
	public async Task Empty_IsEmpty()
	{
		var result = GeneratorResult<string>.Empty;

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.IsEmpty).IsTrue();
		await Assert.That(result.IsFatal).IsFalse();
		await Assert.That(result.HasDiagnostics).IsFalse();
		await Assert.That(result.Value).IsNull();
	}
}
