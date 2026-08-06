using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// Describes a target symbol and its declaration syntax for source generation.
/// </summary>
public sealed record class TargetSymbolDescriptor(
	INamedTypeSymbol Symbol,
	TypeDeclarationSyntax? Declaration
);
