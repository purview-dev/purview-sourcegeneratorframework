using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.SourceGeneratorFramework;

public class CodeQueryTests
{
	const string Source = """
		namespace Test;

		public class ComplexType { }

		public sealed class Sample
		{
			public const string Constant = "value";
			public int Count { get; set; }

			public void DoWork(int value, int? optional, ComplexType complex) { }
			public string Name { get; set; } = "";
			public int Compute(int left, int right) => left + right;
			public string Format(string format, object? value) => "";
		}

		public interface IContract { }
		public enum Level { None, Low, High }
		public delegate void Handler(int value);
		public record Person(string Name);
		""";

	static CodeQuery CreateQuery()
	{
		var (compilation, _) = TestCompilation.CreateWithRoot(Source);

		return new([.. compilation.SyntaxTrees], compilation);
	}

	[Test]
	public async Task GetMethod_FindsMethodByName()
	{
		var query = CreateQuery();

		var method = query.GetMethod("DoWork");

		await Assert.That(method.Identifier.ValueText).IsEqualTo("DoWork");
	}

	[Test]
	public async Task HasMethod_GivenPresentAndAbsent_ReturnsTrueFalse()
	{
		var query = CreateQuery();

		await Assert.That(query.HasMethod("DoWork")).IsTrue();
		await Assert.That(query.HasMethod("Missing")).IsFalse();
	}

	[Test]
	public async Task TryGetMethod_GivenPresent_ReturnsTrueAndNode()
	{
		var query = CreateQuery();

		await Assert.That(query.TryGetMethod("Compute", out var method)).IsTrue();
		await Assert.That(method).IsNotNull();
	}

	[Test]
	public async Task GetMethod_GivenAbsent_ThrowsSyntaxNotFoundException()
	{
		var query = CreateQuery();

		await Assert
			.That(() => query.GetMethod("Missing"))
			.Throws<SyntaxNotFoundException>()
			.WithMessageContaining("Missing", StringComparison.Ordinal);
	}

	[Test]
	public async Task GetMethod_WithParameterTypes_MatchesSignature()
	{
		var query = CreateQuery();

		var intType = TypeReference.Create<int>();
		var nullableInt = TypeReference.Create<int>().Nullable();
		var complexType = new TypeReference(new TypeIdentity("ComplexType", "Test"));

		var method = query.GetMethod("DoWork", intType, nullableInt, complexType);
		await Assert.That(method.Identifier.ValueText).IsEqualTo("DoWork");
	}

	[Test]
	public async Task HasMethod_WithParameterTypes_EnforcesNullableValueTypes()
	{
		var query = CreateQuery();

		var intType = TypeReference.Create<int>();
		var nullableInt = TypeReference.Create<int>().Nullable();

		// int? parameter must not match a plain int reference and vice versa.
		await Assert.That(query.HasMethod("DoWork", intType, intType, intType)).IsFalse();
		await Assert.That(query.HasMethod("DoWork", nullableInt, nullableInt, nullableInt)).IsFalse();
	}

	[Test]
	public async Task GetMethod_WithReturnType_Matches()
	{
		var query = CreateQuery();

		await Assert.That(query.HasReturnType("Compute", TypeReference.Create<int>())).IsTrue();
		await Assert.That(query.HasReturnType("Compute", TypeReference.Create<string>())).IsFalse();
	}

	[Test]
	public async Task HasParameters_OnNode_MatchesSignature()
	{
		var query = CreateQuery();
		var method = query.GetMethod("Format");
		var stringType = TypeReference.Create<string>();
		var objectType = TypeReference.Create<object>().Nullable();

		await Assert.That(method.HasParameters(query, stringType, objectType)).IsTrue();
		await Assert.That(method.HasParameters(query, stringType)).IsFalse();
	}

	[Test]
	public async Task GetClass_GetStruct_GetInterface_GetEnum_GetDelegate_GetRecord_FindDeclarations()
	{
		var query = CreateQuery();

		await Assert.That(query.GetClass("Sample").Identifier.ValueText).IsEqualTo("Sample");
		await Assert.That(query.GetInterface("IContract").Identifier.ValueText).IsEqualTo("IContract");
		await Assert.That(query.GetEnum("Level").Identifier.ValueText).IsEqualTo("Level");
		await Assert.That(query.GetDelegate("Handler").Identifier.ValueText).IsEqualTo("Handler");
		await Assert.That(query.GetRecord("Person").Identifier.ValueText).IsEqualTo("Person");
		await Assert.That(query.HasClass("Missing")).IsFalse();
		await Assert.That(query.HasInterface("IContract")).IsTrue();
	}

	[Test]
	public async Task GetProperty_GetField_FindMembers()
	{
		var query = CreateQuery();

		await Assert.That(query.GetProperty("Count").Identifier.ValueText).IsEqualTo("Count");
		await Assert.That(query.HasProperty("Name")).IsTrue();
		await Assert
			.That(query.GetField("Constant").Declaration.Variables[0].Identifier.ValueText)
			.IsEqualTo("Constant");
		await Assert.That(query.HasField("Constant")).IsTrue();
		await Assert.That(query.HasField("Missing")).IsFalse();
	}

	[Test]
	public async Task GetTypeDeclaration_MatchesAnyDeclarationKind()
	{
		var query = CreateQuery();

		await Assert.That(query.HasTypeDeclaration("Sample")).IsTrue();
		await Assert.That(query.HasTypeDeclaration("IContract")).IsTrue();
		await Assert.That(query.HasTypeDeclaration("Person")).IsTrue();
		await Assert.That(query.HasTypeDeclaration("Missing")).IsFalse();
	}

	[Test]
	public async Task GetNamespace_FindsDottedNamespace()
	{
		var query = CreateQuery();

		await Assert.That(query.HasNamespace("Test")).IsTrue();
		await Assert.That(query.HasNamespace("Other")).IsFalse();
	}

	[Test]
	public async Task GenericGet_And_Has_FindSyntaxByPredicate()
	{
		var query = CreateQuery();

		await Assert
			.That(query.Has<MethodDeclarationSyntax>(method => method.Identifier.ValueText == "DoWork"))
			.IsTrue();
		await Assert
			.That(query.Has<MethodDeclarationSyntax>(method => method.Identifier.ValueText == "Missing"))
			.IsFalse();
		await Assert
			.That(query.Get<MethodDeclarationSyntax>(method => method.Identifier.ValueText == "Compute") is not null)
			.IsTrue();
	}

	[Test]
	public async Task TryGetSyntaxTree_MatchesBySuffix()
	{
		var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(Source, path: "Generated/File.g.cs");
		var query = new CodeQuery([tree]);

		await Assert.That(query.HasSyntaxTree("File.g.cs")).IsTrue();
		await Assert.That(query.HasSyntaxTree("Other.g.cs")).IsFalse();
		await Assert.That(query.TryGetSyntaxTree("File.g.cs", out var found)).IsTrue();
		await Assert.That(ReferenceEquals(found, tree)).IsTrue();
	}

	[Test]
	public async Task Get_WhenNothingMatches_ThrowsSyntaxNotFoundException()
	{
		var query = CreateQuery();

		await Assert
			.That(() => query.Get<MethodDeclarationSyntax>(static method => method.Identifier.ValueText == "Nope"))
			.Throws<SyntaxNotFoundException>();
	}

	[Test]
	public async Task NestedQuery_GivenRoot_ScopesNodeSearches()
	{
		var query = CreateQuery();
		var method = query.GetMethod("DoWork");

		var nested = query.In(method);

		await Assert.That(nested.Has<ParameterSyntax>()).IsTrue();
		await Assert.That(nested.HasMethod("Compute")).IsFalse();
		await Assert.That(nested.HasProperty("Count")).IsFalse();
	}

	[Test]
	public async Task NestedQuery_ChainingExtension_ScopesSearches()
	{
		var query = CreateQuery();

		var nested = query.GetClass("Sample").Query(query);

		await Assert.That(nested.HasProperty("Count")).IsTrue();
		await Assert.That(nested.HasField("Constant")).IsTrue();
		await Assert.That(nested.HasMethod("DoWork")).IsTrue();
		await Assert.That(nested.HasClass("Sample")).IsFalse();
	}

	[Test]
	public async Task GetAllAndCount_ReturnAllMatches()
	{
		var query = CreateQuery();

		await Assert.That(query.Count<PropertyDeclarationSyntax>()).IsEqualTo(2);
		await Assert.That(query.GetAll<MethodDeclarationSyntax>().Length).IsEqualTo(3);
		await Assert
			.That(
				query
					.GetAll<MethodDeclarationSyntax>(static method => method.Identifier.ValueText.StartsWith('C'))
					.Length
			)
			.IsEqualTo(1);
	}

	[Test]
	public async Task OperatorAndIndexerAndAttributeQueries_FindDeclarations()
	{
		// Arrange
		const string source = """
			using System;

			namespace Test;

			public struct Money
			{
				[Obsolete]
				public string this[int index] => "";

				public static bool operator ==(Money left, Money right) => true;
				public static bool operator !=(Money left, Money right) => false;
				public static implicit operator int(Money value) => 0;
			}
			""";
		var (compilation, _) = TestCompilation.CreateWithRoot(source);
		var query = new CodeQuery([.. compilation.SyntaxTrees], compilation);

		// Act / Assert
		await Assert.That(query.HasOperator("==")).IsTrue();
		await Assert.That(query.HasOperator("!=")).IsTrue();
		await Assert.That(query.HasConversionOperator("implicit")).IsTrue();
		await Assert.That(query.HasOperator("+")).IsFalse();
		await Assert.That(query.GetOperator("==").ParameterList.Parameters.Count).IsEqualTo(2);

		await Assert.That(query.HasIndexer(TypeReference.Create<int>())).IsTrue();
		await Assert.That(query.HasIndexer(TypeReference.Create<string>())).IsFalse();

		var money = query.GetStruct("Money");
		await Assert.That(query.HasAttribute(money, "Obsolete")).IsTrue();
		await Assert.That(query.GetAttribute(money, "Obsolete")).IsNotNull();
		await Assert.That(query.HasAttribute(money, "Missing")).IsFalse();
	}

	[Test]
	public async Task StatementQueries_FindStatementsAndInvocations()
	{
		// Arrange
		const string source = """
			using System;
			using System.Collections.Generic;

			namespace Test;

			public class Worker
			{
				public void Run(List<string> items)
				{
					var total = 0;
					try
					{
						foreach (var item in items)
						{
							total += int.Parse(item);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
					}
					if (total > 0)
					{
						Process(total);
					}
				}

				void Process(int value) { }
			}
			""";
		var (compilation, _) = TestCompilation.CreateWithRoot(source);
		var query = new CodeQuery([.. compilation.SyntaxTrees], compilation);

		// Act / Assert
		await Assert.That(query.HasTry()).IsTrue();
		await Assert.That(query.HasForeach()).IsTrue();
		await Assert.That(query.HasIf()).IsTrue();
		await Assert.That(query.HasWhile("true")).IsFalse();
		await Assert.That(query.HasInvocation("Parse")).IsTrue();
		await Assert.That(query.HasInvocation("Process")).IsTrue();
		await Assert.That(query.HasInvocation("Missing")).IsFalse();
	}
}
