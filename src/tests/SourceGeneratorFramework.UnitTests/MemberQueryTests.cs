namespace Purview.SourceGeneratorFramework;

public class MemberQueryTests
{
	const string Source = """
		namespace Test;

		public class ComplexType { }

		public sealed class Sample
		{
			public Sample() { }
			public Sample(int id) { }

			public int Count { get; set; }
			public string Name { get; set; } = "";

			public string this[int index] => "value";

			public void DoWork(int value, int? optional, ComplexType complex) { }
			public int Compute(int left, int right) => left + right;
			public string Format(string format, object? value) => "";
		}
		""";

	static CodeQuery CreateQuery()
	{
		var (compilation, _) = TestCompilation.CreateWithRoot(Source);

		return new([.. compilation.SyntaxTrees], compilation);
	}

	static readonly TypeReference IntType = TypeReference.Create<int>();
	static readonly TypeReference StringType = TypeReference.Create<string>();
	static readonly TypeReference NullableIntType = TypeReference.Create<int>().Nullable();
	static readonly TypeReference ComplexType = new(new TypeIdentity("ComplexType", "Test"));

	[Test]
	public async Task Class_HasMethod_MatchesParameterTypes()
	{
		var query = CreateQuery();
		var cls = query.GetClass("Sample");

		await Assert.That(cls.HasMethod(query, "DoWork", IntType, NullableIntType, ComplexType)).IsTrue();
		await Assert.That(cls.HasMethod(query, "DoWork", IntType, IntType)).IsFalse();
		await Assert.That(cls.HasMethod(query, "Missing")).IsFalse();
		await Assert.That(cls.GetMethod(query, "Compute").HasParameters(query, IntType, IntType)).IsTrue();
	}

	[Test]
	public async Task Class_HasMethodReturnType_Matches()
	{
		var query = CreateQuery();
		var cls = query.GetClass("Sample");

		await Assert.That(cls.HasMethodReturnType(query, "Compute", IntType)).IsTrue();
		await Assert.That(cls.HasMethodReturnType(query, "Compute", StringType)).IsFalse();
		await Assert.That(cls.GetMethod(query, "Compute").HasReturnType(query, IntType)).IsTrue();
	}

	[Test]
	public async Task Class_HasProperty_MatchesType()
	{
		var query = CreateQuery();
		var cls = query.GetClass("Sample");

		await Assert.That(cls.HasProperty(query, "Count")).IsTrue();
		await Assert.That(cls.HasProperty(query, "Count", IntType)).IsTrue();
		await Assert.That(cls.HasProperty(query, "Count", StringType)).IsFalse();
		await Assert.That(cls.GetProperty(query, "Name").HasType(query, StringType)).IsTrue();
	}

	[Test]
	public async Task Class_HasIndexer_MatchesTypeAndIndexParameters()
	{
		var query = CreateQuery();
		var cls = query.GetClass("Sample");

		await Assert.That(cls.HasIndexer(query)).IsTrue();
		await Assert.That(cls.HasIndexer(query, StringType)).IsTrue();
		await Assert.That(cls.HasIndexer(query, StringType, IntType)).IsTrue();
		await Assert.That(cls.HasIndexer(query, IntType, IntType)).IsFalse();
		await Assert.That(cls.GetIndexer(query).HasType(query, StringType)).IsTrue();
	}

	[Test]
	public async Task Class_HasConstructor_MatchesParameterTypes()
	{
		var query = CreateQuery();
		var cls = query.GetClass("Sample");

		await Assert.That(cls.HasConstructor(query)).IsTrue();
		await Assert.That(cls.HasConstructor(query, IntType)).IsTrue();
		await Assert.That(cls.HasConstructor(query, StringType)).IsFalse();
		await Assert.That(cls.GetConstructor(query, IntType).ParameterList.Parameters.Count).IsEqualTo(1);
	}

	[Test]
	public async Task GetClass_GivenNamespace_FiltersByNamespace()
	{
		var query = CreateQuery();

		await Assert.That(query.HasClass("Sample", "Test")).IsTrue();
		await Assert.That(query.HasClass("Sample", "Other")).IsFalse();
		await Assert.That(query.HasClass("Sample")).IsTrue();
		await Assert
			.That(() => query.GetClass("Sample", "Other"))
			.Throws<SyntaxNotFoundException>()
			.WithMessageContaining("Other", StringComparison.Ordinal);
	}
}
