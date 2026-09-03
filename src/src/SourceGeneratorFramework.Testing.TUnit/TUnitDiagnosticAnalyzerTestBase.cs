using Microsoft.CodeAnalysis.Diagnostics;

namespace Purview.SourceGeneratorFramework.Testing.TUnit;

/// <summary>
/// TUnit-specific base class for diagnostic analyzer tests.
/// </summary>
public abstract class TUnitDiagnosticAnalyzerTestBase<TAnalyzer>
	: TUnitDiagnosticAnalyzerTestBase<TAnalyzer, AnalyzerTestOptions>
	where TAnalyzer : DiagnosticAnalyzer, new();

/// <summary>
/// TUnit-specific base class for diagnostic analyzer tests.
/// </summary>
public abstract class TUnitDiagnosticAnalyzerTestBase<TAnalyzer, TOptions> : AnalyzerTestBase<TAnalyzer, TOptions>
	where TAnalyzer : DiagnosticAnalyzer, new()
	where TOptions : AnalyzerTestOptions, new();
