using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework;

partial class TypeValueObjectTests
{
	public static IEnumerable<Func<SymbolTestDataInfo>> SymbolTestData
	{
		get
		{
			yield return () =>
				CreateSymbolTestData(new("Testing.Testing", true), "TestingClass", TypeDeclarationKind.Class);
			yield return () =>
				CreateSymbolTestData(new("Testing.Testing", false), "TestingClass", TypeDeclarationKind.Class);
			yield return () =>
				CreateSymbolTestData(NamespaceInfo.GlobalNamespace, "TestingClass", TypeDeclarationKind.Class);
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1859:Use concrete types when possible for improved performance"
	)]
	static SymbolTestDataInfo CreateSymbolTestData(
		NamespaceInfo namespaceInfo,
		string typeName,
		TypeDeclarationKind declarationKind = TypeDeclarationKind.Class,
		TypeDeclarationAccessibility accessibility = TypeDeclarationAccessibility.Public
	)
	{
		var writer = CodeWriterFactory.ForTests();

		IDisposable? blockNamespace = null;
		if (namespaceInfo.HasNamespace)
		{
			if (namespaceInfo.IsFileScoped)
				writer.WriteFileScopedNamespace(namespaceInfo.Namespace);
			else
				blockNamespace = writer.WriteBlockNamespaceScope(namespaceInfo.Namespace);
		}

		using (writer.WriteTypeScope(new TypeDeclarationOptions(typeName, accessibility) { Kind = declarationKind }))
		{
			writer.WriteLine("public void DoAThing() {}");
		}

		blockNamespace?.Dispose();

		SourceGeneratorTestOptions options = new();

		var source = writer.ToString();
		var syntax = CSharpSyntaxTree.ParseText(
			source,
			encoding: System.Text.Encoding.UTF8,
			options: new CSharpParseOptions(LanguageVersion.Preview),
			cancellationToken: TestContext.Current?.Execution.CancellationToken ?? default
		);

		var references = SourceGeneratorHelpers.ResolveReferences(options, typeof(TypeValueObjectTests).Assembly);
		var compilation = SourceGeneratorHelpers.CreateCompilation([syntax], references, options);

		//var model = compilation.GetSemanticModel(syntax);
		var fullTypeName = namespaceInfo.HasNamespace ? $"{namespaceInfo.Namespace}.{typeName}" : typeName;
		var symbol = compilation.GetTypeByMetadataName(fullTypeName) as ITypeSymbol;

		ArgumentNullException.ThrowIfNull(symbol);

		return new(fullTypeName, typeName, namespaceInfo, symbol);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
	public sealed record class SymbolTestDataInfo(
		string FullTypeName,
		string TypeName,
		NamespaceInfo NamespaceInfo,
		ITypeSymbol Symbol
	);

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
	public readonly record struct NamespaceInfo(string? Namespace, bool IsFileScoped)
	{
		public bool HasNamespace => !string.IsNullOrWhiteSpace(Namespace);

		public static NamespaceInfo GlobalNamespace => new(null, false);
	}
}
