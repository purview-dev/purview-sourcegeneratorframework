using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Purview.SourceGeneratorFramework.Models;

namespace Purview.SourceGeneratorFramework;

public class DiagnosticInfoTests
{
	static DiagnosticDescriptor TestDescriptor { get; } =
		new("TEST001", "Test Title", "Test message: {0}", "Test", DiagnosticSeverity.Warning, true);

	[Test]
	public async Task Create_WithNullLocation_ReturnsDiagnosticInfoWithEmptyPath()
	{
		var info = DiagnosticInfo.Create(TestDescriptor, (Location?)null, "arg");

		await Assert.That(info.FilePath).IsEqualTo(string.Empty);
		await Assert.That(info.TextSpan).IsEqualTo(default);
		await Assert.That(info.MessageArgs.Count).IsEqualTo(1);
		await Assert.That(info.MessageArgs[0]).IsEqualTo("arg");
	}

	[Test]
	public async Task Create_WithLocation_CapturesPathAndSpan()
	{
		var source = "class C { }";
		var tree = CSharpSyntaxTree.ParseText(source, path: "Test.cs");
		var location = Location.Create(tree, TextSpan.FromBounds(6, 7));

		var info = DiagnosticInfo.Create(TestDescriptor, location);

		await Assert.That(info.FilePath).IsEqualTo("Test.cs");
		await Assert.That(info.TextSpan.Start).IsEqualTo(6);
		await Assert.That(info.TextSpan.End).IsEqualTo(7);
	}

	[Test]
	public async Task ToDiagnostic_WithNullLocation_CreatesDiagnostic()
	{
		var info = DiagnosticInfo.Create(TestDescriptor, (Location?)null, "arg");
		var diagnostic = info.ToDiagnostic();

		await Assert.That(diagnostic.Descriptor).IsEqualTo(TestDescriptor);
		await Assert
			.That(diagnostic.GetMessage(CultureInfo.InvariantCulture))
			.Contains("Test message: arg");
		await Assert.That(diagnostic.Location.SourceSpan).IsEqualTo(default);
	}

	[Test]
	public async Task ToDiagnostic_WithLocation_CreatesDiagnosticWithLocation()
	{
		var source = "class C { }";
		var tree = CSharpSyntaxTree.ParseText(source, path: "Test.cs");
		var location = Location.Create(tree, TextSpan.FromBounds(6, 7));
		var info = DiagnosticInfo.Create(TestDescriptor, location, "arg");

		var diagnostic = info.ToDiagnostic();

		await Assert.That(diagnostic.Location.SourceSpan.Start).IsEqualTo(6);
		await Assert.That(diagnostic.Location.SourceSpan.End).IsEqualTo(7);
		await Assert.That(diagnostic.Location.GetLineSpan().Path).IsEqualTo("Test.cs");
	}
}
