using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.SourceGeneratorFramework.Benchmarks;

[MemoryDiagnoser]
public class TypeIdentityBenchmarks
{
	INamedTypeSymbol _int32Symbol;
	INamedTypeSymbol _stringSymbol;
	INamedTypeSymbol _listOfStringSymbol;
	INamedTypeSymbol _dictionarySymbol;
	INamedTypeSymbol _nestedGenericSymbol;

	[GlobalSetup]
	public void Setup()
	{
		var references = new MetadataReference[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location),
		};

		var compilation = CSharpCompilation.Create(
			"TypeIdentityBenchmarks",
			[],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		_int32Symbol = compilation.GetSpecialType(SpecialType.System_Int32);
		_stringSymbol = compilation.GetSpecialType(SpecialType.System_String);

		var list = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
		_listOfStringSymbol = list.Construct(_stringSymbol);

		var dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
		_dictionarySymbol = dictionary.Construct(_stringSymbol, _int32Symbol);

		var immutableArray = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1");
		_nestedGenericSymbol = immutableArray.Construct(list.Construct(_stringSymbol));
	}

	[Benchmark]
	public TypeIdentity Int32Identity() => new(_int32Symbol);

	[Benchmark]
	public TypeIdentity StringIdentity() => new(_stringSymbol);

	[Benchmark]
	public TypeIdentity ListOfStringIdentity() => new(_listOfStringSymbol);

	[Benchmark]
	public TypeIdentity DictionaryIdentity() => new(_dictionarySymbol);

	[Benchmark]
	public TypeIdentity NestedGenericIdentity() => new(_nestedGenericSymbol);
}
