using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>The result of a refactoring test run.</summary>
/// <param name="CodeActions">The code actions registered by the refactoring provider.</param>
/// <param name="FixedSources">The refactored source of each document, keyed by document name.</param>
/// <param name="ChangedSolution">The solution after applying the selected refactoring.</param>
/// <param name="Compilation">The input compilation the refactoring was applied to.</param>
public sealed record RefactorTestResult(
	ImmutableArray<CodeAction> CodeActions,
	ImmutableDictionary<string, string> FixedSources,
	Solution ChangedSolution,
	Compilation Compilation
);
