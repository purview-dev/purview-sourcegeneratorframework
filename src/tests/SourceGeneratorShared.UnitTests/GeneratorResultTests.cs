namespace Purview.SourceGeneratorFramework;

public class GeneratorResultTests
{
	[Test]
	public async Task Ok_WithValue_IsSuccess()
	{
		var result = GeneratorResult<string>.Ok("value");

		await Assert.That(result.HasValue).IsTrue();
		await Assert.That(result.IsEmpty).IsFalse();
		await Assert.That(result.ShouldProcess).IsTrue();
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
			)
		);
		var result = GeneratorResult<string>.Ok("value", diagnostic);

		await Assert.That(result.HasValue).IsTrue();
		await Assert.That(result.HasDiagnostics).IsTrue();
		await Assert.That(result.Value).IsEqualTo("value");
	}

	[Test]
	public async Task Fail_WithDiagnostics_ShouldProcessIsFalse()
	{
		var diagnostic = DiagnosticInfo.Create(
			new Microsoft.CodeAnalysis.DiagnosticDescriptor(
				"TEST001",
				"Test",
				"Message",
				"Test",
				Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
				true
			)
		);
		var result = GeneratorResult<string>.Fail(diagnostic);

		await Assert.That(result.HasValue).IsFalse();
		await Assert.That(result.ShouldProcess).IsFalse();
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

		await Assert.That(result.HasValue).IsFalse();
		await Assert.That(result.IsEmpty).IsTrue();
		await Assert.That(result.ShouldProcess).IsFalse();
		await Assert.That(result.HasDiagnostics).IsFalse();
		await Assert.That(result.Value).IsNull();
	}

	[Test]
	public async Task Empty_WithValueType_IsEmpty()
	{
		var result = GeneratorResult<int>.Empty;

		await Assert.That(result.HasValue).IsFalse();
		await Assert.That(result.IsEmpty).IsTrue();
		await Assert.That(result.ShouldProcess).IsFalse();
	}

	[Test]
	public async Task Fail_WithValueType_ShouldProcessIsFalse()
	{
		var diagnostic = DiagnosticInfo.Create(
			new Microsoft.CodeAnalysis.DiagnosticDescriptor(
				"TEST001",
				"Test",
				"Message",
				"Test",
				Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
				true
			)
		);

		var result = GeneratorResult<int>.Fail(diagnostic);

		await Assert.That(result.HasValue).IsFalse();
		await Assert.That(result.IsEmpty).IsFalse();
		await Assert.That(result.ShouldProcess).IsFalse();
	}
}
