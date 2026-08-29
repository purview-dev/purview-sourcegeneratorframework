using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.SourceGeneratorFramework.Testing;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Builds compilations for symbol- and syntax-level matching tests.
/// </summary>
public static class TestCompilation
{
	// Basic.Reference.Assemblies provides a single, unambiguous reference set. Building references from
	// AppDomain.CurrentDomain.GetAssemblies() makes GetTypeByMetadataName return null for types that are
	// forwarded across facade assemblies.
	static CSharpParseOptions ParseOptions => new(LanguageVersion.Latest);

	static CSharpCompilationOptions CompilationOptions =>
		new(
			OutputKind.DynamicallyLinkedLibrary,
			allowUnsafe: true,
			nullableContextOptions: NullableContextOptions.Enable
		);

	public static CSharpCompilation Create(string? source = null)
	{
		SyntaxTree[] trees = source is null ? [] : [CSharpSyntaxTree.ParseText(source, ParseOptions)];

		return CSharpCompilation.Create(
			"Tests",
			trees,
			SourceGeneratorHelpers.ResolveTrustedReferences,
			CompilationOptions
		);
	}

	public static (CSharpCompilation Compilation, CompilationUnitSyntax Root) CreateWithRoot(string source)
	{
		var compilation = Create(source);
		var root = (CompilationUnitSyntax)compilation.SyntaxTrees.Single().GetRoot();

		return (compilation, root);
	}

	public static (SyntaxTree Tree, CompilationUnitSyntax Root) Parse(string source)
	{
		var tree = CSharpSyntaxTree.ParseText(source, ParseOptions);

		return (tree, (CompilationUnitSyntax)tree.GetRoot());
	}

	/// <summary>
	/// Resolves the type of a field declared in the given source, by field name.
	/// </summary>
	/// <summary>
	/// Declares a single field inside <c>Sample.Holder&lt;T&gt;</c> and returns its resolved type symbol.
	/// </summary>
	/// <param name="fieldDeclaration">A field declaration, for example <c>public int[] Value;</c>.</param>
	/// <param name="fieldName">The name of the field to resolve.</param>
	public static ITypeSymbol FieldType(string fieldDeclaration, string fieldName = "Value")
	{
		var compilation = Create(Holder(fieldDeclaration));
		var containing = compilation.GetTypeByMetadataName("Sample.Holder`1")!;

		return ((IFieldSymbol)containing.GetMembers(fieldName).Single()).Type;
	}

	/// <summary>
	/// Wraps member declarations in <c>Sample.Holder&lt;T&gt;</c>, with nullable and unsafe enabled and a
	/// type parameter <c>T</c> in scope.
	/// </summary>
	public static string Holder(string members) =>
		$$"""
			#nullable enable
			using System;
			using System.Collections.Generic;

			namespace Sample;

			public unsafe class Holder<T>
			{
			{{members}}
			}
			""";
}
