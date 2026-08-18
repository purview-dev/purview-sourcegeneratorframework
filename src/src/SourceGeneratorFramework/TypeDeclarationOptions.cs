using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

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

	/// <summary>An interface declaration.</summary>
	Interface,

	/// <summary>An enum declaration.</summary>
	Enum,

	/// <summary>A delegate declaration.</summary>
	Delegate,
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
/// Converts Roslyn symbol accessibility values to C# declaration accessibility values.
/// </summary>
public static class TypeDeclarationAccessibilityExtensions
{
	/// <summary>
	/// Converts a Roslyn <see cref="Accessibility"/> value to the corresponding
	/// <see cref="TypeDeclarationAccessibility"/> value.
	/// </summary>
	/// <param name="accessibility">The Roslyn accessibility value.</param>
	/// <returns>
	/// The corresponding declaration accessibility, or <see langword="null"/> when Roslyn reports
	/// <see cref="Accessibility.NotApplicable"/> or an unknown future value.
	/// </returns>
	/// <remarks>This method never throws for an accessibility value.</remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases")]
	public static TypeDeclarationAccessibility? ToTypeDeclarationAccessibility(this Accessibility accessibility) =>
		accessibility switch
		{
			Accessibility.Private => TypeDeclarationAccessibility.Private,
			Accessibility.ProtectedAndInternal => TypeDeclarationAccessibility.PrivateProtected,
			Accessibility.Protected => TypeDeclarationAccessibility.Protected,
			Accessibility.Internal => TypeDeclarationAccessibility.Internal,
			Accessibility.ProtectedOrInternal => TypeDeclarationAccessibility.ProtectedInternal,
			Accessibility.Public => TypeDeclarationAccessibility.Public,
			_ => null,
		};

	/// <summary>
	/// Converts a declaration accessibility value to the corresponding Roslyn
	/// <see cref="Accessibility"/> value.
	/// </summary>
	/// <param name="accessibility">The declaration accessibility value.</param>
	/// <returns>
	/// The corresponding Roslyn accessibility, or <see cref="Accessibility.NotApplicable"/> for
	/// <see cref="TypeDeclarationAccessibility.File"/> or an unknown future value.
	/// </returns>
	/// <remarks>
	/// Roslyn represents file-local accessibility separately from <see cref="Accessibility"/>, so
	/// <see cref="TypeDeclarationAccessibility.File"/> has no direct mapping. This method never
	/// throws for an accessibility value.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases")]
	public static Accessibility ToRoslynAccessibility(this TypeDeclarationAccessibility accessibility) =>
		accessibility switch
		{
			TypeDeclarationAccessibility.Private => Accessibility.Private,
			TypeDeclarationAccessibility.PrivateProtected => Accessibility.ProtectedAndInternal,
			TypeDeclarationAccessibility.Protected => Accessibility.Protected,
			TypeDeclarationAccessibility.Internal => Accessibility.Internal,
			TypeDeclarationAccessibility.ProtectedInternal => Accessibility.ProtectedOrInternal,
			TypeDeclarationAccessibility.Public => Accessibility.Public,
			_ => Accessibility.NotApplicable,
		};
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
			throw new ArgumentException("Type parameter name cannot be null or whitespace.", nameof(name));

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

/// <summary>Describes a field in a generated enum declaration.</summary>
public readonly record struct EnumFieldDeclarationOptions
{
	/// <summary>Initializes an enum field declaration.</summary>
	/// <param name="fieldName">The enum field name.</param>
	/// <param name="fieldValue">
	/// The enum field value. Strings are emitted as C# expressions; other values are
	/// formatted using the invariant culture.
	/// </param>
	/// <param name="xmlSummary">The lines written in the field's XML <c>summary</c> block.</param>
	public EnumFieldDeclarationOptions(string fieldName, object fieldValue, params string[] xmlSummary)
		: this(fieldName, xmlSummary)
	{
		if (fieldValue is null)
			throw new ArgumentNullException(nameof(fieldValue));

		FieldValue = fieldValue;
	}

	/// <summary>Initializes an enum field declaration.</summary>
	/// <param name="fieldName">The enum field name.</param>
	/// <param name="xmlSummary">The lines written in the field's XML <c>summary</c> block.</param>
	public EnumFieldDeclarationOptions(string fieldName, params string[] xmlSummary)
	{
		if (string.IsNullOrWhiteSpace(fieldName))
			throw new ArgumentException("Enum field name cannot be null or whitespace.", nameof(fieldName));

		FieldName = fieldName;
		XmlSummary = [.. xmlSummary ?? []];
	}

	/// <summary>Gets the enum field name.</summary>
	public string FieldName { get; }

	/// <summary>
	/// Gets the optional enum field value. Strings are treated as C# expressions rather than
	/// string literals.
	/// </summary>
	public object? FieldValue { get; }

	/// <summary>Gets the lines written in the field's XML <c>summary</c> block.</summary>
	public ImmutableArray<string> XmlSummary { get; init; } = [];

	/// <summary>Gets the attributes applied to the enum field.</summary>
	public ImmutableArray<AttributeDeclarationOptions> Attributes { get; init; } = [];
}

/// <summary>
/// Describes a generated class, struct, record, interface, enum, or delegate declaration.
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
	/// Gets whether the <c>abstract</c> modifier is emitted for a class or record class.
	/// </summary>
	/// <remarks>
	/// Abstract declarations take precedence over the default <see cref="IsSealed"/> value.
	/// </remarks>
	public bool IsAbstract { get; init; }

	/// <summary>
	/// Gets whether the <c>static</c> modifier is emitted for a class declaration.
	/// </summary>
	/// <remarks>
	/// Static classes cannot declare a base type, implement interfaces, or declare
	/// primary-constructor parameters. <see cref="IsSealed"/> is ignored when this value is
	/// <see langword="true"/>.
	/// </remarks>
	public bool IsStatic { get; init; }

	/// <summary>
	/// Gets whether the <c>readonly</c> modifier is emitted for a struct or record struct.
	/// </summary>
	public bool IsReadOnly { get; init; }

	/// <summary>
	/// Gets the optional base class or base record type.
	/// </summary>
	/// <remarks>Struct and record struct declarations cannot specify a base type.</remarks>
	public TypeReferenceOptions? BaseType { get; init; }

	/// <summary>Gets the optional enum underlying integral type.</summary>
	public TypeReferenceOptions? EnumUnderlyingType { get; init; }

	/// <summary>Gets the delegate return type.</summary>
	public TypeReferenceOptions? DelegateReturnType { get; init; }

	/// <summary>Gets the complete delegate parameter declarations.</summary>
	public ImmutableArray<ParameterDeclarationOptions> DelegateParameters { get; init; } = [];

	/// <summary>
	/// Gets the interfaces implemented by the generated type, or inherited by an interface.
	/// </summary>
	public ImmutableArray<TypeReferenceOptions> Interfaces { get; init; } = [];

	/// <summary>
	/// Gets the generic type parameters and their constraints.
	/// </summary>
	public ImmutableArray<GenericTypeParameterOptions> GenericTypes { get; init; } = [];

	/// <summary>
	/// Gets the primary-constructor parameters written after the type name and generic parameters.
	/// </summary>
	/// <remarks>Each entry is emitted verbatim as a complete parameter declaration.</remarks>
	public ImmutableArray<ParameterDeclarationOptions> PrimaryConstructorParameters { get; init; } = [];

	/// <summary>
	/// If <see  langword="true" />, the primary-constructor parameters are emitted on separate lines with one parameter per line.
	/// </summary>
	public bool ConstructorParametersOnSeparateLines { get; init; }

	/// <summary>
	/// Gets the attributes applied to the generated type.
	/// </summary>
	public ImmutableArray<AttributeDeclarationOptions> Attributes { get; init; } = [];

	/// <summary>
	/// Gets whether to emit <see cref="EmbeddedAttribute"/> on the type.
	/// When <see langword="null"/>, <c>WriteAttributeClass</c> enables it and other type-writing
	/// APIs leave it disabled. Set this explicitly to <see langword="false"/> to opt a generated
	/// attribute out of embedding.
	/// </summary>
	public bool? IncludeEmbeddedAttribute { get; init; }

	/// <summary>
	/// Gets whether to process any generated attributes, such as <see cref="System.CodeDom.Compiler.GeneratedCodeAttribute"/> and <see cref="System.Runtime.CompilerServices.CompilerGeneratedAttribute"/>.
	/// <see cref="IncludeEmbeddedAttribute"/> is ignored if this is <see langword="false"/>.
	/// When <see langword="null"/>, the value is inherited from <see cref="CodeWriter.DefaultIncludeGeneratedAttributes"/>.
	/// </summary>
	public bool? IncludeGeneratedAttributes { get; init; }
}
