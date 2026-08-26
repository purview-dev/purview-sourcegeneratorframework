using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>Executes an analyzer and applies one code fix to a test document.</summary>
public sealed class CodeFixTestRunner<TAnalyzer, TCodeFix> : RoslynTestRunner
	where TAnalyzer : DiagnosticAnalyzer, new()
	where TCodeFix : CodeFixProvider, new()
{
	/// <summary>Runs the analyzer and applies a registered code action.</summary>
	public async Task<CodeFixTestResult> RunAsync(
		string source,
		CodeFixTestOptions? options = null,
		CancellationToken cancellationToken = default
	)
	{
		options ??= new();
		using var testProject = CreateProject([source], options, typeof(TAnalyzer).Assembly);
		var compilation =
			await testProject.Project.GetCompilationAsync(cancellationToken)
			?? throw new InvalidOperationException("Unable to create the test compilation.");
		var diagnostics = await WithAnalyzers(compilation, [new TAnalyzer()], options)
			.GetAnalyzerDiagnosticsAsync(cancellationToken);
		var diagnostic =
			diagnostics.FirstOrDefault(diagnostic => diagnostic.Location.IsInSource)
			?? throw new InvalidOperationException("The analyzer did not report a source diagnostic.");
		var document = testProject.Project.Solution.GetDocument(testProject.DocumentIds[0])!;
		List<CodeAction> actions = [];
		var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), cancellationToken);
		await new TCodeFix().RegisterCodeFixesAsync(context);

		var action = options.EquivalenceKey is null
			? actions.ElementAtOrDefault(options.CodeActionIndex)
			: actions.FirstOrDefault(action => action.EquivalenceKey == options.EquivalenceKey);
		action = action ?? throw new InvalidOperationException("The requested code action was not registered.");

		var operations = await action.GetOperationsAsync(cancellationToken);
		var changedSolution =
			operations.OfType<ApplyChangesOperation>().SingleOrDefault()?.ChangedSolution
			?? throw new InvalidOperationException("The code action did not produce an ApplyChangesOperation.");
		var changedDocument =
			changedSolution.GetDocument(document.Id)
			?? throw new InvalidOperationException("The code action removed the source document.");
		var fixedSource = (await changedDocument.GetTextAsync(cancellationToken)).ToString();

		return new(diagnostics, [.. actions], fixedSource, compilation);
	}
}
