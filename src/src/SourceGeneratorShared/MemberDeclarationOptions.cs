using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>Describes a generated method declaration.</summary>
public readonly record struct MethodDeclarationOptions
{
	/// <summary>Creates a method declaration.</summary>
	/// <param name="name">The method name.</param>
	/// <param name="returnType">The return type. The default is <c>void</c>.</param>
	/// <param name="accessibility">The optional accessibility.</param>
	public MethodDeclarationOptions(
		string name,
		TypeReferenceOptions returnType,
		TypeDeclarationAccessibility? accessibility = null
	)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Method name cannot be null or whitespace.", nameof(name));

		Name = name;
		ReturnType = returnType;
		Accessibility = accessibility;
	}

	/// <summary>Creates a method declaration with the <see cref="ReturnType"/> set to <c>void</c>.</summary>
	/// <param name="name">The method name.</param>
	/// <param name="accessibility">The optional accessibility.</param>
	public MethodDeclarationOptions(string name, TypeDeclarationAccessibility? accessibility = null)
		: this(name, PurviewTypeLibrary.System.Void, accessibility) { }

	/// <summary>Gets the method name.</summary>
	public string Name { get; }

	/// <summary>Gets the return type.</summary>
	public TypeReferenceOptions ReturnType { get; }

	/// <summary>Gets the optional accessibility.</summary>
	public TypeDeclarationAccessibility? Accessibility { get; init; }

	/// <summary>Gets whether the method is static.</summary>
	public bool IsStatic { get; init; }

	/// <summary>Gets whether the method is partial.</summary>
	public bool IsPartial { get; init; }

	/// <summary>Gets whether the method is abstract.</summary>
	public bool IsAbstract { get; init; }

	/// <summary>Gets whether the method is virtual.</summary>
	public bool IsVirtual { get; init; }

	/// <summary>Gets whether the method is an override.</summary>
	public bool IsOverride { get; init; }

	/// <summary>Gets whether the method is sealed.</summary>
	public bool IsSealed { get; init; }

	/// <summary>Gets whether the method is asynchronous.</summary>
	public bool IsAsync { get; init; }

	/// <summary>Gets whether the method is unsafe.</summary>
	public bool IsUnsafe { get; init; }

	/// <summary>Gets the complete parameter declarations.</summary>
	public ImmutableArray<ParameterDeclarationOptions> Parameters { get; init; }

	/// <summary>Gets attributes applied to the method.</summary>
	public ImmutableArray<AttributeDeclarationOptions> Attributes { get; init; }

	/// <summary>Gets attributes applied to the return value.</summary>
	public ImmutableArray<AttributeDeclarationOptions> ReturnAttributes { get; init; }

	/// <summary>Gets generic parameters and constraints.</summary>
	public ImmutableArray<GenericTypeParameterOptions> GenericTypes { get; init; }

	/// <summary>Gets an optional expression body without the leading <c>=&gt;</c>.</summary>
	public string? ExpressionBody { get; init; }

	/// <summary>
	/// Gets whether to emit generated attributes such as <see cref="System.CodeDom.Compiler.GeneratedCodeAttribute"/> and
	/// <see cref="System.Runtime.CompilerServices.CompilerGeneratedAttribute"/>.
	/// When <see langword="null"/>, the value is inherited from <see cref="CodeWriter.DefaultIncludeGeneratedAttributes"/>.
	/// </summary>
	public bool? IncludeGeneratedAttributes { get; init; }
}

/// <summary>Describes a generated property declaration.</summary>
public readonly record struct PropertyDeclarationOptions
{
	/// <summary>Creates a property declaration.</summary>
	public PropertyDeclarationOptions(
		string name,
		TypeReferenceOptions type,
		TypeDeclarationAccessibility? accessibility = null
	)
	{
		Name = name;
		Type = type;
		Accessibility = accessibility;
	}

	/// <summary>Gets the property name.</summary>
	public string Name { get; }

	/// <summary>Gets the property type.</summary>
	public TypeReferenceOptions Type { get; }

	/// <summary>Gets the optional accessibility.</summary>
	public TypeDeclarationAccessibility? Accessibility { get; init; }

	/// <summary>Gets whether the property is static.</summary>
	public bool IsStatic { get; init; }

	/// <summary>Gets whether the property is abstract.</summary>
	public bool IsAbstract { get; init; }

	/// <summary>Gets whether the property is virtual.</summary>
	public bool IsVirtual { get; init; }

	/// <summary>Gets whether the property is an override.</summary>
	public bool IsOverride { get; init; }

	/// <summary>Gets whether the property is sealed.</summary>
	public bool IsSealed { get; init; }

	/// <summary>Gets whether a getter is emitted. The default is <see langword="true"/>.</summary>
	public bool HasGetter { get; init; } = true;

	/// <summary>Gets whether a setter or init accessor is emitted.</summary>
	public bool HasSetter { get; init; }

	/// <summary>
	/// Gets whether the setter is emitted as an init accessor. Setting this to <see langword="true"/>
	/// implicitly enables the setter accessor even when <see cref="HasSetter"/> is <see langword="false"/>.
	/// </summary>
	public bool IsInitOnly { get; init; }

	/// <summary>Gets optional getter accessibility.</summary>
	public TypeDeclarationAccessibility? GetterAccessibility { get; init; }

	/// <summary>Gets optional setter accessibility.</summary>
	public TypeDeclarationAccessibility? SetterAccessibility { get; init; }

	/// <summary>Gets an optional expression body without the leading <c>=&gt;</c>.</summary>
	public string? ExpressionBody { get; init; }

	/// <summary>Gets an optional initializer without the leading equals sign.</summary>
	public string? Initializer { get; init; }

	/// <summary>Gets attributes applied to the property.</summary>
	public ImmutableArray<AttributeDeclarationOptions> Attributes { get; init; }

	/// <summary>
	/// Gets whether to emit generated attributes such as <see cref="System.CodeDom.Compiler.GeneratedCodeAttribute"/> and
	/// <see cref="System.Runtime.CompilerServices.CompilerGeneratedAttribute"/>.
	/// When <see langword="null"/>, the value is inherited from <see cref="CodeWriter.DefaultIncludeGeneratedAttributes"/>.
	/// </summary>
	public bool? IncludeGeneratedAttributes { get; init; }
}

/// <summary>Describes a generated field declaration.</summary>
public readonly record struct FieldDeclarationOptions
{
	/// <summary>Creates a field declaration.</summary>
	public FieldDeclarationOptions(
		string name,
		TypeReferenceOptions type,
		TypeDeclarationAccessibility? accessibility = null
	)
	{
		Name = name;
		Type = type;
		Accessibility = accessibility;
	}

	/// <summary>Gets the field name.</summary>
	public string Name { get; }

	/// <summary>Gets the field type.</summary>
	public TypeReferenceOptions Type { get; }

	/// <summary>Gets the optional accessibility.</summary>
	public TypeDeclarationAccessibility? Accessibility { get; init; }

	/// <summary>Gets whether the field is static.</summary>
	public bool IsStatic { get; init; }

	/// <summary>Gets whether the field is readonly.</summary>
	public bool IsReadOnly { get; init; }

	/// <summary>Gets whether the field is constant.</summary>
	public bool IsConst { get; init; }

	/// <summary>Gets whether the field is volatile.</summary>
	public bool IsVolatile { get; init; }

	/// <summary>Gets an optional initializer without the leading equals sign.</summary>
	public string? Initializer { get; init; }

	/// <summary>Gets attributes applied to the field.</summary>
	public ImmutableArray<AttributeDeclarationOptions> Attributes { get; init; }

	/// <summary>
	/// Gets whether to emit generated attributes such as <see cref="System.CodeDom.Compiler.GeneratedCodeAttribute"/> and
	/// <see cref="System.Runtime.CompilerServices.CompilerGeneratedAttribute"/>.
	/// When <see langword="null"/>, the value is inherited from <see cref="CodeWriter.DefaultIncludeGeneratedAttributes"/>.
	/// </summary>
	public bool? IncludeGeneratedAttributes { get; init; }
}
