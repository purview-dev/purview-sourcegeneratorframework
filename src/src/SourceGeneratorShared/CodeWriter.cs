using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
public sealed partial class CodeWriter
{
	const char IndentCharacter = '\t';
	const char NewLineCharacter = '\n';
	const int DefaultCapacity = 4096;
	const int IndentDisplayWidth = 4;
	const int DefaultMaximumLineLength = 100;

	int _indentLevel;
	int _nextScopeId;
	int _lastWrittenItemIndent = -1;
	int _lastWrittenItemEnd;
	WrittenItemKind _lastWrittenItem;
	bool _atLineStart = true;

	readonly StringBuilder _builder;
	readonly Dictionary<int, CodeWriterOpenScope>? _openScopes;

	/// <summary>
	/// Initializes a new writer with required generator identity.
	/// </summary>
	/// <param name="generatorName">The source generator name used by headers and attributes.</param>
	/// <param name="generatorVersion">The source generator version used by headers and attributes.</param>
	/// <param name="initialCapacity">
	/// The initial number of characters that the internal buffer can contain without growing.
	/// </param>
	/// <param name="throwOnUnclosedScopes">
	/// Whether materializing the generated source throws while disposable scopes remain open.
	/// The default is <see langword="true"/>; set to <see langword="false"/> only for best-effort
	/// diagnostic output when a scope imbalance is expected and must not terminate generation.
	/// </param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="initialCapacity"/> is less than zero.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="generatorName"/> or <paramref name="generatorVersion"/> is empty or whitespace.
	/// </exception>
	public CodeWriter(
		string generatorName,
		string generatorVersion,
		int initialCapacity = DefaultCapacity,
		bool throwOnUnclosedScopes = true
	)
	{
		if (initialCapacity < 0)
			throw new ArgumentOutOfRangeException(nameof(initialCapacity));

		_builder = new StringBuilder(initialCapacity);
		GeneratorName =
			NormalizeOptionalIdentity(generatorName, nameof(generatorName))
			?? throw new ArgumentException("Generator name cannot be null or whitespace.", nameof(generatorName));
		GeneratorVersion =
			NormalizeOptionalIdentity(generatorVersion, nameof(generatorVersion))
			?? throw new ArgumentException("Generator version cannot be null or whitespace.", nameof(generatorVersion));
		ThrowOnUnclosedScopes = throwOnUnclosedScopes;
		if (throwOnUnclosedScopes)
			_openScopes = [];
	}

	/// <summary>
	/// Gets the number of characters currently written.
	/// </summary>
	public int Length => _builder.Length;

	/// <summary>
	/// Gets the number of block or indentation scopes that have been opened but not disposed.
	/// </summary>
	public int OpenScopeCount { get; private set; }

	/// <summary>
	/// Gets whether <see cref="ToString"/> throws when disposable scopes remain open.
	/// </summary>
	/// <remarks>
	/// This value is set at construction. When validation is enabled, the writer tracks every
	/// opened scope so that an unclosed scope throws <see cref="CodeWriterScopeValidationException"/>
	/// with the offending scope's stack trace when the generated source is materialized. When
	/// disabled, the writer still tracks scopes for <see cref="OpenScopeCount"/> but does not throw,
	/// allowing best-effort inspection of structurally broken output during debugging.
	/// </remarks>
	public bool ThrowOnUnclosedScopes { get; }

	/// <summary>Gets the source generator name used by generated headers and attributes.</summary>
	public string GeneratorName { get; }

	/// <summary>Gets the source generator version used by generated headers and attributes.</summary>
	public string GeneratorVersion { get; }

	/// <summary>
	/// Gets or sets whether generated attributes are emitted for declarations that do not
	/// explicitly override <see cref="TypeDeclarationOptions.IncludeGeneratedAttributes"/>,
	/// <see cref="MethodDeclarationOptions.IncludeGeneratedAttributes"/>, or other declaration
	/// option equivalents. The default is <see langword="true"/>.
	/// </summary>
	public bool DefaultIncludeGeneratedAttributes { get; set; } = true;

	/// <summary>
	/// Increases the current indentation level.
	/// </summary>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.Indent().WriteLine("value");</code></example>
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
	/// <example><code>writer.Indent().WriteLine("value").Unindent();</code></example>
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
	/// <example><code>writer.Write("first").NewLine().Write("second");</code></example>
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
	/// <example><code>writer.Write("value").EnsureNewLine();</code></example>
	public CodeWriter EnsureNewLine()
	{
		return _atLineStart ? this : NewLine();
	}

	/// <summary>
	/// Ensures that the next content starts after a blank line.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="EnsureNewLine"/>, this method also adds a separator when the writer is
	/// already at the start of a line. Calling it repeatedly does not add additional blank lines.
	/// </remarks>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteMethodCall("Run").EnsureBlankLine().Comment("Explains the next member.");</code></example>
	public CodeWriter EnsureBlankLine()
	{
		if (_builder.Length == 0)
			return this;

		EnsureNewLine();
		if (_builder.Length < 2 || _builder[_builder.Length - 2] != NewLineCharacter)
			NewLine();

		return this;
	}

	/// <summary>
	/// Writes an optional value followed by a line feed, applying the current indentation.
	/// </summary>
	/// <param name="value">The value to write, or <see langword="null"/> to write an empty line.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteLine("return value;");</code></example>
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
	/// Writes a line comment or a multi-line comment block.
	/// </summary>
	/// <param name="comments">The comment lines.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.Comment("Generated member", "Do not edit.");</code></example>
	public CodeWriter Comment(params string[] comments)
	{
		if (comments is null || comments.Length == 0)
			return this;

		if (comments.Length == 1)
			return Write("// ").WriteLine(comments[0]);

		WriteLine("/*");
		for (var index = 0; index < comments.Length; index++)
			Write(" * ").WriteLine(comments[index]);

		return WriteLine(" */");
	}

	/// <summary>
	/// Writes the current indentation without writing content.
	/// </summary>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.Indent().WriteIndent().Write("value");</code></example>
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
	/// <example><code>writer.Write("int value = 1;");</code></example>
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
	/// <example><code>writer.Write('{');</code></example>
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
	/// <example><code>writer.Append("int value = 1;");</code></example>
	public CodeWriter Append(string? value) => Write(value);

	/// <summary>
	/// Writes an optional value followed by a line feed.
	/// </summary>
	/// <param name="value">The value to write, or <see langword="null"/> for an empty line.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.AppendLine("return value;");</code></example>
	public CodeWriter AppendLine(string? value = null) => WriteLine(value);

	/// <summary>
	/// Writes a value when the supplied condition is true.
	/// </summary>
	/// <param name="condition">Whether to write the value.</param>
	/// <param name="value">The value to write.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteIf(includeValue, "value");</code></example>
	public CodeWriter WriteIf(bool condition, string? value) => condition ? Write(value) : this;

	/// <summary>
	/// Writes a line when the supplied condition is true.
	/// </summary>
	/// <param name="condition">Whether to write the line.</param>
	/// <param name="value">The value to write.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteLineIf(includeValue, "value");</code></example>
	public CodeWriter WriteLineIf(bool condition, string? value) => condition ? WriteLine(value) : this;

	/// <summary>
	/// Writes a value surrounded by double quotes.
	/// </summary>
	/// <param name="value">The value to quote.</param>
	/// <returns>The current writer.</returns>
	/// <remarks>This method does not escape characters contained in <paramref name="value"/>.</remarks>
	/// <example><code>writer.Quote("value"); // "value"</code></example>
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
	/// <example><code>writer.QuoteLine("value"); // "value"</code></example>
	public CodeWriter QuoteLine(string? value = null) => Quote(value).NewLine();

	/// <summary>
	/// Opens an indented scope and returns a value that closes it when disposed.
	/// </summary>
	/// <param name="header">Optional content written before the opening brace.</param>
	/// <returns>A scope that restores indentation and writes the closing token.</returns>
	/// <example><code>using (writer.OpenBlockScope("if (enabled)")) writer.WriteLine("Run();");
	/// // if (enabled)
	/// // {
	/// // 	Run();
	/// // }</code></example>
	public BlockScope OpenBlockScope(string? header = null) => OpenDelimitedBlockScope(header, "{", "}");

	/// <summary>Writes a complete block and invokes a callback for its body.</summary>
	/// <example><code>writer.OpenBlock("if (enabled)", body =&gt; body.WriteLine("Run();"));</code></example>
	public CodeWriter OpenBlock(string? header, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (OpenBlockScope(header))
			bodyWriter(this);

		return this;
	}

	/// <summary>
	/// Opens an indented scope using explicit opening and closing tokens.
	/// </summary>
	/// <param name="header">Optional content written before the opening token.</param>
	/// <param name="openingToken">The opening token, or <see langword="null"/> for none.</param>
	/// <param name="closingToken">The closing token, or <see langword="null"/> for none.</param>
	/// <returns>A scope that restores indentation and writes the closing token.</returns>
	/// <example><code>using (writer.OpenDelimitedBlockScope("items", "(", ");")) writer.WriteLine("value");
	/// // items
	/// // (
	/// // 	value
	/// // );</code></example>
	public BlockScope OpenDelimitedBlockScope(string? header, string? openingToken, string? closingToken)
	{
		if (header is not null)
		{
			Write(header);
			EnsureNewLine();
		}

		if (openingToken is not null)
			WriteLine(openingToken);

		Indent();
		return TrackOpenBlockScope(header, closingToken);
	}

	/// <summary>Writes a complete explicitly delimited block and invokes a callback for its body.</summary>
	/// <example><code>writer.OpenDelimitedBlock("items", "(", ");", body =&gt; body.WriteLine("value"));</code></example>
	public CodeWriter OpenDelimitedBlock(
		string? header,
		string? openingToken,
		string? closingToken,
		Action<CodeWriter> bodyWriter
	)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));
		using (OpenDelimitedBlockScope(header, openingToken, closingToken))
			bodyWriter(this);
		return this;
	}

	/// <summary>
	/// Opens an indented scope whose header is completed by a callback before the opening token.
	/// </summary>
	/// <param name="header">Optional content written before the additional header parts.</param>
	/// <param name="writeRemainingHeader">Writes content appended to the header.</param>
	/// <param name="openingToken">The opening token, or <see langword="null"/> for none.</param>
	/// <param name="closingToken">The closing token, or <see langword="null"/> for none.</param>
	/// <returns>A scope that restores indentation and writes the closing token.</returns>
	/// <example><code>using (writer.OpenDelimitedBlockWithHeaderScope("Call", w =&gt; w.Write("(value)"), "{", "}"))
	/// 	writer.WriteLine("Run();");</code></example>
	public BlockScope OpenDelimitedBlockWithHeaderScope(
		string? header,
		Action<CodeWriter> writeRemainingHeader,
		string? openingToken,
		string? closingToken
	)
	{
		if (writeRemainingHeader is null)
			throw new ArgumentNullException(nameof(writeRemainingHeader));

		if (header is not null)
			Write(header);

		writeRemainingHeader(this);
		EnsureNewLine();

		if (openingToken is not null)
			WriteLine(openingToken);

		Indent();
		return TrackOpenBlockScope(header, closingToken);
	}

	/// <summary>Writes a complete delimited block with a callback-completed header and body.</summary>
	/// <example><code>writer.OpenDelimitedBlockWithHeader("Call", w =&gt; w.Write("(value)"), "{", "}", body =&gt; body.WriteLine("Run();"));</code></example>
	public CodeWriter OpenDelimitedBlockWithHeader(
		string? header,
		Action<CodeWriter> writeRemainingHeader,
		string? openingToken,
		string? closingToken,
		Action<CodeWriter> bodyWriter
	)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));
		using (OpenDelimitedBlockWithHeaderScope(header, writeRemainingHeader, openingToken, closingToken))
			bodyWriter(this);
		return this;
	}

	/// <summary>
	/// Writes a complete scoped block by invoking the supplied body.
	/// </summary>
	/// <param name="header">Optional content written before the opening token.</param>
	/// <param name="body">The block body.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteBlock("if (enabled)", body =&gt; body.WriteLine("Run();"));</code></example>
	public CodeWriter WriteBlock(string? header, Action<CodeWriter> body) =>
		WriteDelimitedBlock(header, "{", "}", body);

	/// <summary>
	/// Writes a complete scope using explicit opening and closing tokens.
	/// </summary>
	/// <param name="header">Optional content written before the opening token.</param>
	/// <param name="openingToken">The opening token, or <see langword="null"/> for none.</param>
	/// <param name="closingToken">The closing token, or <see langword="null"/> for none.</param>
	/// <param name="body">The block body.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteDelimitedBlock("items", "(", ");", body =&gt; body.WriteLine("value"));</code></example>
	public CodeWriter WriteDelimitedBlock(
		string? header,
		string? openingToken,
		string? closingToken,
		Action<CodeWriter> body
	)
	{
		if (body is null)
			throw new ArgumentNullException(nameof(body));

		using (OpenDelimitedBlockScope(header, openingToken, closingToken))
			body(this);

		return this;
	}

	/// <summary>Writes a structured method declaration and returns its body scope.</summary>
	/// <param name="declaration">The method declaration.</param>
	/// <returns>
	/// The method body scope, or an empty scope when an abstract or expression-bodied method was
	/// emitted.
	/// </returns>
	/// <example><code>using (writer.WriteMethodScope(new MethodDeclarationOptions("Run"))) writer.WriteLine("return;");</code></example>
	public BlockScope WriteMethodScope(MethodDeclarationOptions declaration) =>
		WriteMethodScope(declaration, expressionWriter: null);

	BlockScope WriteMethodScope(MethodDeclarationOptions declaration, Action<CodeWriter>? expressionWriter)
	{
		if (declaration.ReturnType.IsEmpty)
			return default;

		WriteMethodHeader(declaration);

		if (declaration.IsPartial)
		{
			Write(';').NewLine();

			CompleteWrittenItem(WrittenItemKind.Method, _indentLevel);
			return default;
		}

		if (declaration.ExpressionBody is not null)
		{
			Write(" => ");
			WriteExpression(declaration.ExpressionBody, expressionWriter);
			WriteLine(";");
			CompleteWrittenItem(WrittenItemKind.Method, _indentLevel);
			return default;
		}

		if (declaration.IsAbstract)
		{
			WriteLine(";");
			CompleteWrittenItem(WrittenItemKind.Method, _indentLevel);
			return default;
		}

		NewLine();
		return OpenBlockScope(WrittenItemKind.Method);
	}

	void WriteMethodHeader(MethodDeclarationOptions declaration)
	{
		ValidateMethodDeclaration(declaration);
		BeginWrittenItem(WrittenItemKind.Method);

		if (declaration.IncludeGeneratedAttributes ?? DefaultIncludeGeneratedAttributes)
			WriteGeneratedAttributes(includeCoverageExclusion: true, includeEmbeddedAttribute: false);

		WriteAttributes(declaration.Attributes);
		WriteAttributes(declaration.ReturnAttributes, defaultTarget: "return");

		WriteMemberModifiers(
			declaration.Accessibility,
			declaration.IsStatic,
			declaration.IsAbstract,
			declaration.IsVirtual,
			declaration.IsOverride,
			declaration.IsSealed
		);

		WriteIf(declaration.IsAsync, "async ").WriteIf(declaration.IsUnsafe, "unsafe ");
		WriteIf(declaration.IsPartial, "partial ")
			.WriteTypeReference(declaration.ReturnType)
			.Write(' ')
			.Write(declaration.Name);

		WriteGenericTypeParameters(declaration.GenericTypes);
		WriteParametersWithHeuristic(declaration.Parameters);
		if (HasGenericConstraints(declaration.GenericTypes))
			NewLine();

		WriteMethodGenericConstraints(declaration.GenericTypes);
	}

	/// <summary>Writes a structured partial method declaration.</summary>
	/// <example><code>writer.WritePartialMethod(new MethodDeclarationOptions("OnChanged"));</code></example>
	public CodeWriter WritePartialMethod(MethodDeclarationOptions declaration)
	{
		WriteMethodScope(declaration with { IsPartial = true });
		return this;
	}

	/// <summary>Writes a structured partial method declaration.</summary>
	/// <example><code>writer.WriteMethodExpression(new MethodDeclarationOptions("Count", "int") { ExpressionBody = "items.Count" });</code></example>
	public CodeWriter WriteMethodExpression(MethodDeclarationOptions declaration)
	{
		if (string.IsNullOrWhiteSpace(declaration.ExpressionBody))
		{
			throw new ArgumentException(
				"An expression-bodied method must have a non-empty expression body.",
				nameof(declaration)
			);
		}

		// The method is not abstract, so we can use the WriteMethod overload that takes a body callback.
		return WriteMethod(declaration, _ => { });
	}

	/// <summary>Writes an expression-bodied method using a callback for the expression.</summary>
	/// <example><code>writer.WriteMethodExpression(new MethodDeclarationOptions("Count", "int"), expression =&gt; expression.Write("items.Count"));</code></example>
	public CodeWriter WriteMethodExpression(MethodDeclarationOptions declaration, Action<CodeWriter> writeExpression)
	{
		if (writeExpression is null)
			throw new ArgumentNullException(nameof(writeExpression));
		if (declaration.ExpressionBody is not null)
			throw new ArgumentException(
				"A callback expression cannot be supplied when ExpressionBody is already set.",
				nameof(declaration)
			);

		using (WriteMethodScope(declaration with { ExpressionBody = string.Empty }, writeExpression))
		{
			//
		}

		return this;
	}

	/// <summary>Writes a structured method and invokes a callback for its body.</summary>
	/// <example><code>writer.WriteMethod(new MethodDeclarationOptions("Run"), body =&gt; body.WriteLine("return;"));</code></example>
	public CodeWriter WriteMethod(MethodDeclarationOptions declaration, Action<CodeWriter> writeBody)
	{
		if (writeBody is null)
			throw new ArgumentNullException(nameof(writeBody));

		if (declaration.IsAbstract || declaration.ExpressionBody is not null)
		{
			throw new ArgumentException(
				"A callback body cannot be supplied for an abstract or expression-bodied method.",
				nameof(declaration)
			);
		}

		if (declaration.IsPartial)
		{
			if (declaration.ReturnType.IsEmpty)
				return this;

			WriteMethodHeader(declaration);
			NewLine();
			using (OpenBlockScope(WrittenItemKind.Method))
				writeBody(this);

			return this;
		}

		using (WriteMethodScope(declaration))
			writeBody(this);

		return this;
	}

	/// <summary>Writes an auto-property or expression-bodied property.</summary>
	/// <example><code>writer.WriteProperty(new PropertyDeclarationOptions("Name", "string"));</code></example>
	public CodeWriter WriteProperty(PropertyDeclarationOptions declaration)
	{
		if (declaration.Type.IsEmpty)
			return this;
		ValidatePropertyDeclaration(declaration);
		BeginWrittenItem(WrittenItemKind.Property);
		if (declaration.IncludeGeneratedAttributes ?? DefaultIncludeGeneratedAttributes)
			WriteGeneratedAttributes(includeCoverageExclusion: true, includeEmbeddedAttribute: false);
		WriteAttributes(declaration.Attributes);
		WritePropertyHeader(declaration);
		if (declaration.ExpressionBody is not null)
		{
			Write(" => ");
			WriteExpression(declaration.ExpressionBody, expressionWriter: null);
			WriteLine(";");
			CompleteWrittenItem(WrittenItemKind.Property, _indentLevel);
			return this;
		}

		Write(" { ");
		if (declaration.HasGetter)
			WriteAccessor(declaration.GetterAccessibility, "get;");
		if (declaration.HasSetter || declaration.IsInitOnly)
			WriteAccessor(declaration.SetterAccessibility, declaration.IsInitOnly ? "init;" : "set;");
		Write("}");
		if (declaration.Initializer is not null)
		{
			Write(" = ");
			WriteExpression(declaration.Initializer, expressionWriter: null);
			Write(';');
		}
		NewLine();
		CompleteWrittenItem(WrittenItemKind.Property, _indentLevel);
		return this;
	}

	/// <summary>Writes an expression-bodied property using a callback for the expression.</summary>
	/// <example><code>writer.WritePropertyExpression(new PropertyDeclarationOptions("Count", "int"), expression =&gt; expression.Write("items.Count"));</code></example>
	public CodeWriter WritePropertyExpression(
		PropertyDeclarationOptions declaration,
		Action<CodeWriter> writeExpression
	)
	{
		if (writeExpression is null)
			throw new ArgumentNullException(nameof(writeExpression));
		if (declaration.ExpressionBody is not null)
			throw new ArgumentException(
				"A callback expression cannot be supplied when ExpressionBody is already set.",
				nameof(declaration)
			);
		if (declaration.Initializer is not null)
			throw new ArgumentException(
				"An expression-bodied property cannot specify an initializer.",
				nameof(declaration)
			);

		ValidatePropertyDeclaration(declaration with { ExpressionBody = "callback" });
		BeginWrittenItem(WrittenItemKind.Property);
		if (declaration.IncludeGeneratedAttributes ?? DefaultIncludeGeneratedAttributes)
			WriteGeneratedAttributes(includeCoverageExclusion: true, includeEmbeddedAttribute: false);
		WriteAttributes(declaration.Attributes);
		WritePropertyHeader(declaration).Write(" => ");
		WriteExpression(null, writeExpression);
		WriteLine(";");
		CompleteWrittenItem(WrittenItemKind.Property, _indentLevel);
		return this;
	}

	/// <summary>Writes a property with callback-generated accessor bodies.</summary>
	/// <example><code>writer.WriteProperty(new PropertyDeclarationOptions("Value", "int"), get =&gt; get.WriteLine("return _value;"), null);</code></example>
	public CodeWriter WriteProperty(
		PropertyDeclarationOptions declaration,
		Action<CodeWriter>? writeGetterBody,
		Action<CodeWriter>? writeSetterBody
	)
	{
		ValidatePropertyDeclaration(declaration);
		if (declaration.ExpressionBody is not null || declaration.Initializer is not null)
			throw new ArgumentException(
				"A property with accessor bodies cannot specify an expression body or initializer.",
				nameof(declaration)
			);
		if (declaration.IsAbstract)
			throw new ArgumentException(
				"Accessor bodies cannot be supplied for an abstract property.",
				nameof(declaration)
			);

		BeginWrittenItem(WrittenItemKind.Property);
		if (declaration.IncludeGeneratedAttributes ?? DefaultIncludeGeneratedAttributes)
			WriteGeneratedAttributes(includeCoverageExclusion: true, includeEmbeddedAttribute: false);
		WriteAttributes(declaration.Attributes);
		WritePropertyHeader(declaration).NewLine();
		using (OpenBlockScope())
		{
			if (declaration.HasGetter)
				WriteAccessorBody(declaration.GetterAccessibility, "get", writeGetterBody);
			if (declaration.HasSetter || declaration.IsInitOnly)
				WriteAccessorBody(
					declaration.SetterAccessibility,
					declaration.IsInitOnly ? "init" : "set",
					writeSetterBody
				);
		}
		CompleteWrittenItem(WrittenItemKind.Property, _indentLevel);
		return this;
	}

	/// <summary>Writes a field declaration.</summary>
	/// <example><code>writer.WriteField(new FieldDeclarationOptions("_value", "int"));</code></example>
	public CodeWriter WriteField(FieldDeclarationOptions declaration)
	{
		if (declaration.Type.IsEmpty)
			return this;
		ValidateFieldDeclaration(declaration);
		BeginWrittenItem(WrittenItemKind.Field);
		if (declaration.IncludeGeneratedAttributes ?? DefaultIncludeGeneratedAttributes)
			WriteGeneratedAttributes(includeCoverageExclusion: false, includeEmbeddedAttribute: false);
		WriteAttributes(declaration.Attributes);
		if (declaration.Accessibility is { } accessibility)
			WriteAccessibility(accessibility).Write(' ');
		WriteIf(declaration.IsConst, "const ")
			.WriteIf(declaration.IsStatic && !declaration.IsConst, "static ")
			.WriteIf(declaration.IsReadOnly, "readonly ")
			.WriteIf(declaration.IsVolatile, "volatile ")
			.WriteTypeReference(declaration.Type)
			.Write(' ')
			.Write(declaration.Name);
		if (declaration.Initializer is not null)
		{
			Write(" = ");
			WriteExpression(declaration.Initializer, expressionWriter: null);
		}
		WriteLine(";");
		CompleteWrittenItem(WrittenItemKind.Field, _indentLevel);
		return this;
	}

	/// <summary>
	/// Writes a C# using directive.
	/// </summary>
	/// <param name="namespaceName">The namespace to import.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteUsing("System"); // using System;</code></example>
	public CodeWriter WriteUsing(string namespaceName)
	{
		return string.IsNullOrWhiteSpace(namespaceName)
			? throw new ArgumentException("Namespace cannot be null or whitespace.", nameof(namespaceName))
			: Write("using ").Write(namespaceName).WriteLine(";");
	}

	/// <summary>
	/// Writes a block-scoped namespace and returns its body scope.
	/// </summary>
	/// <param name="namespaceName">The namespace, or <see langword="null"/> to write nothing.</param>
	/// <returns>The namespace body scope, or an empty scope when no namespace is supplied.</returns>
	/// <example><code>using (writer.WriteBlockNamespaceScope("Example")) writer.WriteLine("class C { }");</code></example>
	public BlockScope WriteBlockNamespaceScope(string? namespaceName)
	{
		if (string.IsNullOrWhiteSpace(namespaceName))
			return default;

		BeginWrittenItem(WrittenItemKind.Namespace);
		Write("namespace ").WriteLine(namespaceName);
		return OpenBlockScope(WrittenItemKind.Namespace);
	}

	/// <summary>
	/// Writes a block-scoped namespace and invokes a callback for its body.
	/// </summary>
	/// <param name="typeReference">The type reference whose namespace will be used, or a value with no namespace to omit the wrapper.</param>
	/// <param name="bodyWriter">The action that writes the namespace body.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteBlockNamespace(new TypeValueObject("C", "Example").AsTypeReference(), body =&gt; body.WriteLine("class C { }"));</code></example>
	public CodeWriter WriteBlockNamespace(TypeReferenceOptions typeReference, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteBlockNamespaceScope(typeReference))
			bodyWriter(this);

		return this;
	}

	/// <summary>
	/// Writes a block-scoped namespace and returns its body scope.
	/// </summary>
	/// <param name="typeReference">The type reference whose namespace will be used, or a value with no namespace to return an empty scope.</param>
	/// <returns>The namespace body scope, or an empty scope when no namespace is supplied.</returns>
	/// <example><code>using (writer.WriteBlockNamespaceScope(new TypeValueObject("C", "Example").AsTypeReference())) writer.WriteLine("class C { }");</code></example>
	public IDisposable WriteBlockNamespaceScope(TypeReferenceOptions? typeReference) =>
		typeReference is null ? NoOpScope.Instance : WriteBlockNamespaceScope(typeReference.Type.Namespace);

	/// <summary>Writes a block-scoped namespace and invokes a callback for its body.</summary>
	/// <param name="namespaceName">The namespace, or <see langword="null"/> to omit the wrapper.</param>
	/// <param name="bodyWriter">The action that writes the namespace body.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteBlockNamespace("Example", body =&gt; body.WriteLine("class C { }"));</code></example>
	public CodeWriter WriteBlockNamespace(string? namespaceName, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));
		using (WriteBlockNamespaceScope(namespaceName))
			bodyWriter(this);
		return this;
	}

	/// <summary>
	/// Writes a file-scoped namespace followed by an empty line.
	/// </summary>
	/// <param name="namespaceName">The namespace, or <see langword="null"/> to write nothing.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteFileScopedNamespace("Example"); // namespace Example;</code></example>
	public CodeWriter WriteFileScopedNamespace(string? namespaceName)
	{
		return string.IsNullOrWhiteSpace(namespaceName)
			? this
			: Write("namespace ").Write(namespaceName).WriteLine(";").NewLine();
	}

	/// <summary>
	/// Writes a file-scoped namespace followed by an empty line.
	/// </summary>
	/// <param name="typeReference">The type reference whose namespace will be used, or a value with no namespace to write nothing.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteFileScopedNamespace(new TypeValueObject("C", "Example").AsTypeReference());</code></example>
	public CodeWriter WriteFileScopedNamespace(TypeReferenceOptions? typeReference) =>
		typeReference is null ? this : WriteFileScopedNamespace(typeReference.Type.Namespace);

	/// <summary>
	/// Writes a class declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The class declaration options.</param>
	/// <returns>The class body scope.</returns>
	/// <example><code>using (writer.WriteClassScope(new TypeDeclarationOptions("C"))) writer.WriteLine("// body");</code></example>
	public BlockScope WriteClassScope(TypeDeclarationOptions declaration)
	{
		return declaration is null
			? throw new ArgumentNullException(nameof(declaration))
			: WriteTypeScope(declaration with { Kind = TypeDeclarationKind.Class });
	}

	/// <summary>
	/// Writes a class declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The class declaration options.</param>
	/// <param name="bodyWriter">The action that writes the body of the class.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteClass(new TypeDeclarationOptions("C"), body =&gt; body.WriteLine("// body"));</code></example>
	public CodeWriter WriteClass(TypeDeclarationOptions declaration, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteClassScope(declaration))
			bodyWriter(this);

		return this;
	}

	/// <summary>
	/// Writes an attribute class with an <see cref="AttributeUsageAttribute"/> declaration.
	/// </summary>
	/// <param name="declaration">
	/// The class declaration options. When no base type is specified, <see cref="Attribute"/> is
	/// used. Set <see cref="TypeDeclarationOptions.BaseType"/> to derive from a custom attribute
	/// base class.
	/// </param>
	/// <param name="targets">The declarations on which the generated attribute may be applied.</param>
	/// <param name="bodyWriter">The action that writes the body of the attribute class.</param>
	/// <param name="inherited">Whether derived classes and overriding members inherit the attribute.</param>
	/// <param name="allowMultiple">Whether more than one instance may be specified on one declaration.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteAttributeClass(new TypeDeclarationOptions("MarkerAttribute"), AttributeTargets.Class, _ =&gt; { });</code></example>
	public CodeWriter WriteAttributeClass(
		TypeDeclarationOptions declaration,
		AttributeTargets targets,
		Action<CodeWriter> bodyWriter,
		bool inherited = false,
		bool allowMultiple = false
	)
	{
		if (declaration is null)
			throw new ArgumentNullException(nameof(declaration));
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));
		if (targets == 0 || (targets & ~AttributeTargets.All) != 0)
			throw new ArgumentOutOfRangeException(nameof(targets), targets, "Invalid attribute targets.");

		AttributeDeclarationOptions attributeUsage = new(new TypeValueObject("AttributeUsageAttribute", "System"))
		{
			Arguments =
			[
				new(RenderAttributeTargets(targets)),
				new(inherited, "Inherited", isPropertyAssignment: true),
				new(allowMultiple, "AllowMultiple", isPropertyAssignment: true),
			],
		};

		return WriteClass(
			declaration with
			{
				BaseType = declaration.BaseType ?? new TypeValueObject("Attribute", "System"),
				Attributes = declaration.Attributes.Insert(0, attributeUsage),
				IncludeEmbeddedAttribute = declaration.IncludeEmbeddedAttribute ?? true,
			},
			bodyWriter
		);
	}

	static string RenderAttributeTargets(AttributeTargets targets)
	{
		if (targets == AttributeTargets.All)
			return "global::System.AttributeTargets.All";

		var names = targets.ToString().Split(',');
		var builder = new StringBuilder();
		for (var index = 0; index < names.Length; index++)
		{
			if (index != 0)
				builder.Append(" | ");
			builder.Append("global::System.AttributeTargets.").Append(names[index].Trim());
		}
		return builder.ToString();
	}

	/// <summary>
	/// Writes a struct declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The struct declaration options.</param>
	/// <returns>The struct body scope.</returns>
	/// <example><code>using (writer.WriteStructScope(new TypeDeclarationOptions("Value"))) { }</code></example>
	public BlockScope WriteStructScope(TypeDeclarationOptions declaration)
	{
		return declaration is null
			? throw new ArgumentNullException(nameof(declaration))
			: WriteTypeScope(declaration with { Kind = TypeDeclarationKind.Struct });
	}

	/// <summary>
	/// Writes a struct declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The struct declaration options.</param>
	/// <param name="bodyWriter">The action that writes the body of the struct.</param>
	/// <returns>The struct body scope.</returns>
	/// <example><code>writer.WriteStruct(new TypeDeclarationOptions("Value"), _ =&gt; { });</code></example>
	public CodeWriter WriteStruct(TypeDeclarationOptions declaration, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteStructScope(declaration))
			bodyWriter(this);

		return this;
	}

	/// <summary>
	/// Writes a record class declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The record class declaration options.</param>
	/// <returns>The record body scope.</returns>
	/// <example><code>using (writer.WriteRecordClassScope(new TypeDeclarationOptions("Model"))) { }</code></example>
	public BlockScope WriteRecordClassScope(TypeDeclarationOptions declaration)
	{
		return declaration is null
			? throw new ArgumentNullException(nameof(declaration))
			: WriteTypeScope(declaration with { Kind = TypeDeclarationKind.RecordClass });
	}

	/// <summary>
	/// Writes a record class declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The record class declaration options.</param>
	/// <param name="bodyWriter">The action that writes the body of the record class.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteRecordClass(new TypeDeclarationOptions("Model"), _ =&gt; { });</code></example>
	public CodeWriter WriteRecordClass(TypeDeclarationOptions declaration, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteRecordClassScope(declaration))
			bodyWriter(this);

		return this;
	}

	/// <summary>
	/// Writes a record struct declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The record struct declaration options.</param>
	/// <returns>The record body scope.</returns>
	/// <example><code>using (writer.WriteRecordStructScope(new TypeDeclarationOptions("Value"))) { }</code></example>
	public BlockScope WriteRecordStructScope(TypeDeclarationOptions declaration)
	{
		return declaration is null
			? throw new ArgumentNullException(nameof(declaration))
			: WriteTypeScope(declaration with { Kind = TypeDeclarationKind.RecordStruct });
	}

	/// <summary>
	/// Writes a record struct declaration from structured options and returns its body scope.
	/// </summary>
	/// <param name="declaration">The record struct declaration options.</param>
	/// <param name="bodyWriter">The action that writes the body of the record struct.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteRecordStruct(new TypeDeclarationOptions("Value"), _ =&gt; { });</code></example>
	public CodeWriter WriteRecordStruct(TypeDeclarationOptions declaration, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteRecordStructScope(declaration))
			bodyWriter(this);

		return this;
	}

	/// <summary>Writes an interface declaration and returns its body scope.</summary>
	/// <example><code>using (writer.WriteInterfaceScope(new TypeDeclarationOptions("IService"))) { }</code></example>
	public BlockScope WriteInterfaceScope(TypeDeclarationOptions declaration) =>
		declaration is null
			? throw new ArgumentNullException(nameof(declaration))
			: WriteTypeScope(declaration with { Kind = TypeDeclarationKind.Interface });

	/// <summary>Writes an interface declaration and invokes a callback for its body.</summary>
	/// <example><code>writer.WriteInterface(new TypeDeclarationOptions("IService"), _ =&gt; { });</code></example>
	public CodeWriter WriteInterface(TypeDeclarationOptions declaration, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));
		using (WriteInterfaceScope(declaration))
			bodyWriter(this);
		return this;
	}

	/// <summary>Writes an enum declaration and returns its body scope.</summary>
	/// <example><code>using (writer.WriteEnumScope(new TypeDeclarationOptions("Status"))) { }</code></example>
	public BlockScope WriteEnumScope(TypeDeclarationOptions declaration) =>
		declaration is null
			? throw new ArgumentNullException(nameof(declaration))
			: WriteTypeScope(declaration with { Kind = TypeDeclarationKind.Enum });

	/// <summary>Writes an enum declaration and invokes a callback for its body.</summary>
	/// <example><code>writer.WriteEnum(new TypeDeclarationOptions("Status"), _ =&gt; { });</code></example>
	public CodeWriter WriteEnum(TypeDeclarationOptions declaration, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteEnumScope(declaration))
			bodyWriter(this);

		return this;
	}

	/// <summary>Writes an enum declaration with structured field declarations.</summary>
	/// <param name="declaration">The enum declaration options.</param>
	/// <param name="fields">The fields to write in declaration order.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteEnum(new TypeDeclarationOptions("Status"), new EnumFieldDeclarationOptions("Ready", 1));</code></example>
	public CodeWriter WriteEnum(TypeDeclarationOptions declaration, params EnumFieldDeclarationOptions[] fields)
	{
		if (fields is null)
			throw new ArgumentNullException(nameof(fields));

		// Validate each field before writing the enum to avoid partial output on error.
		for (var index = 0; index < fields.Length; index++)
			ValidateEnumFieldDeclaration(fields[index]);

		return WriteEnum(
			declaration,
			body =>
			{
				for (var index = 0; index < fields.Length; index++)
					body.WriteEnumField(fields[index]);
			}
		);
	}

	/// <summary>Writes a field in an enum declaration.</summary>
	/// <param name="declaration">The enum field declaration options.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteEnumField(new EnumFieldDeclarationOptions("Ready", 1));</code></example>
	public CodeWriter WriteEnumField(EnumFieldDeclarationOptions declaration)
	{
		ValidateEnumFieldDeclaration(declaration);
		if (!declaration.XmlSummary.IsDefaultOrEmpty)
			WriteXmlSummary(declaration.XmlSummary);
		WriteAttributes(declaration.Attributes);
		Write(declaration.FieldName);
		if (declaration.FieldValue is not null)
		{
			Write(" = ");
			Write(
				declaration.FieldValue as string
					?? Convert.ToString(declaration.FieldValue, CultureInfo.InvariantCulture)
			);
		}
		return WriteLine(",");
	}

	void WriteXmlSummary(ImmutableArray<string> summary)
	{
		if (summary.Length == 1)
		{
			Write("/// <summary>").Write(summary[0]).WriteLine("</summary>");
			return;
		}

		WriteLine("/// <summary>");
		for (var index = 0; index < summary.Length; index++)
			Write("/// ").WriteLine(summary[index]);
		WriteLine("/// </summary>");
	}

	/// <summary>Writes a complete delegate declaration.</summary>
	/// <example><code>writer.WriteDelegate(new TypeDeclarationOptions("Handler") { DelegateReturnType = "void" });</code></example>
	public CodeWriter WriteDelegate(TypeDeclarationOptions declaration)
	{
		if (declaration is null)
			throw new ArgumentNullException(nameof(declaration));
		WriteTypeScope(declaration with { Kind = TypeDeclarationKind.Delegate });
		return this;
	}

	/// <summary>
	/// Writes a structured type declaration and returns its body scope when the declaration has one.
	/// </summary>
	/// <param name="declaration">The structured type declaration options.</param>
	/// <returns>The generated type body scope.</returns>
	/// <example><code>using (writer.WriteTypeScope(new TypeDeclarationOptions("C"))) { }</code></example>
	public BlockScope WriteTypeScope(TypeDeclarationOptions declaration)
	{
		if (declaration is null)
			throw new ArgumentNullException(nameof(declaration));

		ValidateTypeDeclaration(declaration);
		BeginWrittenItem(WrittenItemKind.Type);

		if (declaration.IncludeGeneratedAttributes ?? DefaultIncludeGeneratedAttributes)
		{
			WriteGeneratedAttributes(
				includeCoverageExclusion: declaration.Kind
					is TypeDeclarationKind.Class
						or TypeDeclarationKind.Struct
						or TypeDeclarationKind.RecordClass
						or TypeDeclarationKind.RecordStruct,
				includeEmbeddedAttribute: declaration.IncludeEmbeddedAttribute == true
			);
		}

		WriteAttributes(declaration.Attributes);

		if (declaration.Accessibility is { } accessibility)
			WriteAccessibility(accessibility).Write(' ');

		var isStruct = declaration.Kind is TypeDeclarationKind.Struct or TypeDeclarationKind.RecordStruct;
		var isClass = declaration.Kind is TypeDeclarationKind.Class or TypeDeclarationKind.RecordClass;

		if (declaration.IsStatic)
			Write("static ");
		else if (isStruct && declaration.IsReadOnly)
			Write("readonly ");
		else if (isClass && declaration.IsAbstract)
			Write("abstract ");
		else if (isClass && declaration.IsSealed)
			Write("sealed ");

		if (
			declaration.IsPartial
			&& declaration.Kind is not TypeDeclarationKind.Enum and not TypeDeclarationKind.Delegate
		)
			Write("partial ");

		if (declaration.Kind == TypeDeclarationKind.Delegate)
			Write("delegate ").WriteTypeReference(declaration.DelegateReturnType!).Write(' ');

		Write(
				declaration.Kind switch
				{
					TypeDeclarationKind.Class => "class ",
					TypeDeclarationKind.Struct => "struct ",
					TypeDeclarationKind.RecordClass => "record class ",
					TypeDeclarationKind.RecordStruct => "record struct ",
					TypeDeclarationKind.Interface => "interface ",
					TypeDeclarationKind.Enum => "enum ",
					TypeDeclarationKind.Delegate => string.Empty,
					_ => throw new ArgumentOutOfRangeException(nameof(declaration)),
				}
			)
			.Write(declaration.Name);

		WriteGenericTypeParameters(declaration.GenericTypes);
		if (declaration.Kind == TypeDeclarationKind.Delegate)
			WriteParametersWithHeuristic(declaration.DelegateParameters);
		else
			WriteParameterList(
				declaration.PrimaryConstructorParameters,
				declaration.ConstructorParametersOnSeparateLines
			);
		WriteBaseTypes(declaration);
		if (declaration.Kind == TypeDeclarationKind.Enum && declaration.EnumUnderlyingType is { IsEmpty: false })
			Write(" : ").WriteTypeReference(declaration.EnumUnderlyingType!);

		if (declaration.Kind == TypeDeclarationKind.Delegate)
		{
			if (HasGenericConstraints(declaration.GenericTypes))
				NewLine();
			WriteMethodGenericConstraints(declaration.GenericTypes);
			WriteLine(";");
			CompleteWrittenItem(WrittenItemKind.Type, _indentLevel);
			return default;
		}

		NewLine();
		WriteGenericConstraints(declaration.GenericTypes);

		return OpenBlockScope(WrittenItemKind.Type);
	}

	/// <summary>Writes a structured type declaration and invokes a callback for its body.</summary>
	/// <param name="declaration">The structured type declaration options.</param>
	/// <param name="bodyWriter">The action that writes the type body.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteType(new TypeDeclarationOptions("C"), _ =&gt; { });</code></example>
	public CodeWriter WriteType(TypeDeclarationOptions declaration, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));
		using (WriteTypeScope(declaration))
			bodyWriter(this);
		return this;
	}

	/// <summary>
	/// Writes an ordinary instance or static constructor and returns its body scope.
	/// </summary>
	/// <param name="declaration">The constructor declaration options.</param>
	/// <returns>The constructor body scope.</returns>
	/// <example><code>using (writer.WriteConstructorScope(new ConstructorDeclarationOptions("C"))) writer.WriteLine("// body");</code></example>
	public BlockScope WriteConstructorScope(ConstructorDeclarationOptions declaration)
	{
		ValidateConstructorDeclaration(declaration);
		BeginWrittenItem(WrittenItemKind.Constructor);
		if (declaration.IncludeGeneratedAttributes ?? DefaultIncludeGeneratedAttributes)
			WriteGeneratedAttributes(includeCoverageExclusion: true, includeEmbeddedAttribute: false);
		WriteAttributes(declaration.Attributes);

		if (declaration.IsStatic)
			Write("static ");
		else if (declaration.Accessibility is { } accessibility)
			WriteAccessibility(accessibility).Write(' ');

		Write(declaration.Reference.Type.Name);
		if (declaration.WriteParametersOnSeparateLines)
			WriteParameterList(declaration.Parameters, writeOnSeparateLines: true, writeWhenEmpty: true);
		else
			WriteParametersWithHeuristic(declaration.Parameters);

		if (!string.IsNullOrWhiteSpace(declaration.Initializer))
		{
			EnsureNewLine();
			Indent();
			Write(": ").WriteLine(declaration.Initializer);
			Unindent();
		}

		EnsureNewLine();
		return OpenBlockScope(WrittenItemKind.Constructor);
	}

	/// <summary>Writes a structured constructor and invokes a callback for its body.</summary>
	/// <example><code>writer.WriteConstructor(new ConstructorDeclarationOptions("C"), _ =&gt; { });</code></example>
	public CodeWriter WriteConstructor(ConstructorDeclarationOptions declaration, Action<CodeWriter> writeBody)
	{
		if (writeBody is null)
			throw new ArgumentNullException(nameof(writeBody));
		using (WriteConstructorScope(declaration))
			writeBody(this);
		return this;
	}

	/// <summary>
	/// Writes the standard header for an automatically generated source file.
	/// </summary>
	/// <param name="generatorName">The generator name; defaults to <see cref="GeneratorName"/>.</param>
	/// <param name="version">The generator version; defaults to <see cref="GeneratorVersion"/>.</param>
	/// <param name="pragmas">The pragmas to include in the header.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteAutoGeneratedHeader(pragmas: ["CS0618"]);</code></example>
	public CodeWriter WriteAutoGeneratedHeader(
		string? generatorName = null,
		string? version = null,
		params string[] pragmas
	)
	{
		generatorName ??= GeneratorName;
		version ??= GeneratorVersion;
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
			{
				Write("#pragma warning disable ").WriteLine(pragma);
			}
		}

		return NewLine();
	}

	/// <summary>
	/// Writes a <see cref="System.CodeDom.Compiler.GeneratedCodeAttribute"/> declaration.
	/// </summary>
	/// <param name="generatorName">The generator name.</param>
	/// <param name="version">The generator version, defaulting to <c>1.0.0.0</c>.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteGeneratedCodeAttribute("MyGenerator", "1.0.0");</code></example>
	public CodeWriter WriteGeneratedCodeAttribute(string generatorName, string? version = null)
	{
		if (string.IsNullOrWhiteSpace(generatorName))
		{
			throw new ArgumentException("Generator name cannot be null or whitespace.", nameof(generatorName));
		}

		// The GeneratedCodeAttribute constructor requires a non-null version, so we default to "
		return Write("[global::System.CodeDom.Compiler.GeneratedCode(\"")
			.Write(generatorName)
			.Write("\", \"")
			.Write(version ?? "1.0.0.0")
			.WriteLine("\")]");
	}

	/// <summary>Writes the standard marker attributes for a generated declaration.</summary>
	/// <param name="includeCoverageExclusion">
	/// Whether to emit <see cref="ExcludeFromCodeCoverageAttribute"/>.
	/// This must be enabled only for declaration targets supported by that attribute.
	/// </param>
	/// <param name="includeEmbeddedAttribute">
	/// Whether to emit <see cref="Microsoft.CodeAnalysis.EmbeddedAttribute"/>.
	/// This is intended only for generator-emitted marker attribute types (R11) and must be
	/// <see langword="false"/> for ordinary generated members.
	/// </param>
	/// <param name="includeGeneratedCodeAttribute">Whether to emit <see cref="System.CodeDom.Compiler.GeneratedCodeAttribute"/> and <see cref="System.Runtime.CompilerServices.CompilerGeneratedAttribute"/>.</param>
	/// <example><code>writer.WriteGeneratedAttributes(includeCoverageExclusion: true);</code></example>
	public CodeWriter WriteGeneratedAttributes(
		bool includeCoverageExclusion = false,
		bool includeEmbeddedAttribute = false,
		bool includeGeneratedCodeAttribute = true
	)
	{
		if (includeEmbeddedAttribute)
			WriteLine("[global::Microsoft.CodeAnalysis.Embedded]");

		if (includeCoverageExclusion)
			WriteLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");

		if (includeGeneratedCodeAttribute)
		{
			WriteLine("[global::System.Runtime.CompilerServices.CompilerGenerated]");
			WriteGeneratedCodeAttribute(GeneratorName, GeneratorVersion);
		}

		return this;
	}

	/// <summary>
	/// Disables the specified warning pragmas and returns a scope that restores them on dispose.
	/// </summary>
	/// <param name="pragmas">The warning codes to disable.</param>
	/// <returns>A scope that writes the corresponding restore pragmas once.</returns>
	/// <example><code>using (writer.OpenPragmasScope("CS0618")) writer.WriteLine("ObsoleteCall();");</code></example>
	public PragmaScope OpenPragmasScope(params string[] pragmas)
	{
		if (pragmas is null || pragmas.Length == 0)
			return new PragmaScope(this, []);

		NewLine();
		foreach (var pragma in pragmas)
			Write("#pragma warning disable ").WriteLine(pragma);

		return new PragmaScope(this, pragmas);
	}

	/// <summary>
	/// Writes each supplied part on its own line.
	/// </summary>
	/// <param name="parts">The lines to write.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.MultiLine("first", "second");</code></example>
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
	/// <example><code>writer.Write("Call(").MultiLineParameters("string name", "int count");</code></example>
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

	/// <summary>Writes a method invocation statement.</summary>
	/// <param name="methodName">The method name, optionally including a receiver.</param>
	/// <param name="arguments">The argument expressions.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteMethodCall("Run", "value", "cancellationToken"); // Run(value, cancellationToken);</code></example>
	public CodeWriter WriteMethodCall(string methodName, params string[] arguments) =>
		WriteMethodCallCore(methodName, arguments, receiver: null, genericArguments: null, false, false);

	/// <summary>Writes an awaited method invocation statement.</summary>
	/// <param name="methodName">The method name, optionally including a receiver.</param>
	/// <param name="arguments">The argument expressions.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteAwaitedMethodCall("LoadAsync", "cancellationToken"); // await LoadAsync(cancellationToken);</code></example>
	public CodeWriter WriteAwaitedMethodCall(string methodName, params string[] arguments) =>
		WriteMethodCallCore(methodName, arguments, receiver: null, genericArguments: null, false, true);

	/// <summary>
	/// Writes a method invocation from structured argument declarations.
	/// </summary>
	/// <remarks>
	/// The argument type is not emitted; each declaration contributes its name and argument
	/// modifier. This allows call sites to reuse parameter declarations while preserving
	/// <c>ref</c>, <c>out</c>, and <c>in</c> arguments.
	/// </remarks>
	/// <param name="methodName">The method name, optionally including a receiver.</param>
	/// <param name="arguments">The structured arguments to invoke the method with.</param>
	/// <param name="receiver">An optional receiver such as <c>service</c>.</param>
	/// <param name="genericArguments">Optional generic type arguments.</param>
	/// <param name="writeArgumentsOnSeparateLines">Whether to force one argument per line.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteMethodCall("Copy", [
	/// 	new("source"),
	/// 	new("destination") { Modifier = ParameterModifier.Out }]);</code></example>
	public CodeWriter WriteMethodCall(
		string methodName,
		IEnumerable<MethodCallArgumentOptions> arguments,
		string? receiver = null,
		IEnumerable<TypeReferenceOptions>? genericArguments = null,
		bool writeArgumentsOnSeparateLines = false
	) =>
		WriteMethodCall(
			methodName,
			(arguments ?? throw new ArgumentNullException(nameof(arguments))).Select(RenderCallArgument),
			receiver,
			genericArguments,
			writeArgumentsOnSeparateLines
		);

	/// <summary>Writes an awaited method invocation from structured argument declarations.</summary>
	/// <param name="methodName">The method name without a receiver or generic argument list.</param>
	/// <param name="arguments">The structured arguments to invoke the method with.</param>
	/// <param name="receiver">An optional receiver such as <c>service</c>.</param>
	/// <param name="genericArguments">Optional generic type arguments.</param>
	/// <param name="writeArgumentsOnSeparateLines">Whether to force one argument per line.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteAwaitedMethodCall("LoadAsync", [new("token")], "service");</code></example>
	public CodeWriter WriteAwaitedMethodCall(
		string methodName,
		IEnumerable<MethodCallArgumentOptions> arguments,
		string? receiver = null,
		IEnumerable<TypeReferenceOptions>? genericArguments = null,
		bool writeArgumentsOnSeparateLines = false
	) =>
		WriteMethodCallCore(
			methodName,
			(arguments ?? throw new ArgumentNullException(nameof(arguments))).Select(RenderCallArgument),
			receiver,
			genericArguments,
			writeArgumentsOnSeparateLines,
			true
		);

	/// <summary>
	/// Writes a method invocation statement with optional receiver and generic type arguments.
	/// </summary>
	/// <param name="methodName">The method name without a receiver or generic argument list.</param>
	/// <param name="arguments">The argument expressions.</param>
	/// <param name="receiver">An optional receiver such as <c>service</c> or <c>global::Api.Service</c>.</param>
	/// <param name="genericArguments">Optional generic type arguments.</param>
	/// <param name="writeArgumentsOnSeparateLines">Whether to force one argument per line.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.WriteMethodCall("Create", ["value"], "factory", [TypeLibrary.System.String.AsTypeReference()]);</code></example>
	public CodeWriter WriteMethodCall(
		string methodName,
		IEnumerable<string?> arguments,
		string? receiver = null,
		IEnumerable<TypeReferenceOptions>? genericArguments = null,
		bool writeArgumentsOnSeparateLines = false
	) => WriteMethodCallCore(methodName, arguments, receiver, genericArguments, writeArgumentsOnSeparateLines, false);

	CodeWriter WriteMethodCallCore(
		string methodName,
		IEnumerable<string?> arguments,
		string? receiver,
		IEnumerable<TypeReferenceOptions>? genericArguments,
		bool writeArgumentsOnSeparateLines,
		bool isAwaited
	)
	{
		ValidateStatementPart(methodName, nameof(methodName));
		if (arguments is null)
			throw new ArgumentNullException(nameof(arguments));
		if (receiver is not null && string.IsNullOrWhiteSpace(receiver))
			throw new ArgumentException("Receiver cannot be whitespace.", nameof(receiver));

		var argumentList = arguments.ToArray();
		var genericArgumentList = genericArguments?.ToArray() ?? [];
		for (var index = 0; index < genericArgumentList.Length; index++)
			if (genericArgumentList[index].IsEmpty)
				throw new ArgumentException("Generic arguments cannot be empty.", nameof(genericArguments));

		if (isAwaited)
			Write("await ");
		if (receiver is not null)
			Write(receiver).Write('.');
		Write(methodName);
		if (genericArgumentList.Length > 0)
		{
			Write('<');
			for (var index = 0; index < genericArgumentList.Length; index++)
			{
				if (index != 0)
					Write(", ");
				WriteTypeReference(genericArgumentList[index]);
			}
			Write('>');
		}

		Write('(');
		if (WriteMethodCallArguments(argumentList, writeArgumentsOnSeparateLines))
			return this;

		// If the arguments were written inline, we can write the closing parenthesis and semicolon on the same line.
		return WriteLine(";");
	}

	bool WriteMethodCallArguments(string?[] arguments, bool writeOnSeparateLines, string multilineClosingToken = ");")
	{
		var inlineLength = CurrentLineLength + 2;
		for (var index = 0; index < arguments.Length; index++)
			inlineLength += (arguments[index]?.Length ?? 0) + (index == 0 ? 0 : 2);

		var canWriteInline =
			!writeOnSeparateLines
			&& inlineLength <= DefaultMaximumLineLength
			&& arguments.All(static argument => argument is not null && !argument.Contains('\n'));
		if (arguments.Length == 0)
		{
			Write(')');
			return false;
		}

		if (canWriteInline)
		{
			for (var index = 0; index < arguments.Length; index++)
			{
				if (index != 0)
					Write(", ");
				Write(arguments[index]);
			}
			Write(')');
			return false;
		}

		NewLine().Indent();
		for (var index = 0; index < arguments.Length; index++)
		{
			WriteExpression(arguments[index], expressionWriter: null);
			WriteLine(index == arguments.Length - 1 ? multilineClosingToken : ",");
		}
		Unindent();
		return true;
	}

	/// <summary>Writes an assignment statement.</summary>
	/// <param name="target">The target, such as <c>value</c> or <c>var result</c>.</param>
	/// <param name="value">The assigned expression.</param>
	/// <param name="forceNotNull">Whether to force the value to be not null, by appending the null-forgiving operator (<c>!</c>).</param>
	/// <example><code>writer.WriteAssignment("value", "CreateValue()"); // value = CreateValue();</code></example>
	public CodeWriter WriteAssignment(string target, string value, bool forceNotNull = false)
	{
		ValidateStatementPart(target, nameof(target));
		ValidateStatementPart(value, nameof(value));
		Write(target).Write(" = ");
		WriteExpression(value, expressionWriter: null);
		if (forceNotNull)
			Write("!");

		return WriteLine(";");
	}

	/// <summary>Writes an assignment statement using a callback for a multiline expression.</summary>
	/// <example><code>writer.WriteAssignment("value", expression =&gt; expression.Write("new Value()"));</code></example>
	public CodeWriter WriteAssignment(string target, Action<CodeWriter> writeValue)
	{
		ValidateStatementPart(target, nameof(target));
		if (writeValue is null)
			throw new ArgumentNullException(nameof(writeValue));
		Write(target).Write(" = ");
		WriteExpression(null, writeValue);
		return WriteLine(";");
	}

	/// <summary>Writes an assignment whose value is a structured object-creation expression.</summary>
	/// <example><code>writer.WriteAssignment("@event", new ObjectCreationOptions(eventType, "propVal1", "propVal2"));</code></example>
	public CodeWriter WriteAssignment(string target, ObjectCreationOptions value, bool forceNotNull = false)
	{
		ValidateStatementPart(target, nameof(target));
		Write(target).Write(" = ");

		if (WriteObjectCreationExpression(value, forceNotNull))
			return this;

		// If the object creation was written inline, we can write the closing semicolon on the same line.
		return WriteLine(";");
	}

	/// <summary>Writes a typed local or declaration assignment.</summary>
	/// <example><code>writer.WriteAssignment("var", "value", "CreateValue()");</code></example>
	public CodeWriter WriteAssignment(string type, string name, string value, bool forceNotNull = false)
	{
		ValidateStatementPart(type, nameof(type));
		ValidateStatementPart(name, nameof(name));
		return WriteAssignment($"{type} {name}", value, forceNotNull);
	}

	/// <summary>Writes a typed local or declaration assignment with a multiline expression.</summary>
	/// <example><code>writer.WriteAssignment("Value", "value", expression =&gt; expression.Write("CreateValue()"));</code></example>
	public CodeWriter WriteAssignment(string type, string name, Action<CodeWriter> writeValue)
	{
		ValidateStatementPart(type, nameof(type));
		ValidateStatementPart(name, nameof(name));
		return WriteAssignment($"{type} {name}", writeValue);
	}

	/// <summary>Writes a typed local assignment whose value is a structured object creation.</summary>
	/// <example><code>writer.WriteAssignment("var", "@event", new ObjectCreationOptions(eventType, "propVal1", "propVal2"));</code></example>
	public CodeWriter WriteAssignment(string type, string name, ObjectCreationOptions value, bool forceNotNull = false)
	{
		ValidateStatementPart(type, nameof(type));
		ValidateStatementPart(name, nameof(name));
		return WriteAssignment($"{type} {name}", value, forceNotNull);
	}

	bool WriteObjectCreationExpression(ObjectCreationOptions value, bool forceNotNull)
	{
		Write("new ").WriteTypeReference(value.Reference).Write('(');
		string[] arguments = value.Arguments.IsDefault ? [] : [.. value.Arguments.Select(RenderCallArgument)];
		if (WriteMethodCallArguments(arguments, value.WriteArgumentsOnSeparateLines, forceNotNull ? ")!;" : ");"))
			return true;
		Write(')');
		if (forceNotNull)
			Write('!');
		return false;
	}

	/// <summary>Writes a return statement.</summary>
	/// <example><code>writer.WriteReturn("value"); // return value;</code></example>
	public CodeWriter WriteReturn(string? expression = null)
	{
		if (string.IsNullOrWhiteSpace(expression))
			return WriteLine("return;");
		Write("return ");
		WriteExpression(expression, expressionWriter: null);
		return WriteLine(";");
	}

	/// <summary>Writes a return statement using a callback for a multiline expression.</summary>
	/// <example><code>writer.WriteReturn(expression =&gt; expression.Write("value"));</code></example>
	public CodeWriter WriteReturn(Action<CodeWriter> writeExpression)
	{
		if (writeExpression is null)
			throw new ArgumentNullException(nameof(writeExpression));
		Write("return ");
		WriteExpression(null, writeExpression);
		return WriteLine(";");
	}

	/// <summary>Writes a throw statement.</summary>
	/// <example><code>writer.WriteThrow("new InvalidOperationException()");</code></example>
	public CodeWriter WriteThrow(string expression)
	{
		ValidateStatementPart(expression, nameof(expression));
		Write("throw ");
		WriteExpression(expression, expressionWriter: null);
		return WriteLine(";");
	}

	/// <summary>Writes a throw statement.</summary>
	/// <example><code>writer.WriteThrow("new InvalidOperationException()");</code></example>
	public CodeWriter WriteThrow(TypeReferenceOptions exceptionType, string? message = null)
	{
		if (exceptionType.IsNullOrEmpty())
			throw new ArgumentException("Exception type cannot be null or empty.", nameof(exceptionType));

		Write("throw ");
		WriteExpression(
			$"{exceptionType}{(message is null ? string.Empty : $"(\"{message}\")")}",
			expressionWriter: null
		);

		return WriteLine(";");
	}

	/// <summary>Writes a throw statement using a callback for a multiline expression.</summary>
	/// <example><code>writer.WriteThrow(expression =&gt; expression.Write("new InvalidOperationException()"));</code></example>
	public CodeWriter WriteThrow(Action<CodeWriter> writeExpression)
	{
		if (writeExpression is null)
			throw new ArgumentNullException(nameof(writeExpression));
		Write("throw ");
		WriteExpression(null, writeExpression);
		return WriteLine(";");
	}

	/// <summary>
	/// Writes an if statement with a block body and invokes a callback for the body.
	/// </summary>
	/// <param name="condition">The condition of the if statement.</param>
	/// <param name="bodyWriter">The action to invoke for the body of the if statement.</param>
	/// <returns>The current writer.</returns>
	/// <exception cref="ArgumentException">Thrown if the condition is null or whitespace.</exception>
	/// <exception cref="ArgumentNullException">Thrown if the bodyWriter is null.</exception>
	/// <example><code>writer.WriteIfBlock("enabled", body =&gt; body.WriteReturn());</code></example>
	public CodeWriter WriteIfBlock(string condition, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));

		using (WriteIfBlockScope(condition))
			bodyWriter(this);

		return this;
	}

	/// <summary>Writes an if statement and returns its body scope.</summary>
	/// <example><code>using (writer.WriteIfBlockScope("enabled")) writer.WriteReturn();</code></example>
	public BlockScope WriteIfBlockScope(string condition)
	{
		ValidateStatementPart(condition, nameof(condition));
		Write("if (");
		WriteExpression(condition, expressionWriter: null);
		WriteLine(")");
		return OpenBlockScope();
	}

	/// <summary>
	/// Writes a comma-separated collection with one item per line.
	/// </summary>
	/// <param name="items">The items to write.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.MultiLineItems("first", "second");</code></example>
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
	/// <example><code>writer.WriteLines(["first", null, "second"]);</code></example>
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
	/// <example><code>writer.WriteDelimited(["a", "b"], " | "); // a | b</code></example>
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
	/// <example><code>using (writer.IndentedScope()) writer.WriteLine("value");</code></example>
	public IndentScope IndentedScope()
	{
		Indent();
		return new(this, OpenScope("indentation", header: null, new StackTrace(1, fNeedFileInfo: true).ToString()));
	}

	/// <summary>Invokes a callback at one additional indentation level.</summary>
	/// <summary>Invokes a callback at one additional indentation level.</summary>
	/// <param name="bodyWriter">The action to invoke while indented.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.Indented(body =&gt; body.WriteLine("value"));</code></example>
	public CodeWriter Indented(Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));
		using (IndentedScope())
			bodyWriter(this);
		return this;
	}

	/// <summary>
	/// Writes a line and increases indentation until the returned scope is disposed.
	/// </summary>
	/// <param name="line">The line to write before indenting.</param>
	/// <returns>A scope that restores the indentation level.</returns>
	/// <example><code>using (writer.IndentedScope("if (enabled)")) writer.WriteLine("Run();");</code></example>
	public IndentScope IndentedScope(string line)
	{
		WriteLine(line);
		return IndentedScope();
	}

	/// <summary>Writes a line and invokes a callback at one additional indentation level.</summary>
	/// <summary>Writes a line and invokes a callback at one additional indentation level.</summary>
	/// <param name="line">The line to write before indenting.</param>
	/// <param name="bodyWriter">The action to invoke while indented.</param>
	/// <returns>The current writer.</returns>
	/// <example><code>writer.Indented("if (enabled)", body =&gt; body.WriteLine("Run();"));</code></example>
	public CodeWriter Indented(string line, Action<CodeWriter> bodyWriter)
	{
		if (bodyWriter is null)
			throw new ArgumentNullException(nameof(bodyWriter));
		using (IndentedScope(line))
			bodyWriter(this);
		return this;
	}

	/// <summary>
	/// Creates the generated source string.
	/// </summary>
	/// <returns>The complete contents of the writer.</returns>
	/// <example><code>writer.WriteLine("class C { }");
	/// var source = writer.ToString(); // class C { }</code></example>
	[SuppressMessage(
		"Design",
		"CA1065:Do not raise exceptions in unexpected locations",
		Justification = "Throwing is explicit opt-in validation for detecting unclosed generated-code scopes during development and tests."
	)]
	public override string ToString()
	{
		if (ThrowOnUnclosedScopes && OpenScopeCount != 0)
			throw new CodeWriterScopeValidationException(_openScopes!.Values);

		// Note: StringBuilder.ToString() is optimized to avoid copying the buffer when possible, so this is a low-cost operation.
		return _builder.ToString();
	}

	/// <summary>
	/// Creates a <see cref="Microsoft.CodeAnalysis.Text.SourceText"/> from the writer's contents.
	/// </summary>
	/// <example><code>Microsoft.CodeAnalysis.Text.SourceText sourceText = writer; // class C { }</code></example>
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

	void WriteExpression(string? expression, Action<CodeWriter>? expressionWriter)
	{
		var callback = expressionWriter;
		if (callback is not null)
		{
			var expressionWriterBuffer = new CodeWriter(GeneratorName, GeneratorVersion)
			{
				DefaultIncludeGeneratedAttributes = DefaultIncludeGeneratedAttributes,
			};
			callback!(expressionWriterBuffer);
			expression = expressionWriterBuffer.ToString().TrimEnd(NewLineCharacter);
		}

		if (string.IsNullOrEmpty(expression))
			return;

		var lines = expression!.Split(NewLineCharacter);
		for (var index = 0; index < lines.Length; index++)
		{
			if (index != 0)
			{
				NewLine();
				Indent();
			}
			Write(lines[index].TrimEnd('\r'));
			if (index != 0)
				Unindent();
		}
	}

	static void ValidateStatementPart(string? value, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentException("Statement text cannot be null or whitespace.", parameterName);
	}

	void WriteParametersWithHeuristic(ImmutableArray<ParameterDeclarationOptions> parameters)
	{
		if (parameters.IsDefault)
			parameters = [];

		var inlineLength = CurrentLineLength + 2;
		for (var index = 0; index < parameters.Length; index++)
			inlineLength += GetParameterLength(parameters[index]) + (index == 0 ? 0 : 2);

		Write('(');
		if (inlineLength <= DefaultMaximumLineLength)
		{
			for (var index = 0; index < parameters.Length; index++)
			{
				if (index != 0)
					Write(", ");
				WriteParameter(parameters[index]);
			}
			Write(')');
			return;
		}

		NewLine().Indent();
		for (var index = 0; index < parameters.Length; index++)
		{
			WriteParameter(parameters[index]).Write(index == parameters.Length - 1 ? ")" : ",");
			if (index != parameters.Length - 1)
				NewLine();
		}
		Unindent();
	}

	CodeWriter WriteParameter(ParameterDeclarationOptions parameter)
	{
		for (var index = 0; !parameter.Attributes.IsDefaultOrEmpty && index < parameter.Attributes.Length; index++)
			WriteAttribute(parameter.Attributes[index], defaultTarget: null).Write(' ');
		WriteIf(parameter.IsThis, "this ")
			.WriteIf(parameter.IsScoped, "scoped ")
			.WriteIf(parameter.IsParams, "params ")
			.Write(
				parameter.Modifier switch
				{
					ParameterModifier.None => string.Empty,
					ParameterModifier.Ref => "ref ",
					ParameterModifier.Out => "out ",
					ParameterModifier.In => "in ",
					ParameterModifier.RefReadOnly => "ref readonly ",
					_ => throw new ArgumentOutOfRangeException(nameof(parameter)),
				}
			)
			.WriteTypeReference(GetParameterType(parameter))
			.Write(' ')
			.Write(parameter.Name);
		if (parameter.DefaultValue is not null)
			Write(" = ").Write(parameter.DefaultValue);
		return this;
	}

	static int GetParameterLength(ParameterDeclarationOptions parameter)
	{
		var length = GetTypeReferenceLength(GetParameterType(parameter)) + parameter.Name.Length + 1;
		if (parameter.IsThis)
			length += 5;
		if (parameter.IsScoped)
			length += 7;
		if (parameter.IsParams)
			length += 7;
		length += parameter.Modifier switch
		{
			ParameterModifier.None => 0,
			ParameterModifier.Ref or ParameterModifier.Out => 4,
			ParameterModifier.In => 3,
			ParameterModifier.RefReadOnly => 13,
			_ => 0,
		};
		if (parameter.DefaultValue is not null)
			length += parameter.DefaultValue.Length + 3;
		for (var index = 0; !parameter.Attributes.IsDefaultOrEmpty && index < parameter.Attributes.Length; index++)
			length += GetAttributeLength(parameter.Attributes[index]) + 1;
		return length;
	}

	static TypeReferenceOptions GetParameterType(ParameterDeclarationOptions parameter) => parameter.Reference;

	static string RenderCallArgument(MethodCallArgumentOptions argument)
	{
		ValidateRequired(argument.Value, "Argument value", nameof(argument));
		if (argument.Name is not null)
			ValidateRequired(argument.Name, "Argument name", nameof(argument));
		return (
				argument.Modifier switch
				{
					ParameterModifier.None => string.Empty,
					ParameterModifier.Ref => "ref ",
					ParameterModifier.Out => "out ",
					ParameterModifier.In => "in ",
					ParameterModifier.RefReadOnly => "ref readonly ",
					_ => throw new ArgumentOutOfRangeException(nameof(argument)),
				}
			)
			+ (argument.Name is null ? string.Empty : argument.Name + ": ")
			+ argument.Value;
	}

	void WriteAttributes(ImmutableArray<AttributeDeclarationOptions> attributes, string? defaultTarget = null)
	{
		ValidateAttributes(attributes, nameof(attributes));
		for (var index = 0; !attributes.IsDefaultOrEmpty && index < attributes.Length; index++)
			WriteAttribute(attributes[index], defaultTarget).NewLine();
	}

	CodeWriter WriteAttribute(AttributeDeclarationOptions attribute, string? defaultTarget)
	{
		Write('[');
		var target = attribute.Target ?? defaultTarget;
		if (target is not null)
			Write(target).Write(": ");
		Write(TypeHelpers.GetTypeName(attribute.Reference.Type.RenderFullName));
		if (!attribute.Arguments.IsDefaultOrEmpty)
		{
			Write('(');
			for (var index = 0; index < attribute.Arguments.Length; index++)
			{
				if (index != 0)
					Write(", ");
				var argument = attribute.Arguments[index];
				if (argument.Name is not null)
					Write(argument.Name).Write(argument.IsPropertyAssignment ? " = " : ": ");
				Write(argument.Value);
			}
			Write(')');
		}
		return Write(']');
	}

	static int GetAttributeLength(AttributeDeclarationOptions attribute)
	{
		var length = attribute.Reference.Type.RenderAttributeName.Length + 2;
		if (attribute.Target is not null)
			length += attribute.Target.Length + 2;
		if (attribute.Arguments.IsDefaultOrEmpty)
			return length;
		length += 2;
		for (var index = 0; index < attribute.Arguments.Length; index++)
		{
			if (index != 0)
				length += 2;
			var argument = attribute.Arguments[index];
			length += argument.Value.Length;
			if (argument.Name is not null)
				length += argument.Name.Length + 3;
		}
		return length;
	}

	void WriteMemberModifiers(
		TypeDeclarationAccessibility? accessibility,
		bool isStatic,
		bool isAbstract,
		bool isVirtual,
		bool isOverride,
		bool isSealed
	)
	{
		if (accessibility is { } value)
			WriteAccessibility(value).Write(' ');
		WriteIf(isStatic, "static ")
			.WriteIf(isSealed, "sealed ")
			.WriteIf(isAbstract, "abstract ")
			.WriteIf(isVirtual, "virtual ")
			.WriteIf(isOverride, "override ");
	}

	CodeWriter WritePropertyHeader(PropertyDeclarationOptions declaration)
	{
		WriteMemberModifiers(
			declaration.Accessibility,
			declaration.IsStatic,
			declaration.IsAbstract,
			declaration.IsVirtual,
			declaration.IsOverride,
			declaration.IsSealed
		);
		return WriteTypeReference(declaration.Type).Write(' ').Write(declaration.Name);
	}

	void WriteAccessor(TypeDeclarationAccessibility? accessibility, string accessor)
	{
		if (accessibility is { } value)
			WriteAccessibility(value).Write(' ');
		Write(accessor).Write(' ');
	}

	void WriteAccessorBody(TypeDeclarationAccessibility? accessibility, string accessor, Action<CodeWriter>? writeBody)
	{
		if (accessibility is { } value)
			WriteAccessibility(value).Write(' ');
		if (writeBody is null)
		{
			Write(accessor).WriteLine(";");
			return;
		}
		using (OpenBlockScope(accessor))
			writeBody(this);
	}

	void BeginWrittenItem(WrittenItemKind nextItem)
	{
		if (_lastWrittenItem == WrittenItemKind.None || _lastWrittenItemIndent != _indentLevel)
			return;

		var requiresBlankLine = _lastWrittenItem != WrittenItemKind.Field || nextItem != WrittenItemKind.Field;
		if (
			requiresBlankLine
			&& (_builder.Length == _lastWrittenItemEnd || _builder[_lastWrittenItemEnd] != NewLineCharacter)
		)
		{
			_builder.Insert(_lastWrittenItemEnd, NewLineCharacter);
		}

		// A declaration can only consume the preceding item's separator once. Its own completion
		// establishes the state used by the following declaration.
		_lastWrittenItem = WrittenItemKind.None;
	}

	void CompleteWrittenItem(WrittenItemKind item, int indent)
	{
		_lastWrittenItem = item;
		_lastWrittenItemIndent = indent;
		_lastWrittenItemEnd = _builder.Length;
	}

	BlockScope OpenBlockScope(WrittenItemKind completedItem)
	{
		var itemIndent = _indentLevel;
		WriteLine("{").Indent();
		return TrackOpenBlockScope(header: null, closingSeparator: "}", completedItem, itemIndent);
	}

	int CurrentLineLength
	{
		get
		{
			var length = 0;
			for (var index = _builder.Length - 1; index >= 0; index--)
			{
				if (_builder[index] == NewLineCharacter)
					break;
				length += _builder[index] == IndentCharacter ? IndentDisplayWidth : 1;
			}
			return length + (_atLineStart ? _indentLevel * IndentDisplayWidth : 0);
		}
	}

	BlockScope TrackOpenBlockScope(
		string? header,
		string? closingSeparator,
		WrittenItemKind completedItem = WrittenItemKind.None,
		int itemIndent = -1
	)
	{
		return new BlockScope(
			this,
			closingSeparator,
			OpenScope("block", header, new StackTrace(1, fNeedFileInfo: true).ToString()),
			(int)completedItem,
			itemIndent
		);
	}

	int OpenScope(string kind, string? header, string capturedStackTrace)
	{
		OpenScopeCount++;
		if (_openScopes is null)
			return 0;

		var scopeId = ++_nextScopeId;
		_openScopes.Add(scopeId, new CodeWriterOpenScope(kind, header, capturedStackTrace));
		return scopeId;
	}

	void CloseBlock(string? closingSeparator, int scopeId, int completedItem, int itemIndent)
	{
		CloseScope(scopeId, "block");

		Unindent();
		if (closingSeparator is not null)
			WriteLine(closingSeparator);

		if (completedItem != (int)WrittenItemKind.None)
			CompleteWrittenItem((WrittenItemKind)completedItem, itemIndent);
	}

	void CloseIndentScope(int scopeId)
	{
		CloseScope(scopeId, "indentation");
		Unindent();
	}

	void CloseScope(int scopeId, string kind)
	{
		if (OpenScopeCount <= 0)
			throw new InvalidOperationException($"A {kind} scope was closed without a matching open scope.");

		if (_openScopes is not null && !_openScopes.Remove(scopeId))
			throw new InvalidOperationException($"The {kind} scope has already been closed.");

		OpenScopeCount--;
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
		ImmutableArray<ParameterDeclarationOptions> parameters,
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
				if (index != 0 && !writeOnSeparateLines)
					Write(", ");

				if (writeOnSeparateLines)
					NewLine();

				WriteParameter(parameters[index]);
				if (writeOnSeparateLines && index != parameters.Length - 1)
					Write(',');
			}

			if (writeOnSeparateLines)
				Unindent().NewLine();
		}

		Write(')');
	}

	void WriteBaseTypes(TypeDeclarationOptions declaration)
	{
		var hasBaseType = declaration.BaseType is { IsEmpty: false };
		var hasInterfaces = HasNonEmptyTypeReferences(declaration.Interfaces);
		if (!hasBaseType && !hasInterfaces)
			return;

		Write(" : ");
		if (hasBaseType)
			WriteTypeReference(declaration.BaseType!);

		if (!hasInterfaces)
			return;

		var wroteType = hasBaseType;
		for (var index = 0; index < declaration.Interfaces.Length; index++)
		{
			if (declaration.Interfaces[index].IsEmpty)
				continue;
			if (wroteType)
				Write(", ");

			WriteTypeReference(declaration.Interfaces[index]);
			wroteType = true;
		}
	}

	static bool HasNonEmptyTypeReferences(ImmutableArray<TypeReferenceOptions> types)
	{
		for (var index = 0; !types.IsDefaultOrEmpty && index < types.Length; index++)
		{
			if (!types[index].IsEmpty)
				return true;
		}
		return false;
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
			for (var constraintIndex = 0; constraintIndex < genericType.Constraints.Length; constraintIndex++)
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
		var isStruct = declaration.Kind is TypeDeclarationKind.Struct or TypeDeclarationKind.RecordStruct;
		var supportsPrimaryConstructor =
			declaration.Kind
			is TypeDeclarationKind.Class
				or TypeDeclarationKind.Struct
				or TypeDeclarationKind.RecordClass
				or TypeDeclarationKind.RecordStruct;
		ValidateTypeDeclarationModifiers(declaration, isStruct);
		ValidateAdditionalTypeKindOptions(declaration, supportsPrimaryConstructor);

		ValidateParameters(
			declaration.PrimaryConstructorParameters,
			"Primary-constructor parameters cannot contain null or whitespace values.",
			nameof(declaration)
		);

		for (var index = 0; !declaration.Interfaces.IsDefaultOrEmpty && index < declaration.Interfaces.Length; index++)
		{
			ValidateTypeReference(declaration.Interfaces[index], nameof(declaration));
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

			for (var constraintIndex = 0; constraintIndex < genericType.Constraints.Length; constraintIndex++)
			{
				if (string.IsNullOrWhiteSpace(genericType.Constraints[constraintIndex]))
					throw new ArgumentException(
						"Generic constraints cannot be null or whitespace.",
						nameof(declaration)
					);
			}
		}
	}

	static void ValidateTypeDeclarationModifiers(TypeDeclarationOptions declaration, bool isStruct)
	{
		if (declaration.IsStatic && declaration.Kind != TypeDeclarationKind.Class)
			throw new ArgumentException("Only class declarations can be static.", nameof(declaration));

		if (
			declaration.IsAbstract
			&& declaration.Kind is not TypeDeclarationKind.Class and not TypeDeclarationKind.RecordClass
		)
			throw new ArgumentException(
				"Only class and record class declarations can be abstract.",
				nameof(declaration)
			);

		if (declaration.IsAbstract && declaration.IsStatic)
			throw new ArgumentException("A class cannot be both abstract and static.", nameof(declaration));

		if (declaration.IsStatic && declaration.BaseType is { IsEmpty: false })
			throw new ArgumentException("A static class cannot specify a base type.", nameof(declaration));

		if (declaration.IsStatic && HasNonEmptyTypeReferences(declaration.Interfaces))
			throw new ArgumentException("A static class cannot implement interfaces.", nameof(declaration));

		if (declaration.IsStatic && !declaration.PrimaryConstructorParameters.IsDefaultOrEmpty)
			throw new ArgumentException(
				"A static class cannot declare primary-constructor parameters.",
				nameof(declaration)
			);

		if (isStruct && declaration.BaseType is { IsEmpty: false })
			throw new ArgumentException(
				"Struct and record struct declarations cannot specify a base type.",
				nameof(declaration)
			);

		if (!isStruct && declaration.IsReadOnly)
			throw new ArgumentException(
				"Only struct and record struct declarations can be readonly.",
				nameof(declaration)
			);
	}

	static void ValidateAdditionalTypeKindOptions(TypeDeclarationOptions declaration, bool supportsPrimaryConstructor)
	{
		if (
			declaration.Kind
				is TypeDeclarationKind.Interface
					or TypeDeclarationKind.Enum
					or TypeDeclarationKind.Delegate
			&& declaration.BaseType is { IsEmpty: false }
		)
			throw new ArgumentException(
				"Interfaces, enums, and delegates cannot specify BaseType. Use Interfaces for interface inheritance and EnumUnderlyingType for enums.",
				nameof(declaration)
			);
		if (!supportsPrimaryConstructor && !declaration.PrimaryConstructorParameters.IsDefaultOrEmpty)
			throw new ArgumentException(
				"This type declaration does not support primary-constructor parameters.",
				nameof(declaration)
			);
		if (
			declaration.Kind is TypeDeclarationKind.Enum or TypeDeclarationKind.Delegate
			&& HasNonEmptyTypeReferences(declaration.Interfaces)
		)
			throw new ArgumentException("Enums and delegates cannot declare interfaces.", nameof(declaration));
		if (declaration.Kind == TypeDeclarationKind.Enum && !declaration.GenericTypes.IsDefaultOrEmpty)
			throw new ArgumentException("Enums cannot be generic.", nameof(declaration));
		if (declaration.EnumUnderlyingType is not null && declaration.Kind != TypeDeclarationKind.Enum)
			throw new ArgumentException("EnumUnderlyingType is only valid for enum declarations.", nameof(declaration));
		if (
			declaration.Kind == TypeDeclarationKind.Enum
			&& declaration.EnumUnderlyingType is not null
			&& string.IsNullOrWhiteSpace(declaration.EnumUnderlyingType.Type.Name)
		)
			throw new ArgumentException("Enum underlying type cannot be whitespace.", nameof(declaration));
		if (declaration.Kind == TypeDeclarationKind.Delegate)
		{
			if (declaration.DelegateReturnType is null)
				throw new ArgumentException("Delegate return type is required.", nameof(declaration));
			ValidateTypeReference(declaration.DelegateReturnType, nameof(declaration));
			ValidateParameters(
				declaration.DelegateParameters,
				"Delegate parameters cannot contain null or whitespace values.",
				nameof(declaration)
			);
		}
		else if (declaration.DelegateReturnType is not null || !declaration.DelegateParameters.IsDefaultOrEmpty)
			throw new ArgumentException(
				"Delegate return type and parameters are only valid for delegate declarations.",
				nameof(declaration)
			);
	}

	static void ValidateConstructorDeclaration(ConstructorDeclarationOptions declaration)
	{
		if (declaration.Accessibility == TypeDeclarationAccessibility.File)
			throw new ArgumentException(
				"The file accessibility modifier is only valid for types.",
				nameof(declaration)
			);
		ValidateParameters(
			declaration.Parameters,
			"Constructor parameters cannot contain null or whitespace values.",
			nameof(declaration)
		);

		if (declaration.IsStatic && !declaration.Parameters.IsDefaultOrEmpty)
			throw new ArgumentException("A static constructor cannot declare parameters.", nameof(declaration));

		if (declaration.IsStatic && !string.IsNullOrWhiteSpace(declaration.Initializer))
			throw new ArgumentException("A static constructor cannot specify an initializer.", nameof(declaration));

		if (declaration.IsStatic && declaration.Accessibility is not null)
			throw new ArgumentException("A static constructor cannot specify accessibility.", nameof(declaration));
	}

	void WriteMethodGenericConstraints(ImmutableArray<GenericTypeParameterOptions> genericTypes)
	{
		if (genericTypes.IsDefaultOrEmpty)
			return;

		var wroteConstraint = false;
		for (var typeIndex = 0; typeIndex < genericTypes.Length; typeIndex++)
		{
			var genericType = genericTypes[typeIndex];
			if (genericType.Constraints.IsDefaultOrEmpty)
				continue;
			if (wroteConstraint)
				NewLine();
			Write("where ").Write(genericType.Name).Write(" : ");
			for (var index = 0; index < genericType.Constraints.Length; index++)
			{
				if (index != 0)
					Write(", ");
				Write(genericType.Constraints[index]);
			}
			wroteConstraint = true;
		}
	}

	static void ValidateMethodDeclaration(MethodDeclarationOptions declaration)
	{
		ValidateRequired(declaration.Name, "Method name", nameof(declaration));
		ValidateTypeReference(declaration.ReturnType, nameof(declaration));
		ValidateMemberModifiers(
			declaration.Accessibility,
			declaration.IsAbstract,
			declaration.IsVirtual,
			declaration.IsOverride,
			declaration.IsSealed,
			nameof(declaration)
		);
		ValidateParameters(
			declaration.Parameters,
			"Method parameters cannot contain null or whitespace values.",
			nameof(declaration)
		);
		if (declaration.IsAbstract && declaration.ExpressionBody is not null)
			throw new ArgumentException("An abstract method cannot have an expression body.", nameof(declaration));
	}

	static void ValidatePropertyDeclaration(PropertyDeclarationOptions declaration)
	{
		ValidateRequired(declaration.Name, "Property name", nameof(declaration));
		ValidateTypeReference(declaration.Type, nameof(declaration));
		ValidateMemberModifiers(
			declaration.Accessibility,
			declaration.IsAbstract,
			declaration.IsVirtual,
			declaration.IsOverride,
			declaration.IsSealed,
			nameof(declaration)
		);
		if (!declaration.HasGetter && !declaration.HasSetter && !declaration.IsInitOnly)
			throw new ArgumentException(
				"A property must have a getter, setter, or init accessor.",
				nameof(declaration)
			);
		if (declaration.ExpressionBody is not null && declaration.HasSetter)
			throw new ArgumentException("An expression-bodied property cannot have a setter.", nameof(declaration));
		if (declaration.ExpressionBody is not null && declaration.Initializer is not null)
			throw new ArgumentException(
				"A property cannot specify both an expression body and an initializer.",
				nameof(declaration)
			);
		if (declaration.IsAbstract && declaration.ExpressionBody is not null)
			throw new ArgumentException("An abstract property cannot have an expression body.", nameof(declaration));
	}

	static void ValidateFieldDeclaration(FieldDeclarationOptions declaration)
	{
		ValidateRequired(declaration.Name, "Field name", nameof(declaration));
		ValidateTypeReference(declaration.Type, nameof(declaration));
		if (declaration.Accessibility == TypeDeclarationAccessibility.File)
			throw new ArgumentException(
				"The file accessibility modifier is only valid for types.",
				nameof(declaration)
			);
		if (declaration.IsConst && (declaration.IsStatic || declaration.IsReadOnly || declaration.IsVolatile))
			throw new ArgumentException(
				"A const field cannot also be static, readonly, or volatile.",
				nameof(declaration)
			);
		if (declaration.IsReadOnly && declaration.IsVolatile)
			throw new ArgumentException("A field cannot be both readonly and volatile.", nameof(declaration));
		if (declaration.IsConst && declaration.Initializer is null)
			throw new ArgumentException("A const field requires an initializer.", nameof(declaration));
	}

	static void ValidateEnumFieldDeclaration(EnumFieldDeclarationOptions declaration)
	{
		ValidateRequired(declaration.FieldName, "Enum field name", nameof(declaration));
		ValidateAttributes(declaration.Attributes, nameof(declaration));
	}

	static void ValidateMemberModifiers(
		TypeDeclarationAccessibility? accessibility,
		bool isAbstract,
		bool isVirtual,
		bool isOverride,
		bool isSealed,
		string parameterName
	)
	{
		if (accessibility == TypeDeclarationAccessibility.File)
			throw new ArgumentException("The file accessibility modifier is only valid for types.", parameterName);
		var count = (isAbstract ? 1 : 0) + (isVirtual ? 1 : 0) + (isOverride ? 1 : 0);
		if (count > 1)
			throw new ArgumentException(
				"A member cannot be abstract, virtual, and override at the same time.",
				parameterName
			);
		if (isSealed && !isOverride)
			throw new ArgumentException("Only an override member can be sealed.", parameterName);
	}

	static void ValidateRequired(string? value, string description, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentException($"{description} cannot be null or whitespace.", parameterName);
	}

	static string? NormalizeOptionalIdentity(string? value, string parameterName)
	{
		if (value is null)
			return null;
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentException("Generator identity values cannot be empty or whitespace.", parameterName);

		// Normalize the value to a consistent form for comparison and storage.
		return value;
	}

	static void ValidateParameters(
		ImmutableArray<ParameterDeclarationOptions> parameters,
		string message,
		string parameterName
	)
	{
		if (parameters.IsDefaultOrEmpty)
			return;

		for (var index = 0; index < parameters.Length; index++)
		{
			if (
				string.IsNullOrWhiteSpace(parameters[index].Name)
				|| string.IsNullOrWhiteSpace(parameters[index].Reference.Type.Name)
			)
				throw new ArgumentException(message, parameterName);
			else
			{
				ValidateTypeReference(parameters[index].Reference, parameterName);
				ValidateAttributes(parameters[index].Attributes, parameterName);
			}
		}
	}

	CodeWriter WriteTypeReference(TypeReferenceOptions reference)
	{
		if (reference.IsEmpty)
			return this;

		ValidateTypeReference(reference, nameof(reference));

		Write(reference.RenderFullName);

		//Write(type.Name);
		//if (!type.TypeArguments.IsDefaultOrEmpty)
		//{
		//	Write('<');
		//	for (var index = 0; index < type.TypeArguments.Length; index++)
		//	{
		//		if (index != 0)
		//			Write(", ");
		//		WriteTypeReference(type.TypeArguments[index]);
		//	}
		//	Write('>');
		//}
		//else if (type.GenericArity > 0)
		//	Write('<').Write(new string(',', type.GenericArity - 1)).Write('>');

		//for (var index = 0; !reference.ArrayRanks.IsDefaultOrEmpty && index < reference.ArrayRanks.Length; index++)
		//	Write('[').Write(new string(',', reference.ArrayRanks[index] - 1)).Write(']');

		//WriteIf(reference.IsPointer, "*").WriteIf(reference.IsNullable, "?");
		return this;
	}

	static int GetTypeReferenceLength(TypeReferenceOptions type) => type.IsEmpty ? 0 : type.RenderFullName.Length;

	static void ValidateTypeReference(TypeReferenceOptions reference, string parameterName)
	{
		if (reference.IsEmpty)
			return;

		var type = reference.Type;
		if (string.IsNullOrWhiteSpace(type.Name))
			throw new ArgumentException("Type name cannot be null or whitespace.", parameterName);
		if (type.GenericArity < 0)
			throw new ArgumentException("Generic arity cannot be negative.", parameterName);
		if (type.GenericArity != 0 && !type.TypeArguments.IsDefaultOrEmpty)
			throw new ArgumentException(
				"A type cannot have both open generic arity and concrete generic arguments.",
				parameterName
			);
		for (var index = 0; !type.TypeArguments.IsDefaultOrEmpty && index < type.TypeArguments.Length; index++)
			ValidateTypeReference(type.TypeArguments[index], parameterName);
	}

	static void ValidateAttributes(ImmutableArray<AttributeDeclarationOptions> attributes, string parameterName)
	{
		for (var index = 0; !attributes.IsDefaultOrEmpty && index < attributes.Length; index++)
		{
			var attribute = attributes[index];
			if (attribute.Reference.IsEmpty)
				throw new ArgumentException("Attribute type names cannot be null or whitespace.", parameterName);
			if (attribute.Target is not null && string.IsNullOrWhiteSpace(attribute.Target))
				throw new ArgumentException("Attribute targets cannot be whitespace.", parameterName);
			for (
				var argumentIndex = 0;
				!attribute.Arguments.IsDefaultOrEmpty && argumentIndex < attribute.Arguments.Length;
				argumentIndex++
			)
			{
				var argument = attribute.Arguments[argumentIndex];
				if (string.IsNullOrWhiteSpace(argument.Value))
					throw new ArgumentException(
						"Attribute argument values cannot be null or whitespace.",
						parameterName
					);
				if (argument.Name is not null && string.IsNullOrWhiteSpace(argument.Name))
					throw new ArgumentException("Attribute argument names cannot be whitespace.", parameterName);
			}
		}
	}

	static bool HasGenericConstraints(ImmutableArray<GenericTypeParameterOptions> genericTypes)
	{
		if (genericTypes.IsDefaultOrEmpty)
			return false;

		for (var index = 0; index < genericTypes.Length; index++)
			if (!genericTypes[index].Constraints.IsDefaultOrEmpty)
				return true;
		return false;
	}
}
