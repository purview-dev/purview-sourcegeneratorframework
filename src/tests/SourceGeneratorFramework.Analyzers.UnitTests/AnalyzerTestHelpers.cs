using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Analyzers;

public static class AnalyzerTestHelpers
{
	public static async Task<IEnumerable<Diagnostic>> GetAnalyzerDiagnosticsAsync(
		this DiagnosticAnalyzer analyzer,
		string source,
		CancellationToken cancellationToken = default
	)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
		};
		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

		return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken);
	}
}
