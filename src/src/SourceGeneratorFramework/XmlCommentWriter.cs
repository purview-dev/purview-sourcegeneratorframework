using System.ComponentModel;
using System.Text;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Provides extension methods for writing XML documentation comments to a <see cref="CodeWriter"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1034:Nested types should not be visible"
)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Naming",
	"CA1708:Identifiers should differ by more than case",
	Justification = "Instance vs. Static"
)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class XmlCommentWriter
{
	extension(CodeWriter writer)
	{
		/// <summary>
		/// Writes one or more XML documentation lines.
		/// </summary>
		/// <param name="comments">The documentation lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlComment(params string[] comments) =>
			XmlInternal(writer, null, comments);

		/// <summary>
		/// Writes an XML documentation block with the specified tag and content.
		/// </summary>
		/// <param name="tag">The XML tag name.</param>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		/// <exception cref="ArgumentException">If the <paramref name="tag"/> is <see langword="null"/> or an empty string.</exception>
		public CodeWriter Xml(string tag, params string[] content) =>
			string.IsNullOrWhiteSpace(tag)
				? throw new ArgumentException(
					"The XML tag name cannot be null or empty.",
					nameof(tag)
				)
				: XmlInternal(writer, tag, content);

		/// <summary>
		/// Writes an XML <c>cref</c> documentation block with the specified type name and content.
		/// </summary>
		/// <param name="typeName">The name of the type.</param>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		/// <exception cref="ArgumentException">If the <paramref name="typeName"/> is <see langword="null"/> or an empty string.</exception>
		public CodeWriter XmlCref(string typeName, string content) =>
			string.IsNullOrWhiteSpace(typeName)
				? throw new ArgumentException(
					"The XML cref cannot be null or empty.",
					nameof(typeName)
				)
				: XmlInternal(writer, BuildXmlTag("cref", ("cref", typeName)), "cref", content);

		/// <summary>
		/// Writes an XML <c>&lt;returns&gt;</c> documentation block with the specified content.
		/// </summary>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlReturn(params string[] content) =>
			XmlInternal(writer, "returns", content);

		/// <summary>
		/// Writes an XML <c>&lt;returns&gt;</c> documentation block with the specified content.
		/// </summary>
		/// <param name="exceptionType">The type of the exception.</param>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlException(string exceptionType, params string[] content) =>
			string.IsNullOrWhiteSpace(exceptionType)
				? throw new ArgumentException(
					"The XML exception type cannot be null or empty.",
					nameof(exceptionType)
				)
				: XmlInternal(
					writer,
					BuildXmlTag("exception", ("cref", exceptionType)),
					"exception",
					content
				);

		/// <summary>
		/// Writes an XML <c>&lt;returns&gt;</c> documentation block with the specified content.
		/// </summary>
		/// <param name="exceptionType">The type of the exception.</param>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlException(TypeValueObject exceptionType, params string[] content) =>
			XmlInternal(
				writer,
				BuildXmlTag("exception", ("cref", exceptionType)),
				"exception",
				content
			);

		/// <summary>
		/// Writes an XML <c>cref</c> documentation block with the specified type name and content.
		/// </summary>
		/// <param name="typeName">The name of the type.</param>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlCref(TypeValueObject typeName, params string[] content) =>
			XmlInternal(writer, BuildXmlTag("cref", ("cref", typeName)), "cref", content);

		/// <summary>
		/// Writes an XML <c>&lt;c&gt;</c> documentation block with the specified content.
		/// </summary>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlCode(params string[] content) => XmlInternal(writer, "c", content);

		/// <summary>
		/// Writes an XML <c>&lt;example&gt;</c> documentation block with the specified content.
		/// </summary>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlExample(params string[] content) =>
			XmlInternal(writer, "example", content);

		/// <summary>
		/// Writes an XML <c>&lt;list&gt;</c> documentation block with the specified content.
		/// Defaults to a bullet list type.
		/// </summary>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlList(params string[] content) =>
			XmlList(writer, "bullet", null, content);

		/// <summary>
		/// Writes an XML <c>&lt;list&gt;</c> documentation block with the specified content.
		/// Defaults to a bullet list type.
		/// </summary>
		/// <param name="description">The description of the list.</param>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlList(string description, params string[] content) =>
			XmlList(writer, "bullet", description, content);

		/// <summary>
		/// Writes an XML <c>&lt;list&gt;</c> documentation block with the specified content.
		/// </summary>
		/// <param name="listType">The type of list, such as "bullet" or "number".</param>
		/// <param name="description">Optional description of the list.</param>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlList(string listType, string? description, params string[] content)
		{
			if (string.IsNullOrWhiteSpace(listType))
				throw new ArgumentException(
					"The list type cannot be null or empty.",
					nameof(listType)
				);

			var startTag = BuildXmlTag("list", ("type", listType));
			if (content is not null)
				content = [.. content.Select(line => "<item>" + line + "</item>")];

			return XmlInternal(
				writer,
				startTag,
				"list",
				content,
				insideStart: string.IsNullOrWhiteSpace(description)
					? null
					: w => w.Xml("description", description!),
				null,
				true
			);
		}

		/// <summary>
		/// Writes an XML <c>&lt;para&gt;</c> documentation block with the specified content.
		/// </summary>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlPara(params string[] content) =>
			XmlInternal(writer, startTag: "para", endTag: "para", content: content);

		/// <summary>
		/// Writes an XML <c>&lt;param&gt;</c> documentation block with the specified parameter name and content.
		/// </summary>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="content">The content lines.</param>
		/// <returns>The current writer.</returns>
		/// <exception cref="ArgumentException">If the <paramref name="parameterName"/> is <see langword="null"/> or an empty string.</exception>
		public CodeWriter XmlParam(string parameterName, params string[] content)
		{
			if (string.IsNullOrWhiteSpace(parameterName))
				throw new ArgumentException(
					"The parameter name cannot be null or empty.",
					nameof(parameterName)
				);

			var tag = "param";
			var startTag = BuildXmlTag(tag, ("name", parameterName));

			return XmlInternal(writer, startTag, tag, content);
		}

		CodeWriter XmlInternal(string? startTag, string? endTag, string content) =>
			string.IsNullOrWhiteSpace(content)
				? writer
				: XmlInternal(writer, startTag, endTag, [content], false);

		CodeWriter XmlInternal(
			string? startTag,
			string? endTag,
			string[]? content,
			bool supportsMultiLine = true
		) => XmlInternal(writer, startTag, endTag, content, null, null, supportsMultiLine);

		CodeWriter XmlInternal(
			string? startTag,
			string? endTag,
			string[]? content,
			Action<CodeWriter>? insideStart,
			Action<CodeWriter>? insideEnd,
			bool supportsMultiLine = true
		)
		{
			if (content is null || content.Length == 0)
				return writer;
			if (!supportsMultiLine)
			{
				if (content.Length > 1)
				{
					throw new ArgumentException(
						"Multiple lines are not supported for this XML tag.",
						nameof(content)
					);
				}
			}

			endTag ??= startTag;

			var isMultiLine = startTag is not null;
			if (startTag is not null)
				writer.Write(
					startTag.StartsWith("<", StringComparison.Ordinal)
						? $"/// {startTag}"
						: $"/// <{startTag}>"
				);
			if (isMultiLine)
				writer.NewLine();

			insideStart?.Invoke(writer);

			foreach (var line in content)
				writer.Write("/// ").WriteLine(line);

			if (endTag is not null)
				writer.Write("/// </").Write(endTag).WriteLine(">");

			insideEnd?.Invoke(writer);

			return writer;
		}

		CodeWriter XmlInternal(string? tag, params string[] content) =>
			XmlInternal(writer, startTag: tag, endTag: tag, content: content);

		/// <summary>
		/// Writes an XML <c>summary</c> documentation block.
		/// </summary>
		/// <param name="summary">The summary lines.</param>
		/// <returns>The current writer.</returns>
		public CodeWriter XmlSummary(params string[] summary) =>
			XmlInternal(writer, startTag: "summary", endTag: "summary", content: summary);
	}

	extension(CodeWriter)
	{
		/// <summary>
		/// Builds an XML tag with optional attributes.
		/// </summary>
		/// <param name="tag">The XML tag name.</param>
		/// <param name="attributes">The XML attributes.</param>
		/// <returns>The constructed XML tag.</returns>
		/// <exception cref="ArgumentException">If the <paramref name="tag"/> is <see langword="null"/> or an empty string.	</exception>
		public static string BuildXmlTag(
			string tag,
			params (string Name, object Value)[]? attributes
		)
		{
			if (string.IsNullOrWhiteSpace(tag))
				throw new ArgumentException(
					"The XML tag name cannot be null or empty.",
					nameof(tag)
				);

			StringBuilder builder = new(tag.Length + 2);
			builder.Append('<').Append(tag);
			if (attributes is not null && attributes.Length > 0)
			{
				foreach (var (name, value) in attributes)
				{
					var attributeValue = value;
					if (value is bool)
					{
#pragma warning disable CA1308 // Normalize strings to uppercase
						attributeValue = value.ToString().ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
					}

					builder
						.Append(' ')
						.Append(name)
						.Append("=\"")
						.Append(attributeValue)
						.Append('"');
				}
			}

			builder.Append('>');

			return builder.ToString();
		}
	}
}
