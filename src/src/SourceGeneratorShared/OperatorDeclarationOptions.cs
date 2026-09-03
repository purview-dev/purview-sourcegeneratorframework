using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>Describes a generated operator declaration.</summary>
/// <param name="OperatorToken">The operator token such as <c>==</c>, <c>&lt;</c>, or <c>&lt;=</c>.</param>
/// <param name="ReturnType">The operator return type, typically <see cref="PurviewTypeLibrary.System.Boolean"/>.</param>
/// <param name="Left">The left operand parameter.</param>
/// <param name="Right">The right operand parameter.</param>
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
	ParameterDeclarationOptions Right,
	TypeDeclarationAccessibility? Accessibility = null,
	bool IsStatic = true,
	string? ExpressionBody = null,
	ImmutableArray<AttributeDeclarationOptions> Attributes = default,
	bool? IncludeGeneratedAttributes = null
);
