using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Purview.SourceGeneratorFramework.Testing.TUnit;

/// <summary>TUnit-specific base class for refactoring tests.</summary>
public abstract class TUnitRefactoringTestBase<TRefactoring>
	: TUnitRefactoringTestBase<TRefactoring, RefactorTestOptions>
	where TRefactoring : CodeRefactoringProvider, new();

/// <summary>TUnit-specific base class for refactoring tests.</summary>
public abstract class TUnitRefactoringTestBase<TRefactoring, TOptions>
	where TRefactoring : CodeRefactoringProvider, new()
	where TOptions : RefactorTestOptions, new()
{
	readonly RefactoringTestRunner<TRefactoring> _runner = new();

	/// <summary>Runs the refactoring against the supplied source.</summary>
	protected Task<RefactorTestResult> RefactorAsync(string source, CancellationToken cancellationToken = default) =>
		RefactorAsync(source, null!, cancellationToken);

	/// <summary>Runs the refactoring against the supplied source using the supplied options.</summary>
	protected Task<RefactorTestResult> RefactorAsync(
		string source,
		TOptions options,
		CancellationToken cancellationToken = default
	) => _runner.RunAsync(source, options ?? new(), cancellationToken);
}
