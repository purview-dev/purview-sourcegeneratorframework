using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Provides an allocation-conscious writer for constructing generated C# source files.
/// </summary>
/// <remarks>
/// A writer owns its mutable buffer and is intended to be used for one generated source file.
/// Instances are not thread-safe and must not be shared between concurrent generator operations.
/// </remarks>
[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
public sealed class CodeWriter
{
	const char IndentCharacter = '\t';
	const char NewLineCharacter = '\n';
	const int DefaultCapacity = 4096;

	readonly StringBuilder _builder;
	int _indentLevel;
	bool _atLineStart = true;

	/// <summary>
	/// Initializes a new writer with the specified initial buffer capacity.
	/// </summary>
	/// <param name="initialCapacity">
	/// The initial number of characters that the internal buffer can contain without growing.
	/// </param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="initialCapacity"/> is less than zero.
	/// </exception>
	public CodeWriter(int initialCapacity = DefaultCapacity)
	{
		if (initialCapacity < 0)
			throw new ArgumentOutOfRangeException(nameof(initialCapacity));

		_builder = new StringBuilder(initialCapacity);
	}

	/// <summary>
	/// Gets the number of characters currently written.
	/// </summary>
	public int Length => _builder.Length;

	/// <summary>
	/// Increases the current indentation level.
	/// </summary>
	/// <returns>The current writer.</returns>
	public CodeWriter Indent()
	{
		_indentLevel++;
		return this;
	}

	/// <summary>
	/// Decreases the current indentation level.
	/// </summary>
	/// <returns>The current writer.</returns>
	/// <exception cref="InvalidOperationException">
	/// The current indentation level is zero.
	/// </exception>
	public CodeWriter Unindent()
	{
		if (_indentLevel == 0)
			throw new InvalidOperationException("Cannot unindent below zero.");

		_indentLevel--;
		return this;
	}

	/// <summary>
	/// Appends a deterministic line-feed character.
	/// </summary>
	/// <returns>The current writer.</returns>
	public CodeWriter NewLine()
	{
		_builder.Append(NewLineCharacter);
		_atLineStart = true;
		return this;
	}

	/// <summary>
	/// Moves to the start of a new line if content exists on the current line.
	/// </summary>
	/// <returns>The current writer.</returns>
	public CodeWriter EnsureNewLine()
	{
		return _atLineStart ? this : NewLine();
	}

	/// <summary>
	/// Writes an optional value followed by a line feed, applying the current indentation.
	/// </summary>
	/// <param name="value">The value to write, or <see langword="null"/> to write an empty line.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteLine(string? value = null)
	{
		if (value is null)
			return NewLine();

		WriteIndentIfRequired();
		_builder.Append(value);
		_builder.Append(NewLineCharacter);
		_atLineStart = true;
		return this;
	}

	/// <summary>
	/// Writes one XML documentation line.
	/// </summary>
	/// <param name="line">The documentation text.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteXml(string line)
	{
		return line is null
			? throw new ArgumentNullException(nameof(line))
			: Write("/// ").WriteLine(line);
	}

	/// <summary>
	/// Writes one or more XML documentation lines.
	/// </summary>
	/// <param name="xmlComment">The documentation lines.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteXml(params string[] xmlComment)
	{
		if (xmlComment is null || xmlComment.Length == 0)
			throw new ArgumentException("XML comment cannot be null or empty.", nameof(xmlComment));

		for (var index = 0; index < xmlComment.Length; index++)
			WriteXml(xmlComment[index]);

		return this;
	}

	/// <summary>
	/// Writes an XML <c>summary</c> documentation block.
	/// </summary>
	/// <param name="summary">The summary lines.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteXmlSummary(params string[] summary)
	{
		if (summary is null || summary.Length == 0)
			throw new ArgumentException("Summary cannot be null or empty.", nameof(summary));

		WriteLine("/// <summary>");
		for (var index = 0; index < summary.Length; index++)
			WriteXml(summary[index]);

		return WriteLine("/// </summary>");
	}

	/// <summary>
	/// Writes one comment line.
	/// </summary>
	/// <param name="comment">The comment text.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter Comment(string comment)
	{
		return comment is null
			? throw new ArgumentNullException(nameof(comment))
			: Write("// ").WriteLine(comment);
	}

	/// <summary>
	/// Writes a line comment or a multi-line comment block.
	/// </summary>
	/// <param name="comments">The comment lines.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter Comment(params string[] comments)
	{
		if (comments is null || comments.Length == 0)
			throw new ArgumentException("Comments cannot be null or empty.", nameof(comments));

		if (comments.Length == 1)
			return Comment(comments[0]);

		WriteLine("/*");
		for (var index = 0; index < comments.Length; index++)
			Write(" * ").WriteLine(comments[index]);

		return WriteLine(" */");
	}

	/// <summary>
	/// Writes the current indentation without writing content.
	/// </summary>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteIndent()
	{
		if (_indentLevel != 0)
			_builder.Append(IndentCharacter, _indentLevel);

		_atLineStart = false;
		return this;
	}

	/// <summary>
	/// Writes a value without a trailing line feed.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter Write(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return this;

		WriteIndentIfRequired();
		_builder.Append(value);
		return this;
	}

	/// <summary>
	/// Writes a character without a trailing line feed.
	/// </summary>
	/// <param name="value">The character to write.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter Write(char value)
	{
		WriteIndentIfRequired();
		_builder.Append(value);
		return this;
	}

	/// <summary>
	/// Writes a value without a trailing line feed.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter Append(string? value) => Write(value);

	/// <summary>
	/// Writes an optional value followed by a line feed.
	/// </summary>
	/// <param name="value">The value to write, or <see langword="null"/> for an empty line.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter AppendLine(string? value = null) => WriteLine(value);

	/// <summary>
	/// Writes a value when the supplied condition is true.
	/// </summary>
	/// <param name="condition">Whether to write the value.</param>
	/// <param name="value">The value to write.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteIf(bool condition, string? value) => condition ? Write(value) : this;

	/// <summary>
	/// Writes a line when the supplied condition is true.
	/// </summary>
	/// <param name="condition">Whether to write the line.</param>
	/// <param name="value">The value to write.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteLineIf(bool condition, string? value) =>
		condition ? WriteLine(value) : this;

	/// <summary>
	/// Writes a value surrounded by double quotes.
	/// </summary>
	/// <param name="value">The value to quote.</param>
	/// <returns>The current writer.</returns>
	/// <remarks>This method does not escape characters contained in <paramref name="value"/>.</remarks>
	public CodeWriter Quote(string? value = null)
	{
		Write('"');
		Write(value);
		return Write('"');
	}

	/// <summary>
	/// Writes a quoted value followed by a line feed.
	/// </summary>
	/// <param name="value">The value to quote.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter QuoteLine(string? value = null) => Quote(value).NewLine();

	/// <summary>
	/// Opens an indented scope and returns a value that closes it when disposed.
	/// </summary>
	/// <param name="header">Optional content written before the opening token.</param>
	/// <param name="separator">The opening token, or <see langword="null"/> for none.</param>
	/// <param name="closingSeparator">The closing token, or <see langword="null"/> to infer it.</param>
	/// <param name="additionalParts">Optional content appended to the header.</param>
	/// <returns>A scope that restores indentation and writes the closing token.</returns>
	public BlockScope Block(
		string? header = null,
		string? separator = "{",
		string? closingSeparator = null,
		Action<CodeWriter>? additionalParts = null
	)
	{
		if (header is not null)
		{
			Write(header);
			additionalParts?.Invoke(this);
			EnsureNewLine();
		}

		if (separator is not null)
			WriteLine(separator);

		Indent();
		closingSeparator ??= GetDefaultClosingToken(separator);
		return new BlockScope(this, closingSeparator);
	}

	/// <summary>
	/// Writes a complete scoped block by invoking the supplied body.
	/// </summary>
	/// <param name="header">Optional content written before the opening token.</param>
	/// <param name="body">The block body.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter Block(string? header, Action<CodeWriter> body)
	{
		if (body is null)
			throw new ArgumentNullException(nameof(body));

		using (Block(header))
			body(this);

		return this;
	}

	/// <summary>
	/// Writes a C# using directive.
	/// </summary>
	/// <param name="namespaceName">The namespace to import.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteUsing(string namespaceName)
	{
		return string.IsNullOrWhiteSpace(namespaceName)
			? throw new ArgumentException(
				"Namespace cannot be null or whitespace.",
				nameof(namespaceName)
			)
			: Write("using ").Write(namespaceName).WriteLine(";");
	}

	/// <summary>
	/// Writes a block-scoped namespace and returns its body scope.
	/// </summary>
	/// <param name="namespaceName">The namespace, or <see langword="null"/> to write nothing.</param>
	/// <returns>The namespace body scope, or an empty scope when no namespace is supplied.</returns>
	public BlockScope WriteBlockNamespace(string? namespaceName)
	{
		if (namespaceName is null)
			return default;

		Write("namespace ").WriteLine(namespaceName);
		return Block();
	}

	/// <summary>
	/// Writes a file-scoped namespace followed by an empty line.
	/// </summary>
	/// <param name="namespaceName">The namespace, or <see langword="null"/> to write nothing.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteFileScopedNamespace(string? namespaceName)
	{
		return namespaceName is null
			? this
			: Write("namespace ").Write(namespaceName).WriteLine(";").NewLine();
	}

	/// <summary>
	/// Writes a class declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The class declaration options.</param>
	/// <returns>The class body scope.</returns>
	public BlockScope WriteClass(TypeDeclarationOptions declaration)
	{
		return declaration is null
			? throw new ArgumentNullException(nameof(declaration))
			: WriteType(declaration with { Kind = TypeDeclarationKind.Class });
	}

	/// <summary>
	/// Writes a class declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The class declaration options.</param>
	/// <param name="bodyWriter">The action that writes the body of the class.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteClass(TypeDeclarationOptions declaration, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteClass(declaration))
			bodyWriter(this);

		return this;
	}

	/// <summary>
	/// Writes a struct declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The struct declaration options.</param>
	/// <returns>The struct body scope.</returns>
	public BlockScope WriteStruct(TypeDeclarationOptions declaration)
	{
		return declaration is null
			? throw new ArgumentNullException(nameof(declaration))
			: WriteType(declaration with { Kind = TypeDeclarationKind.Struct });
	}

	/// <summary>
	/// Writes a struct declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The struct declaration options.</param>
	/// <param name="bodyWriter">The action that writes the body of the struct.</param>
	/// <returns>The struct body scope.</returns>
	public CodeWriter WriteStruct(TypeDeclarationOptions declaration, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteStruct(declaration))
			bodyWriter(this);

		return this;
	}

	/// <summary>
	/// Writes a record class declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The record class declaration options.</param>
	/// <returns>The record body scope.</returns>
	public BlockScope WriteRecordClass(TypeDeclarationOptions declaration)
	{
		return declaration is null
			? throw new ArgumentNullException(nameof(declaration))
			: WriteType(declaration with { Kind = TypeDeclarationKind.RecordClass });
	}

	/// <summary>
	/// Writes a record class declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The record class declaration options.</param>
	/// <param name="bodyWriter">The action that writes the body of the record class.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteRecordClass(
		TypeDeclarationOptions declaration,
		Action<CodeWriter> bodyWriter
	)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteRecordClass(declaration))
			bodyWriter(this);

		return this;
	}

	/// <summary>
	/// Writes a record struct declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The record struct declaration options.</param>
	/// <returns>The record body scope.</returns>
	public BlockScope WriteRecordStruct(TypeDeclarationOptions declaration)
	{
		return declaration is null
			? throw new ArgumentNullException(nameof(declaration))
			: WriteType(declaration with { Kind = TypeDeclarationKind.RecordStruct });
	}

	/// <summary>
	/// Writes a record struct declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The record struct declaration options.</param>
	/// <param name="bodyWriter">The action that writes the body of the record struct.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteRecordStruct(
		TypeDeclarationOptions declaration,
		Action<CodeWriter> bodyWriter
	)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteRecordStruct(declaration))
			bodyWriter(this);

		return this;
	}

	/// <summary>
	/// Writes a class, struct, record class, or record struct declaration and returns its body scope.
	/// </summary>
	/// <param name="declaration">The structured type declaration options.</param>
	/// <returns>The generated type body scope.</returns>
	public BlockScope WriteType(TypeDeclarationOptions declaration)
	{
		if (declaration is null)
			throw new ArgumentNullException(nameof(declaration));

		ValidateTypeDeclaration(declaration);

		foreach (var attribute in declaration.TypeAttributes)
		{
			var startsWithBracket = attribute.StartsWith("[", StringComparison.Ordinal);
			if (!startsWithBracket)
				Write('[');

			Write(attribute);

			if (!startsWithBracket)
				Write(']');

			NewLine();
		}

		if (declaration.Accessibility is { } accessibility)
			WriteAccessibility(accessibility).Write(' ');

		var isStruct =
			declaration.Kind is TypeDeclarationKind.Struct or TypeDeclarationKind.RecordStruct;

		if (isStruct && declaration.IsReadOnly)
			Write("readonly ");
		else if (!isStruct && declaration.IsSealed)
			Write("sealed ");

		if (declaration.IsPartial)
			Write("partial ");

		Write(
				declaration.Kind switch
				{
					TypeDeclarationKind.Class => "class ",
					TypeDeclarationKind.Struct => "struct ",
					TypeDeclarationKind.RecordClass => "record class ",
					TypeDeclarationKind.RecordStruct => "record struct ",
					_ => throw new ArgumentOutOfRangeException(nameof(declaration)),
				}
			)
			.Write(declaration.Name);

		WriteGenericTypeParameters(declaration.GenericTypes);
		WriteParameterList(
			declaration.PrimaryConstructorParameters,
			declaration.ConstructorParametersOnSeparateLines
		);
		WriteBaseTypes(declaration);
		NewLine();
		WriteGenericConstraints(declaration.GenericTypes);

		return Block();
	}

	/// <summary>
	/// Writes an ordinary instance or static constructor and returns its body scope.
	/// </summary>
	/// <param name="declaration">The constructor declaration options.</param>
	/// <returns>The constructor body scope.</returns>
	public BlockScope WriteConstructor(ConstructorDeclarationOptions declaration)
	{
		if (declaration is null)
			throw new ArgumentNullException(nameof(declaration));

		ValidateConstructorDeclaration(declaration);

		if (declaration.IsStatic)
			Write("static ");
		else if (declaration.Accessibility is { } accessibility)
			WriteAccessibility(accessibility).Write(' ');

		Write(declaration.TypeName);
		WriteParameterList(
			declaration.Parameters,
			declaration.WriteParametersOnSeparateLines,
			writeWhenEmpty: true
		);

		if (!string.IsNullOrWhiteSpace(declaration.Initializer))
			Write(" : ").Write(declaration.Initializer);

		NewLine();
		return Block();
	}

	/// <summary>
	/// Writes the standard header for an automatically generated source file.
	/// </summary>
	/// <param name="generatorName">The optional generator name.</param>
	/// <param name="version">The optional generator version.</param>
	/// <param name="pragmas">Optional warning pragmas to disable.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteAutoGeneratedHeader(
		string? generatorName = null,
		string? version = null,
		params string[] pragmas
	)
	{
		WriteLine("// <auto-generated />");
		if (!string.IsNullOrEmpty(generatorName))
		{
			Write("// This code was generated by ").Write(generatorName);
			if (!string.IsNullOrEmpty(version))
				Write(" (version ").Write(version).Write(')');

			WriteLine(".");
		}

		WriteLine("// Changes to this file will be lost when the source generator runs again.")
			.NewLine()
			.WriteLine("#nullable enable");

		if (pragmas is not null && pragmas.Length > 0)
		{
			NewLine();

			foreach (var pragma in pragmas)
				Write("#pragma warning disable ").WriteLine(pragma);
		}

		return NewLine();
	}

	/// <summary>
	/// Writes a <see cref="System.CodeDom.Compiler.GeneratedCodeAttribute"/> declaration.
	/// </summary>
	/// <param name="generatorName">The generator name.</param>
	/// <param name="version">The generator version, defaulting to <c>1.0.0.0</c>.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteGeneratedCodeAttribute(string generatorName, string? version = null)
	{
		if (string.IsNullOrWhiteSpace(generatorName))
		{
			throw new ArgumentException(
				"Generator name cannot be null or whitespace.",
				nameof(generatorName)
			);
		}

		// All generated code attributes should have a version, but if one is not supplied, default to
		return Write("[global::System.CodeDom.Compiler.GeneratedCode(\"")
			.Write(generatorName)
			.Write("\", \"")
			.Write(version ?? "1.0.0.0")
			.WriteLine("\")]");
	}

	/// <summary>
	/// Writes each supplied part on its own line.
	/// </summary>
	/// <param name="parts">The lines to write.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter MultiLine(params string[] parts)
	{
		if (parts is null)
			throw new ArgumentNullException(nameof(parts));

		for (var index = 0; index < parts.Length; index++)
			WriteLine(parts[index]);

		return this;
	}

	/// <summary>
	/// Writes a multi-line parameter list ending with a closing parenthesis.
	/// </summary>
	/// <param name="parameters">The parameters to write.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter MultiLineParameters(params string[] parameters)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (parameters.Length == 0)
			return Write(')');

		NewLine();
		Indent();
		for (var index = 0; index < parameters.Length; index++)
		{
			Write(parameters[index]);
			WriteLine(index == parameters.Length - 1 ? ")" : ",");
		}

		return Unindent();
	}

	/// <summary>
	/// Writes a comma-separated collection with one item per line.
	/// </summary>
	/// <param name="items">The items to write.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter MultiLineItems(params string[] items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		for (var index = 0; index < items.Length; index++)
		{
			Write(items[index]);
			if (index != items.Length - 1)
				Write(',');

			NewLine();
		}

		return this;
	}

	/// <summary>
	/// Writes each non-null value on its own line.
	/// </summary>
	/// <param name="lines">The lines to write.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteLines(IEnumerable<string?> lines)
	{
		if (lines is null)
			throw new ArgumentNullException(nameof(lines));

		foreach (var line in lines)
		{
			if (line is not null)
				WriteLine(line);
		}

		return this;
	}

	/// <summary>
	/// Writes values separated by the specified delimiter.
	/// </summary>
	/// <param name="items">The values to write.</param>
	/// <param name="delimiter">The delimiter written between values.</param>
	/// <returns>The current writer.</returns>
	public CodeWriter WriteDelimited(IEnumerable<string?> items, string delimiter = ", ")
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));
		if (delimiter is null)
			throw new ArgumentNullException(nameof(delimiter));

		var first = true;
		foreach (var item in items)
		{
			if (!first)
				Write(delimiter);

			Write(item);
			first = false;
		}

		return this;
	}

	/// <summary>
	/// Increases indentation until the returned scope is disposed.
	/// </summary>
	/// <returns>A scope that restores the indentation level.</returns>
	public IndentScope Indented()
	{
		Indent();
		return new(this);
	}

	/// <summary>
	/// Writes a line and increases indentation until the returned scope is disposed.
	/// </summary>
	/// <param name="line">The line to write before indenting.</param>
	/// <returns>A scope that restores the indentation level.</returns>
	public IndentScope Indented(string line)
	{
		WriteLine(line);
		return Indented();
	}

	/// <summary>
	/// Resets the writer to an empty, unindented state.
	/// </summary>
	/// <returns>The current writer.</returns>
	public CodeWriter Begin()
	{
		_builder.Clear();
		_indentLevel = 0;
		_atLineStart = true;
		return this;
	}

	/// <summary>
	/// Creates the generated source string.
	/// </summary>
	/// <returns>The complete contents of the writer.</returns>
	public override string ToString() => _builder.ToString();

	/// <summary>
	/// Creates a <see cref="Microsoft.CodeAnalysis.Text.SourceText"/> from the writer's contents.
	/// </summary>
	[SuppressMessage("Design", "CA1062:Validate arguments of public methods")]
	public static implicit operator Microsoft.CodeAnalysis.Text.SourceText(CodeWriter writer) =>
		Microsoft.CodeAnalysis.Text.SourceText.From(writer.ToString(), Encoding.UTF8);

	void WriteIndentIfRequired()
	{
		if (!_atLineStart)
			return;

		if (_indentLevel != 0)
			_builder.Append(IndentCharacter, _indentLevel);

		_atLineStart = false;
	}

	CodeWriter WriteAccessibility(TypeDeclarationAccessibility accessibility)
	{
		return Write(
			accessibility switch
			{
				TypeDeclarationAccessibility.Public => "public",
				TypeDeclarationAccessibility.Internal => "internal",
				TypeDeclarationAccessibility.Protected => "protected",
				TypeDeclarationAccessibility.Private => "private",
				TypeDeclarationAccessibility.ProtectedInternal => "protected internal",
				TypeDeclarationAccessibility.PrivateProtected => "private protected",
				TypeDeclarationAccessibility.File => "file",
				_ => throw new ArgumentOutOfRangeException(nameof(accessibility)),
			}
		);
	}

	void WriteGenericTypeParameters(ImmutableArray<GenericTypeParameterOptions> genericTypes)
	{
		if (genericTypes.IsDefaultOrEmpty)
			return;

		Write('<');
		for (var index = 0; index < genericTypes.Length; index++)
		{
			if (index != 0)
				Write(", ");

			Write(genericTypes[index].Name);
		}

		Write('>');
	}

	void WriteParameterList(
		ImmutableArray<string> parameters,
		bool writeOnSeparateLines,
		bool writeWhenEmpty = false
	)
	{
		if (parameters.IsDefaultOrEmpty && !writeWhenEmpty)
			return;

		Write('(');
		if (!parameters.IsDefaultOrEmpty)
		{
			if (writeOnSeparateLines)
				Indent();

			for (var index = 0; index < parameters.Length; index++)
			{
				if (index != 0)
					Write(", ");

				if (writeOnSeparateLines)
					NewLine();

				Write(parameters[index]);
			}

			if (writeOnSeparateLines)
				Unindent().NewLine();
		}

		Write(')');
	}

	void WriteBaseTypes(TypeDeclarationOptions declaration)
	{
		var hasBaseType = !string.IsNullOrWhiteSpace(declaration.BaseType);
		if (!hasBaseType && declaration.Interfaces.IsDefaultOrEmpty)
			return;

		Write(" : ");
		if (hasBaseType)
			Write(declaration.BaseType);

		if (declaration.Interfaces.IsDefaultOrEmpty)
			return;

		for (var index = 0; index < declaration.Interfaces.Length; index++)
		{
			if (hasBaseType || index != 0)
				Write(", ");

			Write(declaration.Interfaces[index]);
		}
	}

	void WriteGenericConstraints(ImmutableArray<GenericTypeParameterOptions> genericTypes)
	{
		if (genericTypes.IsDefaultOrEmpty)
			return;

		for (var typeIndex = 0; typeIndex < genericTypes.Length; typeIndex++)
		{
			var genericType = genericTypes[typeIndex];
			if (genericType.Constraints.IsDefaultOrEmpty)
				continue;

			Write("where ").Write(genericType.Name).Write(" : ");
			for (
				var constraintIndex = 0;
				constraintIndex < genericType.Constraints.Length;
				constraintIndex++
			)
			{
				if (constraintIndex != 0)
					Write(", ");

				Write(genericType.Constraints[constraintIndex]);
			}

			NewLine();
		}
	}

	static void ValidateTypeDeclaration(TypeDeclarationOptions declaration)
	{
		var isStruct =
			declaration.Kind is TypeDeclarationKind.Struct or TypeDeclarationKind.RecordStruct;

		if (isStruct && !string.IsNullOrWhiteSpace(declaration.BaseType))
			throw new ArgumentException(
				"Struct and record struct declarations cannot specify a base type.",
				nameof(declaration)
			);

		if (!isStruct && declaration.IsReadOnly)
			throw new ArgumentException(
				"Only struct and record struct declarations can be readonly.",
				nameof(declaration)
			);

		ValidateParameters(
			declaration.PrimaryConstructorParameters,
			"Primary-constructor parameters cannot contain null or whitespace values.",
			nameof(declaration)
		);

		for (
			var index = 0;
			!declaration.Interfaces.IsDefaultOrEmpty && index < declaration.Interfaces.Length;
			index++
		)
		{
			if (string.IsNullOrWhiteSpace(declaration.Interfaces[index]))
				throw new ArgumentException(
					"Interface names cannot be null or whitespace.",
					nameof(declaration)
				);
		}

		if (declaration.GenericTypes.IsDefaultOrEmpty)
			return;

		for (var typeIndex = 0; typeIndex < declaration.GenericTypes.Length; typeIndex++)
		{
			var genericType =
				declaration.GenericTypes[typeIndex]
				?? throw new ArgumentException(
					"Generic type parameters cannot contain null values.",
					nameof(declaration)
				);
			if (genericType.Constraints.IsDefaultOrEmpty)
				continue;

			for (
				var constraintIndex = 0;
				constraintIndex < genericType.Constraints.Length;
				constraintIndex++
			)
			{
				if (string.IsNullOrWhiteSpace(genericType.Constraints[constraintIndex]))
					throw new ArgumentException(
						"Generic constraints cannot be null or whitespace.",
						nameof(declaration)
					);
			}
		}
	}

	static void ValidateConstructorDeclaration(ConstructorDeclarationOptions declaration)
	{
		ValidateParameters(
			declaration.Parameters,
			"Constructor parameters cannot contain null or whitespace values.",
			nameof(declaration)
		);

		if (declaration.IsStatic && !declaration.Parameters.IsDefaultOrEmpty)
			throw new ArgumentException(
				"A static constructor cannot declare parameters.",
				nameof(declaration)
			);

		if (declaration.IsStatic && !string.IsNullOrWhiteSpace(declaration.Initializer))
			throw new ArgumentException(
				"A static constructor cannot specify an initializer.",
				nameof(declaration)
			);

		if (declaration.IsStatic && declaration.Accessibility is not null)
			throw new ArgumentException(
				"A static constructor cannot specify accessibility.",
				nameof(declaration)
			);
	}

	static void ValidateParameters(
		ImmutableArray<string> parameters,
		string message,
		string parameterName
	)
	{
		if (parameters.IsDefaultOrEmpty)
			return;

		for (var index = 0; index < parameters.Length; index++)
		{
			if (string.IsNullOrWhiteSpace(parameters[index]))
				throw new ArgumentException(message, parameterName);
		}
	}

	static string? GetDefaultClosingToken(string? openingToken)
	{
		return openingToken switch
		{
			"{" => "}",
			"(" => ")",
			"[" => "]",
			_ => null,
		};
	}

	/// <summary>
	/// Restores a writer's indentation when disposed.
	/// </summary>
	[SuppressMessage(
		"Performance",
		"CA1815:Override equals and operator equals on value types",
		Justification = "This type is a mutable lifetime token and has no meaningful value equality."
	)]
	public struct IndentScope(CodeWriter writer) : IDisposable
	{
		CodeWriter? _writer = writer;

		/// <summary>
		/// Restores the indentation level once.
		/// </summary>
		public void Dispose()
		{
			var writer = _writer;
			if (writer is null)
				return;

			_writer = null;
			writer.Unindent();
		}
	}

	/// <summary>
	/// Restores indentation and writes a block's closing token when disposed.
	/// </summary>
	[SuppressMessage(
		"Performance",
		"CA1815:Override equals and operator equals on value types",
		Justification = "This type is a mutable lifetime token and has no meaningful value equality."
	)]
	public struct BlockScope(CodeWriter writer, string? closingSeparator) : IDisposable
	{
		CodeWriter? _writer = writer;

		/// <summary>
		/// Closes the block once.
		/// </summary>
		public void Dispose()
		{
			var writer = _writer;
			if (writer is null)
				return;

			_writer = null;
			writer.Unindent();
			if (closingSeparator is not null)
				writer.WriteLine(closingSeparator);
		}
	}
}
