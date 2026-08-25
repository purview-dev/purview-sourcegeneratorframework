using System.Collections;
using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// An immutable, equatable wrapper around <see cref="ImmutableArray{T}"/> that is safe to use in incremental source generator pipelines.
/// </summary>
/// <typeparam name="T">The type of elements in the array. Must implement <see cref="IEquatable{T}"/>.</typeparam>
public readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
	where T : IEquatable<T>
{
	public static readonly EquatableArray<T> Empty = new([]);

	readonly ImmutableArray<T> _array = array.IsDefault ? [] : array;

	public int Count => _array.IsDefault ? 0 : _array.Length;

	public bool IsEmpty => _array.IsDefaultOrEmpty;

	public T this[int index] => _array[index];

	public ImmutableArray<T> AsImmutableArray() => _array.IsDefault ? [] : _array;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
	public static EquatableArray<T> Create(params T[] items)
	{
		if (items is null || items.Length == 0)
			return Empty;

		// All valid...
		return new EquatableArray<T>(ImmutableArray.Create(items));
	}

	public bool Equals(EquatableArray<T> other) => AsImmutableArray().SequenceEqual(other.AsImmutableArray());

	public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			foreach (var item in AsImmutableArray())
				hash = (hash * 31) + (item?.GetHashCode() ?? 0);
			return hash;
		}
	}

	public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);

	public static implicit operator ImmutableArray<T>(EquatableArray<T> array) => array.AsImmutableArray();

	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
