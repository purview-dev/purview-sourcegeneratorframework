using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Models;

public sealed class TypeReferenceOptionsTests
{
	// ---------------------------------------------------------------------------------------------
	// Rendering
	// ---------------------------------------------------------------------------------------------

	[Test]
	public async Task RenderFullName_RendersComposedSuffixesInSourceOrder()
	{
		var int32 = new TypeValueObject(SpecialType.System_Int32);

		await Assert.That(int32.AsTypeReference().RenderFullName).IsEqualTo("int");
		await Assert.That(int32.MakeArray().RenderFullName).IsEqualTo("int[]");
		await Assert.That(int32.MakeArray(2).RenderFullName).IsEqualTo("int[,]");
		await Assert.That(int32.MakeNullable().RenderFullName).IsEqualTo("int?");
		await Assert.That(int32.MakeNullable().MakeArray().RenderFullName).IsEqualTo("int?[]");
		await Assert.That(int32.MakeArray().Nullable().RenderFullName).IsEqualTo("int[]?");
		await Assert.That(int32.MakePointer().MakeArray().RenderFullName).IsEqualTo("int*[]");

		// A run of array declarators reads outermost-first: a rank-1 array of rank-2 arrays.
		await Assert.That(int32.MakeArray(2).MakeArray(1).RenderFullName).IsEqualTo("int[][,]");
	}

	[Test]
	public async Task RenderFullName_GivenTypeParameterOrDynamic_RendersCore()
	{
		await Assert.That(TypeReferenceOptions.ForTypeParameter("TKey").RenderFullName).IsEqualTo("TKey");
		await Assert.That(TypeReferenceOptions.ForTypeParameter("TKey").MakeArray().RenderFullName).IsEqualTo("TKey[]");
		await Assert.That(TypeReferenceOptions.Dynamic.RenderFullName).IsEqualTo("dynamic");
	}

	// ---------------------------------------------------------------------------------------------
	// Round-tripping from symbols
	// ---------------------------------------------------------------------------------------------

	[Test]
	[Arguments("public int[] Value = null!;", "int[]")]
	[Arguments("public int[,] Value = null!;", "int[,]")]
	[Arguments("public int[][,] Value = null!;", "int[][,]")]
	[Arguments("public int? Value;", "int?")]
	[Arguments("public byte* Value;", "byte*")]
	[Arguments("public byte** Value;", "byte**")]
	[Arguments("public byte*[] Value = null!;", "byte*[]")]
	[Arguments("public T Value = default!;", "T")]
	[Arguments("public T[] Value = null!;", "T[]")]
	[Arguments("public dynamic Value = null!;", "dynamic")]
	public async Task TryCreate_RoundTripsSymbolToRenderedSource(string fieldDeclaration, string expected)
	{
		var symbol = TestCompilation.FieldType(fieldDeclaration);

		await Assert.That(TypeReferenceOptions.TryCreate(symbol, out var reference)).IsTrue();
		await Assert.That(reference.RenderFullName).IsEqualTo(expected);
		await Assert.That(reference.Matches(symbol)).IsTrue();
	}

	[Test]
	public async Task TryCreate_GivenNullableReferenceType_RecordsAnnotation()
	{
		var symbol = TestCompilation.FieldType("public string? Value;");

		await Assert.That(TypeReferenceOptions.TryCreate(symbol, out var reference)).IsTrue();
		await Assert.That(reference.IsNullable).IsTrue();
		await Assert.That(reference.RenderFullName).IsEqualTo("string?");
	}

	[Test]
	public async Task TryCreate_GivenNullableArrayOfNullableElements_PreservesOrdering()
	{
		var symbol = TestCompilation.FieldType("public string?[]? Value;");

		await Assert.That(TypeReferenceOptions.TryCreate(symbol, out var reference)).IsTrue();
		await Assert.That(reference.RenderFullName).IsEqualTo("string?[]?");
	}

	[Test]
	public async Task TryCreate_GivenErrorType_ReturnsFalse()
	{
		var symbol = TestCompilation.FieldType("public Missing.Thing Value;");

		await Assert.That(TypeReferenceOptions.TryCreate(symbol, out _)).IsFalse();
	}

	[Test]
	[Arguments(typeof(int[]), "int[]")]
	[Arguments(typeof(int[,]), "int[,]")]
	[Arguments(typeof(int?), "int?")]
	public async Task TryCreate_GivenRuntimeType_RoundTrips(Type type, string expected)
	{
		await Assert.That(TypeReferenceOptions.TryCreate(type, out var reference)).IsTrue();
		await Assert.That(reference.RenderFullName).IsEqualTo(expected);
	}

	// ---------------------------------------------------------------------------------------------
	// Matching
	// ---------------------------------------------------------------------------------------------

	[Test]
	public async Task Matches_GivenArrayRankMismatch_ReturnsFalse()
	{
		var reference = new TypeValueObject(SpecialType.System_Int32).MakeArray();

		await Assert.That(reference.Matches(TestCompilation.FieldType("public int[] Value = null!;"))).IsTrue();
		await Assert.That(reference.Matches(TestCompilation.FieldType("public int[,] Value = null!;"))).IsFalse();
		await Assert.That(reference.Matches(TestCompilation.FieldType("public int Value;"))).IsFalse();
	}

	[Test]
	public async Task Matches_GivenNullableValueType_IsEnforced()
	{
		var nullable = new TypeValueObject(SpecialType.System_Int32).MakeNullable();

		await Assert.That(nullable.Matches(TestCompilation.FieldType("public int? Value;"))).IsTrue();
		await Assert.That(nullable.Matches(TestCompilation.FieldType("public int Value;"))).IsFalse();
	}

	[Test]
	public async Task Matches_GivenNullableReferenceType_IgnoresAnnotation()
	{
		var nullable = new TypeValueObject(SpecialType.System_String).MakeNullable();

		// Annotation is metadata, not identity: both spellings resolve to System.String.
		await Assert.That(nullable.Matches(TestCompilation.FieldType("public string? Value;"))).IsTrue();
		await Assert.That(nullable.Matches(TestCompilation.FieldType("public string Value = null!;"))).IsTrue();
	}

	[Test]
	public async Task Matches_GivenTypeParameter_ComparesByName()
	{
		var symbol = TestCompilation.FieldType("public T Value = default!;");

		await Assert.That(TypeReferenceOptions.ForTypeParameter("T").Matches(symbol)).IsTrue();
		await Assert.That(TypeReferenceOptions.ForTypeParameter("TOther").Matches(symbol)).IsFalse();
		await Assert.That(TypeReferenceOptions.Dynamic.Matches(symbol)).IsFalse();
	}

	[Test]
	public async Task Matches_GivenDynamic_ReturnsTrue()
	{
		var symbol = TestCompilation.FieldType("public dynamic Value = null!;");

		await Assert.That(TypeReferenceOptions.Dynamic.Matches(symbol)).IsTrue();
		await Assert.That(new TypeValueObject(SpecialType.System_Object).AsTypeReference().Matches(symbol)).IsFalse();
	}

	[Test]
	public async Task Matches_GivenMemberSymbol_ResolvesItsType()
	{
		var compilation = TestCompilation.Create(
			"""
			namespace Sample;

			public class Holder
			{
				public int[] Field = null!;
				public int Other;
			}
			"""
		);

		var holder = compilation.GetTypeByMetadataName("Sample.Holder")!;
		var reference = new TypeValueObject(SpecialType.System_Int32).MakeArray();

		await Assert.That(reference.Matches(holder.GetMembers("Field").Single())).IsTrue();
		await Assert.That(reference.Matches(holder.GetMembers("Other").Single())).IsFalse();
	}

	// ---------------------------------------------------------------------------------------------
	// Equality
	// ---------------------------------------------------------------------------------------------

	[Test]
	public async Task Equality_DistinguishesModifierOrder()
	{
		var int32 = new TypeValueObject(SpecialType.System_Int32);

		await Assert.That(int32.MakeNullable().MakeArray()).IsNotEqualTo(int32.MakeArray().Nullable());
		await Assert.That(int32.MakeArray()).IsEqualTo(int32.MakeArray());
		await Assert.That(int32.MakeArray().GetHashCode()).IsEqualTo(int32.MakeArray().GetHashCode());
		await Assert.That(int32.MakeArray(1)).IsNotEqualTo(int32.MakeArray(2));
	}

	[Test]
	public async Task Equals_GivenPlainNamedType_MatchesUnderlyingValueObject()
	{
		var int32 = new TypeValueObject(SpecialType.System_Int32);

		await Assert.That(int32.AsTypeReference().Equals(int32)).IsTrue();
		await Assert.That(int32.MakeArray().Equals(int32)).IsFalse();
		await Assert.That(int32.AsTypeReference().IsPlainNamedType).IsTrue();
	}

	[Test]
	public async Task ImplicitConversion_FromNamedType_ProducesPlainReference()
	{
		TypeReferenceOptions reference = new TypeValueObject("Order", "Sample");

		await Assert.That(reference.Kind).IsEqualTo(TypeReferenceKind.Named);
		await Assert.That(reference.IsPlainNamedType).IsTrue();
		await Assert.That(reference.RenderFullName).IsEqualTo("global::Sample.Order");
	}

	[Test]
	public async Task Append_GivenEmptyReference_Throws()
	{
		await Assert.That(void () => _ = TypeReferenceOptions.Empty.MakeArray()).Throws<InvalidOperationException>();
	}

	[Test]
	public async Task MakeArray_GivenInvalidRank_Throws()
	{
		var int32 = new TypeValueObject(SpecialType.System_Int32);

		await Assert.That(void () => _ = int32.MakeArray(0)).Throws<ArgumentOutOfRangeException>();
	}
}
