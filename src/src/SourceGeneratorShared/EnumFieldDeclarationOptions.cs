using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

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
