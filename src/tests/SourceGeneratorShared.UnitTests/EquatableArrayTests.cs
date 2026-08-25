using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

public class EquatableArrayTests
{
	[Test]
	public async Task Create_WithItems_HasCorrectCount()
	{
		var array = EquatableArray<string>.Create("a", "b", "c");

		await Assert.That(array.Count).IsEqualTo(3);
		await Assert.That(array.IsEmpty).IsFalse();
	}

	[Test]
	public async Task Create_EmptyArray_IsEmpty()
	{
		var array = EquatableArray<string>.Create();

		await Assert.That(array.Count).IsEqualTo(0);
		await Assert.That(array.IsEmpty).IsTrue();
	}

	[Test]
	public async Task Empty_HasZeroCount()
	{
		var array = EquatableArray<int>.Empty;

		await Assert.That(array.Count).IsEqualTo(0);
		await Assert.That(array.IsEmpty).IsTrue();
	}

	[Test]
	public async Task Equals_SameItems_ReturnsTrue()
	{
		var first = EquatableArray<string>.Create("a", "b");
		var second = EquatableArray<string>.Create("a", "b");

		await Assert.That(first.Equals(second)).IsTrue();
		await Assert.That(first == second).IsTrue();
		await Assert.That(first != second).IsFalse();
	}

	[Test]
	public async Task Equals_DifferentItems_ReturnsFalse()
	{
		var first = EquatableArray<string>.Create("a", "b");
		var second = EquatableArray<string>.Create("a", "c");

		await Assert.That(first.Equals(second)).IsFalse();
		await Assert.That(first == second).IsFalse();
		await Assert.That(first != second).IsTrue();
	}

	[Test]
	public async Task GetHashCode_SameItems_AreEqual()
	{
		var first = EquatableArray<string>.Create("a", "b");
		var second = EquatableArray<string>.Create("a", "b");

		await Assert.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
	}

	[Test]
	public async Task ImplicitConversion_FromImmutableArray_Works()
	{
		var immutable = ImmutableArray.Create(1, 2, 3);
		EquatableArray<int> equatable = immutable;

		await Assert.That(equatable.Count).IsEqualTo(3);
	}

	[Test]
	public async Task ImplicitConversion_ToImmutableArray_Works()
	{
		var equatable = EquatableArray<int>.Create(1, 2, 3);
		ImmutableArray<int> immutable = equatable;

		await Assert.That(immutable.Length).IsEqualTo(3);
	}

	[Test]
	public async Task Indexer_ReturnsExpectedItem()
	{
		var array = EquatableArray<string>.Create("a", "b", "c");

		await Assert.That(array[1]).IsEqualTo("b");
	}

	[Test]
	public async Task GetEnumerator_EnumeratesAllItems()
	{
		var array = EquatableArray<int>.Create(1, 2, 3);
		var sum = 0;

		foreach (var item in array)
			sum += item;

		await Assert.That(sum).IsEqualTo(6);
	}

	[Test]
	public async Task DefaultArray_IsEmpty()
	{
		EquatableArray<int> array = default;

		await Assert.That(array.Count).IsEqualTo(0);
		await Assert.That(array.IsEmpty).IsTrue();
	}
}
