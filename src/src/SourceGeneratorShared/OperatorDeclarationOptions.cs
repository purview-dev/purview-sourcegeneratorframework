using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Identifies the shape of a generated operator declaration.
/// </summary>
public enum OperatorDeclarationKind
{
	/// <summary>
	/// A binary operator such as <c>==</c>, <c>&lt;</c>, or <c>+</c>, taking two operands.
	/// </summary>
	Binary,

	/// <summary>
	/// A unary operator such as <c>-</c> or <c>!</c>, taking a single operand.
	/// </summary>
	Unary,

	/// <summary>
	/// An <c>implicit</c> conversion operator.
	/// </summary>
	ImplicitConversion,

	/// <summary>
	/// An <c>explicit</c> conversion operator.
	/// </summary>
	ExplicitConversion,
}

/// <summary>
/// Describes a generated operator declaration.
/// </summary>
/// <param name="OperatorToken">
/// The operator token such as <c>==</c>, <c>&lt;</c>, or <c>&lt;=</c>. Ignored for conversion operators.
/// </param>
/// <param name="ReturnType">
/// The operator return type: the result for binary/unary operators, or the target type for conversion operators.
/// </param>
/// <param name="Left">
/// The left operand parameter, or the single source parameter for unary and conversion operators.
/// </param>
/// <param name="Right">The right operand parameter. Only used for <see cref="OperatorDeclarationKind.Binary"/>.</param>
/// <param name="Accessibility">The optional accessibility modifier, or <see langword="null"/> to omit accessibility.</param>
/// <param name="IsStatic">
/// Whether the <c>static</c> keyword is emitted. Operators are implicitly static; the default is
/// <see langword="true"/> and matches the convention of explicit static emission.
/// </param>
/// <param name="ExpressionBody">An optional expression body without the leading <c>=&gt;</c>.</param>
/// <param name="Attributes">Attributes applied to the operator.</param>
/// <param name="IncludeGeneratedAttributes">
/// Whether to emit generated attributes. When <see langword="null"/>, the value is inherited from
/// <see cref="CodeWriter.DefaultIncludeGeneratedAttributes"/>.
/// </param>
public readonly record struct OperatorDeclarationOptions(
	string OperatorToken,
	TypeReference ReturnType,
	ParameterDeclarationOptions Left,
	ParameterDeclarationOptions Right = default,
	TypeDeclarationAccessibility? Accessibility = null,
	bool IsStatic = true,
	string? ExpressionBody = null,
	ImmutableArray<AttributeDeclarationOptions> Attributes = default,
	bool? IncludeGeneratedAttributes = null
)
{
	/// <summary>
	/// Gets the operator shape, which controls how the header is emitted.
	/// </summary>
	public OperatorDeclarationKind Kind { get; init; } = OperatorDeclarationKind.Binary;
}
