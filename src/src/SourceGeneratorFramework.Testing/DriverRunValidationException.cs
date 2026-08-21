using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing.Models;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Describes an exception thrown by a source generator during a test run.
/// </summary>
/// <param name="GeneratorName">The generator type name.</param>
/// <param name="Exception">The exception thrown by the generator.</param>
public sealed record GeneratorFailure(string GeneratorName, Exception Exception);

/// <summary>
/// Represents all validation failures found after a source generator test run.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
public sealed class DriverRunValidationException : Exception
{
	internal DriverRunValidationException(
		DriverRunResult runResult,
		IEnumerable<GeneratorFailure> generatorFailures,
		IEnumerable<Diagnostic> compilationErrors,
		IEnumerable<Diagnostic> emitErrors,
		IEnumerable<LogEntry> logErrors,
		IEnumerable<SyntaxTree> generatedTrees,
		IEnumerable<SyntaxTree> compilationTrees
	)
		: this(
			runResult,
			[.. generatorFailures],
			[.. compilationErrors],
			[.. emitErrors],
			[.. logErrors],
			[.. generatedTrees],
			[.. compilationTrees]
		)
	{
		// Here to prevent IDE0055
	}

	DriverRunValidationException(
		DriverRunResult runResult,
		ImmutableArray<GeneratorFailure> generatorFailures,
		ImmutableArray<Diagnostic> compilationErrors,
		ImmutableArray<Diagnostic> emitErrors,
		ImmutableArray<LogEntry> logErrors,
		ImmutableArray<SyntaxTree> generatedTrees,
		ImmutableArray<SyntaxTree> compilationTrees
	)
		: base(
			BuildMessage(generatorFailures, compilationErrors, emitErrors, logErrors, generatedTrees, compilationTrees)
		)
	{
		RunResult = runResult;
		GeneratorFailures = generatorFailures;
		CompilationErrors = compilationErrors;
		EmitErrors = emitErrors;
		LogErrors = logErrors;
	}

	/// <summary> Gets the result of the source generator test run that failed validation. </summary>
	public DriverRunResult RunResult { get; }

	/// <summary>Gets exceptions thrown by generators.</summary>
	public IReadOnlyList<GeneratorFailure> GeneratorFailures { get; }

	/// <summary>Gets errors reported by the output compilation.</summary>
	public IReadOnlyList<Diagnostic> CompilationErrors { get; }

	/// <summary>Gets additional errors reported while emitting the output assembly.</summary>
	public IReadOnlyList<Diagnostic> EmitErrors { get; }

	/// <summary>Gets error-level entries written through generator logging.</summary>
	public IReadOnlyList<LogEntry> LogErrors { get; }

	static string BuildMessage(
		IReadOnlyList<GeneratorFailure> generatorFailures,
		IReadOnlyList<Diagnostic> compilationErrors,
		IReadOnlyList<Diagnostic> emitErrors,
		IReadOnlyList<LogEntry> logErrors,
		IReadOnlyList<SyntaxTree> generatedTrees,
		IReadOnlyList<SyntaxTree> compilationTrees
	)
	{
		StringBuilder builder = new();
		builder.AppendLine("Source generator test run was invalid.");

		if (generatorFailures.Count > 0)
		{
			builder
				.AppendLine()
				.Append("Generator exceptions (")
				.Append(generatorFailures.Count.ToString(CultureInfo.InvariantCulture))
				.AppendLine("):");
			foreach (var failure in generatorFailures)
			{
				builder.Append("  ").AppendLine(failure.GeneratorName);
				AppendIndented(builder, failure.Exception.ToString(), "    ");
			}
		}

		AppendDiagnostics(builder, "Compilation errors", compilationErrors, generatedTrees, compilationTrees);
		AppendDiagnostics(builder, "Emit errors", emitErrors, generatedTrees, compilationTrees);

		if (logErrors.Count > 0)
		{
			builder
				.AppendLine()
				.Append("Generator log errors (")
				.Append(logErrors.Count.ToString(CultureInfo.InvariantCulture))
				.AppendLine("):");
			foreach (var error in logErrors)
				builder.Append("  - ").AppendLine(error.Message);
		}

		return builder.ToString().TrimEnd();
	}

	static void AppendDiagnostics(
		StringBuilder builder,
		string heading,
		IReadOnlyList<Diagnostic> diagnostics,
		IReadOnlyList<SyntaxTree> generatedTrees,
		IReadOnlyList<SyntaxTree> compilationTrees
	)
	{
		if (diagnostics.Count == 0)
			return;

		builder
			.AppendLine()
			.Append(heading)
			.Append(" (")
			.Append(diagnostics.Count.ToString(CultureInfo.InvariantCulture))
			.AppendLine("):");

		foreach (var group in diagnostics.GroupBy(diagnostic => diagnostic.Location.SourceTree))
		{
			var tree = group.Key;
			builder.Append("  Source: ").AppendLine(GetTreeDescription(tree, generatedTrees, compilationTrees));

			foreach (var diagnostic in group)
				AppendDiagnostic(builder, diagnostic);
		}
	}

	static string GetTreeDescription(
		SyntaxTree? tree,
		IReadOnlyList<SyntaxTree> generatedTrees,
		IReadOnlyList<SyntaxTree> compilationTrees
	)
	{
		if (tree is null)
			return "<no source location>";

		var path = string.IsNullOrWhiteSpace(tree.FilePath)
			? $"<source {IndexOf(compilationTrees, tree) + 1}>"
			: tree.FilePath;
		var kind = Contains(generatedTrees, tree) ? "generated" : "input";
		return $"{path} ({kind})";
	}

	static void AppendDiagnostic(StringBuilder builder, Diagnostic diagnostic)
	{
		var lineSpan = diagnostic.Location.GetLineSpan();
		var start = lineSpan.StartLinePosition;
		builder
			.Append("    ")
			.Append(diagnostic.Severity)
			.Append(' ')
			.Append(diagnostic.Id)
			.Append(" at ")
			.Append(start.Line + 1)
			.Append(':')
			.Append(start.Character + 1)
			.Append(" - ")
			.AppendLine(diagnostic.GetMessage(CultureInfo.InvariantCulture));

		var tree = diagnostic.Location.SourceTree;
		if (tree is null || !diagnostic.Location.IsInSource)
			return;

		var text = tree.GetText();
		var firstLine = Math.Max(0, start.Line - 2);
		var lastLine = Math.Min(text.Lines.Count - 1, lineSpan.EndLinePosition.Line + 2);
		var width = (lastLine + 1).ToString(CultureInfo.InvariantCulture).Length;

		for (var lineIndex = firstLine; lineIndex <= lastLine; lineIndex++)
		{
			var marker = lineIndex >= start.Line && lineIndex <= lineSpan.EndLinePosition.Line ? '>' : ' ';
			builder
				.Append("    ")
				.Append(marker)
				.Append(' ')
				.Append((lineIndex + 1).ToString(CultureInfo.InvariantCulture).PadLeft(width))
				.Append(" | ")
				.AppendLine(text.Lines[lineIndex].ToString());

			if (lineIndex == start.Line)
			{
				var lineLength = text.Lines[lineIndex].Span.Length;
				var indicatedLength =
					lineSpan.EndLinePosition.Line == start.Line
						? lineSpan.EndLinePosition.Character - start.Character
						: lineLength - start.Character;
				indicatedLength = Math.Max(1, indicatedLength);
				builder
					.Append("      ")
					.Append(' ', width)
					.Append(" | ")
					.Append(' ', start.Character)
					.AppendLine(new string('^', indicatedLength));
			}
		}
	}

	static void AppendIndented(StringBuilder builder, string value, string indentation)
	{
		using StringReader reader = new(value);
		while (reader.ReadLine() is { } line)
			builder.Append(indentation).AppendLine(line);
	}

	static bool Contains(IReadOnlyList<SyntaxTree> trees, SyntaxTree tree) => IndexOf(trees, tree) >= 0;

	static int IndexOf(IReadOnlyList<SyntaxTree> trees, SyntaxTree tree)
	{
		for (var index = 0; index < trees.Count; index++)
		{
			if (ReferenceEquals(trees[index], tree))
				return index;
		}

		return -1;
	}
}
