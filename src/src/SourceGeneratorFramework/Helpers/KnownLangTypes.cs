using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Helpers;

/// <summary>
/// Provides a mapping between known .NET types, their corresponding SpecialType, and their C# keyword representation.
/// </summary>
public static class KnownLangTypes
{
	static readonly ImmutableArray<TypeMapping> All =
	[
		new(typeof(bool), SpecialType.System_Boolean, "bool"),
		new(typeof(byte), SpecialType.System_Byte, "byte"),
		new(typeof(sbyte), SpecialType.System_SByte, "sbyte"),
		new(typeof(char), SpecialType.System_Char, "char"),
		new(typeof(decimal), SpecialType.System_Decimal, "decimal"),
		new(typeof(double), SpecialType.System_Double, "double"),
		new(typeof(float), SpecialType.System_Single, "float"),
		new(typeof(int), SpecialType.System_Int32, "int"),
		new(typeof(uint), SpecialType.System_UInt32, "uint"),
		new(typeof(long), SpecialType.System_Int64, "long"),
		new(typeof(ulong), SpecialType.System_UInt64, "ulong"),
		new(typeof(short), SpecialType.System_Int16, "short"),
		new(typeof(ushort), SpecialType.System_UInt16, "ushort"),
		new(typeof(string), SpecialType.System_String, "string"),
		new(typeof(object), SpecialType.System_Object, "object"),
		new(typeof(void), SpecialType.System_Void, "void"),
		new(typeof(nint), SpecialType.System_IntPtr, "nint"),
		new(typeof(nuint), SpecialType.System_UIntPtr, "nuint"),
	];

	static readonly Dictionary<Type, TypeMapping> ByType = All.ToDictionary(static x => x.Type);

	static readonly Dictionary<SpecialType, TypeMapping> BySpecialType = All.ToDictionary(static x => x.SpecialType);

	static readonly Dictionary<string, TypeMapping> ByKeyword = All.ToDictionary(
		static x => x.Keyword,
		StringComparer.Ordinal
	);

	/// <summary>
	/// Gets the TypeMapping for the specified .NET type, or an empty TypeMapping if the type is not known.
	/// </summary>
	/// <param name="type">The .NET type to get the mapping for.</param>
	/// <returns>The TypeMapping for the specified type, or an empty TypeMapping if the type is not known.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the type is null.</exception>
	public static TypeMapping Get(Type type) =>
		type is null ? throw new ArgumentNullException(nameof(type))
		: ByType.TryGetValue(type, out var mapping) ? mapping
		: TypeMapping.Empty;

	/// <summary>
	/// Gets the TypeMapping for the specified SpecialType, or an empty TypeMapping if the SpecialType is not known.
	/// </summary>
	/// <param name="type">The SpecialType to get the mapping for.</param>
	/// <returns>The TypeMapping for the specified SpecialType, or an empty TypeMapping if the SpecialType is not known.</returns>
	public static TypeMapping Get(SpecialType type) =>
		BySpecialType.TryGetValue(type, out var mapping) ? mapping : TypeMapping.Empty;

	/// <summary>
	/// Gets the TypeMapping for the specified C# keyword, or an empty TypeMapping if the keyword is not known.
	/// </summary>
	/// <param name="keyword">The C# keyword to get the mapping for.</param>
	/// <returns>The TypeMapping for the specified C# keyword, or an empty TypeMapping if the keyword is not known.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the keyword is null or empty.	</exception>
	public static TypeMapping Get(string keyword) =>
		string.IsNullOrEmpty(keyword) ? throw new ArgumentNullException(nameof(keyword))
		: ByKeyword.TryGetValue(keyword, out var mapping) ? mapping
		: TypeMapping.Empty;

	/// <summary>
	/// Determines whether the specified type is a known type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns><c>true</c> if the type is known; otherwise, <c>false</c>.</returns>
	public static bool IsKnownType(Type type) => !Get(type).IsEmpty;

	/// <summary>
	/// Determines whether the specified special type is a known type.
	/// </summary>
	/// <param name="keyword">The keyword to check.</param>
	/// <returns><c>true</c> if the keyword is known; otherwise, <c>false</c>.</returns>
	public static bool IsKnownKeyword(string keyword) => !Get(keyword).IsEmpty;

	/// <summary>
	/// Determines whether the specified special type is a known type.
	/// </summary>
	/// <param name="specialType">The special type to check.</param>
	/// <returns><c>true</c> if the special type is known; otherwise, <c>false</c>.</returns>
	public static bool IsKnownSpecialType(SpecialType specialType) => !Get(specialType).IsEmpty;
}

/// <summary>
/// Represents a mapping between a .NET type, its corresponding SpecialType, and its C# keyword representation.
/// </summary>
public readonly record struct TypeMapping
{
	internal TypeMapping(Type type, SpecialType specialType, string keyword)
	{
		Type = type;
		SpecialType = specialType;
		Keyword = keyword;
	}

	/// <summary>
	/// Gets the .NET type associated with this mapping.
	/// </summary>
	public Type Type { get; }

	/// <summary>
	/// Gets the SpecialType associated with this mapping.
	/// </summary>
	public SpecialType SpecialType { get; }

	/// <summary>
	/// Gets the C# keyword representation of the type associated with this mapping.
	/// </summary>
	public string Keyword { get; }

	/// <summary>
	/// Determines whether this instance is empty (i.e., has no associated type, special type, or keyword).
	/// </summary>
	public bool IsEmpty => this == Empty;

	/// <summary>
	/// Defines an implicit conversion from TypeMapping to TypeValueObject. If the SpecialType is None, it returns an empty TypeValueObject; otherwise, it creates a new TypeValueObject with the specified SpecialType.
	/// </summary>
	/// <param name="mapping">The TypeMapping instance to convert.</param>
	public static implicit operator TypeValueObject(TypeMapping mapping) =>
		mapping.SpecialType == SpecialType.None ? TypeValueObject.Empty : new(mapping.SpecialType);

	/// <summary>
	/// Represents an empty TypeMapping instance with no associated type, special type, or keyword.
	/// </summary>
	public static readonly TypeMapping Empty;
}
