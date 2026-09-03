using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// The result of a diagnostic analyzer test run.
/// </summary>
public sealed record AnalyzerTestResult(ImmutableArray<Diagnostic> Diagnostics, Compilation Compilation);

/// <summary>
/// The result of a code fix test run.
/// </summary>
public sealed record CodeFixTestResult(
	ImmutableArray<Diagnostic> Diagnostics,
	ImmutableArray<CodeAction> CodeActions,
	string FixedSource,
	Compilation Compilation,
	Solution? ChangedSolution = null
);

/// <summary>
/// The result of a fix-all code fix test run.
/// </summary>
public sealed record CodeFixFixAllResult(
	ImmutableArray<Diagnostic> Diagnostics,
	ImmutableArray<CodeAction> CodeActions,
	ImmutableDictionary<string, string> FixedSources,
	Solution ChangedSolution
);
