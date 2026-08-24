using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;

namespace Purview.SourceGeneratorFramework;

public class KnownLangTypesTests
{
	[Test]
	public async Task Get_KnownKeyword_ReturnsCorrectType()
	{
		var result = KnownLangTypes.Get("int");

		await Assert.That(result.Type).IsEqualTo(typeof(int));
		await Assert.That(result.SpecialType).IsEqualTo(SpecialType.System_Int32);
		await Assert.That(result.Keyword).IsEqualTo("int");
	}

	[Test]
	public async Task Get_UnknownKeyword_ReturnsEmpty()
	{
		var result = KnownLangTypes.Get("unknown");

		await Assert.That(result).IsEqualTo(TypeMapping.Empty);
	}
}
