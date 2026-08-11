using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

public sealed class TypeDeclarationAccessibilityExtensionsTests
{
	[Test]
	[Arguments(Accessibility.Private, TypeDeclarationAccessibility.Private)]
	[Arguments(Accessibility.ProtectedAndInternal, TypeDeclarationAccessibility.PrivateProtected)]
	[Arguments(Accessibility.Protected, TypeDeclarationAccessibility.Protected)]
	[Arguments(Accessibility.Internal, TypeDeclarationAccessibility.Internal)]
	[Arguments(Accessibility.ProtectedOrInternal, TypeDeclarationAccessibility.ProtectedInternal)]
	[Arguments(Accessibility.Public, TypeDeclarationAccessibility.Public)]
	public async Task ToTypeDeclarationAccessibility_GivenKnownValue_ReturnsMappedValue(
		Accessibility accessibility,
		TypeDeclarationAccessibility expected
	)
	{
		// Arrange performed by test arguments.

		// Act
		var actual = accessibility.ToTypeDeclarationAccessibility();

		// Assert
		await Assert.That(actual).IsEqualTo(expected);
	}

	[Test]
	[Arguments(Accessibility.NotApplicable)]
	[Arguments((Accessibility)int.MaxValue)]
	public async Task ToTypeDeclarationAccessibility_GivenUnmappedValue_ReturnsNull(
		Accessibility accessibility
	)
	{
		// Arrange performed by test arguments.

		// Act
		var actual = accessibility.ToTypeDeclarationAccessibility();

		// Assert
		await Assert.That(actual).IsNull();
	}
}
