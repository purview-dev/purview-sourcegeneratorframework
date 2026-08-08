using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Identifies the C# declaration emitted for a generated type.
/// </summary>
public enum TypeDeclarationKind
{
	/// <summary>A class declaration.</summary>
	Class,

	/// <summary>A struct declaration.</summary>
	Struct,

	/// <summary>A record class declaration.</summary>
	RecordClass,

	/// <summary>A record struct declaration.</summary>
	RecordStruct,
}

/// <summary>
/// Identifies an optional C# accessibility modifier for a generated type.
/// </summary>
public enum TypeDeclarationAccessibility
{
	/// <summary>The <c>public</c> accessibility modifier.</summary>
	Public,

	/// <summary>The <c>internal</c> accessibility modifier.</summary>
	Internal,

	/// <summary>The <c>protected</c> accessibility modifier.</summary>
	Protected,

	/// <summary>The <c>private</c> accessibility modifier.</summary>
	Private,

	/// <summary>The <c>protected internal</c> accessibility modifier.</summary>
	ProtectedInternal,

	/// <summary>The <c>private protected</c> accessibility modifier.</summary>
	PrivateProtected,

	/// <summary>The <c>file</c> accessibility modifier.</summary>
	File,
}

/// <summary>
/// Describes one generic type parameter and its optional C# constraints.
/// </summary>
public sealed record GenericTypeParameterOptions
{
	/// <summary>
	/// Initializes a generic type parameter description.
	/// </summary>
	/// <param name="name">The type parameter name.</param>
	public GenericTypeParameterOptions(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException(
				"Type parameter name cannot be null or whitespace.",
				nameof(name)
			);

		Name = name;
	}

	/// <summary>
	/// Gets the type parameter name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the ordered constraint expressions written after <c>where T :</c>.
	/// </summary>
	/// <remarks>
	/// Entries are emitted verbatim and may contain values such as <c>class</c>, <c>notnull</c>,
	/// a base type, an interface, or <c>new()</c>.
	/// </remarks>
	public ImmutableArray<string> Constraints { get; init; } = [];
}

/// <summary>
/// Describes a generated class, struct, record class, or record struct declaration.
/// </summary>
public sealed record TypeDeclarationOptions
{
	/// <summary>
	/// Initializes a type declaration description.
	/// </summary>
	/// <param name="name">The generated type name without generic parameters.</param>
	public TypeDeclarationOptions(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Type name cannot be null or whitespace.", nameof(name));

		Name = name;
	}

	/// <summary>
	/// Initializes a type declaration description from a <see cref="TypeValueObject"/>.
	/// </summary>
	/// <param name="typeValue">The type value object.</param>
	public TypeDeclarationOptions(TypeValueObject typeValue)
	{
		Name = typeValue.TypeName;
	}

	/// <summary>
	/// Gets the generated type name without generic parameters.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the declaration kind. The default is <see cref="TypeDeclarationKind.Class"/>.
	/// </summary>
	public TypeDeclarationKind Kind { get; init; } = TypeDeclarationKind.Class;

	/// <summary>
	/// Gets the accessibility modifier, or <see langword="null"/> to omit accessibility.
	/// </summary>
	public TypeDeclarationAccessibility? Accessibility { get; init; }

	/// <summary>
	/// Gets whether the <c>partial</c> modifier is emitted. The default is <see langword="true"/>.
	/// </summary>
	public bool IsPartial { get; init; } = true;

	/// <summary>
	/// Gets whether the <c>sealed</c> modifier is emitted for a class or record class.
	/// The default is <see langword="true"/>. This option is ignored for struct declarations.
	/// </summary>
	public bool IsSealed { get; init; } = true;

	/// <summary>
	/// Gets whether the <c>readonly</c> modifier is emitted for a struct or record struct.
	/// </summary>
	public bool IsReadOnly { get; init; }

	/// <summary>
	/// Gets the optional base class or base record type.
	/// </summary>
	/// <remarks>Struct and record struct declarations cannot specify a base type.</remarks>
	public string? BaseType { get; init; }

	/// <summary>
	/// Gets the interfaces implemented by the generated type.
	/// </summary>
	public ImmutableArray<string> Interfaces { get; init; } = [];

	/// <summary>
	/// Gets the generic type parameters and their constraints.
	/// </summary>
	public ImmutableArray<GenericTypeParameterOptions> GenericTypes { get; init; } = [];

	/// <summary>
	/// Gets the primary-constructor parameters written after the type name and generic parameters.
	/// </summary>
	/// <remarks>Each entry is emitted verbatim as a complete parameter declaration.</remarks>
	public ImmutableArray<string> PrimaryConstructorParameters { get; init; } = [];
}
