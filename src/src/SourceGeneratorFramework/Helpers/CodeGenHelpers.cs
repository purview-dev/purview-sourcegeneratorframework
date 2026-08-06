namespace Purview.SourceGeneratorFramework.Helpers;

/// <summary>
/// Provides extension methods for generating common code patterns with a <see cref="CodeWriter"/>.
/// </summary>
public static class CodeGenHelpers
{
	/// <summary>
	/// Writes a method declaration, invokes the body action inside the block, and closes the block.
	/// </summary>
	public static CodeWriter WriteMethod(
		this CodeWriter writer,
		string signature,
		Action<CodeWriter> body
	)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));
		if (string.IsNullOrWhiteSpace(signature))
			throw new ArgumentException(
				"Signature cannot be null or whitespace.",
				nameof(signature)
			);
		if (body == null)
			throw new ArgumentNullException(nameof(body));

		writer.WriteLine(signature);
		using (var scope = writer.Block())
			body(writer);

		return writer;
	}

	/// <summary>
	/// Writes a constructor declaration, invokes the body action inside the block, and closes the block.
	/// </summary>
	public static CodeWriter WriteConstructor(
		this CodeWriter writer,
		string signature,
		Action<CodeWriter> body
	)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));
		if (string.IsNullOrWhiteSpace(signature))
			throw new ArgumentException(
				"Signature cannot be null or whitespace.",
				nameof(signature)
			);
		if (body == null)
			throw new ArgumentNullException(nameof(body));

		writer.WriteLine(signature);
		using (var scope = writer.Block())
			body(writer);

		return writer;
	}

	/// <summary>
	/// Writes a property declaration with optional getter and setter accessors.
	/// </summary>
	public static CodeWriter WriteProperty(
		this CodeWriter writer,
		string declaration,
		string? getter = "get;",
		string? setter = null
	)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));
		if (string.IsNullOrWhiteSpace(declaration))
			throw new ArgumentException(
				"Declaration cannot be null or whitespace.",
				nameof(declaration)
			);

		writer.WriteLine(declaration);
		using (var scope = writer.Block())
		{
			if (getter != null)
				writer.WriteLine(getter);
			if (setter != null)
				writer.WriteLine(setter);
		}

		return writer;
	}

	/// <summary>
	/// Writes a field declaration.
	/// </summary>
	public static CodeWriter WriteField(this CodeWriter writer, string declaration)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));
		if (string.IsNullOrWhiteSpace(declaration))
			throw new ArgumentException(
				"Declaration cannot be null or whitespace.",
				nameof(declaration)
			);

		// Ensure the declaration ends with a semicolon
		return writer.WriteLine(
			declaration.EndsWith(";", StringComparison.Ordinal) ? declaration : declaration + ";"
		);
	}

	/// <summary>
	/// Writes an attribute declaration with optional arguments.
	/// </summary>
	public static CodeWriter WriteAttribute(
		this CodeWriter writer,
		string attributeName,
		params string[] arguments
	)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));
		if (string.IsNullOrWhiteSpace(attributeName))
		{
			throw new ArgumentException(
				"Attribute name cannot be null or whitespace.",
				nameof(attributeName)
			);
		}
		if (arguments == null)
			throw new ArgumentNullException(nameof(arguments));

		// All valid...
		return arguments.Length == 0
			? writer.WriteLine($"[{attributeName}]")
			: writer.WriteLine($"[{attributeName}({string.Join(", ", arguments)})]");
	}
}
