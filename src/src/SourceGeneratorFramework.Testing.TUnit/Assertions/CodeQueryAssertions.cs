using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace Purview.SourceGeneratorFramework.Testing.TUnit.Assertions;

/// <summary>
/// TUnit assertion extensions that query the code produced by a test run and return the matched syntax node.
/// </summary>
public static partial class CodeQueryAssertions
{
	// ---------------------------------------------------------------------------------------------
	// Generated code (source generators)
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Asserts that the generated code contains a method with the given name, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<MethodDeclarationSyntax> HasGeneratedMethod(
		this DriverRunResult result,
		string methodName
	) => GetMethod(result?.Generated(), methodName, null, "generated code");

	/// <summary>
	/// Asserts that the generated code contains a method with the given name and parameter types, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<MethodDeclarationSyntax> HasGeneratedMethod(
		this DriverRunResult result,
		string methodName,
		TypeReference[] parameters
	) => GetMethod(result?.Generated(), methodName, parameters, "generated code");

	/// <summary>
	/// Asserts that the generated code contains a method with the given name and return type, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<MethodDeclarationSyntax> HasGeneratedMethodReturnType(
		this DriverRunResult result,
		string methodName,
		TypeReference returnType
	)
	{
		var query = result?.Generated();
		if (query is null)
			return (AssertionResult<MethodDeclarationSyntax>)AssertionResult.Failed("expected DriverRunResult is null");
		if (string.IsNullOrWhiteSpace(methodName))
			return (AssertionResult<MethodDeclarationSyntax>)
				AssertionResult.Failed("method name cannot be null or whitespace");

		if (query.TryGetMethod(methodName, out var method) && query.HasReturnType(methodName, returnType))
			return AssertionResult<MethodDeclarationSyntax>.Passed(method!);

		// If the method exists but has a different return type, we could provide more detail in the failure message.
		return (AssertionResult<MethodDeclarationSyntax>)
			AssertionResult.Failed(
				$"generated code did not contain a method named '{methodName}' with the expected return type"
			);
	}

	/// <summary>
	/// Asserts that the generated code contains a class with the given name, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<ClassDeclarationSyntax> HasGeneratedClass(
		this DriverRunResult result,
		string className
	)
	{
		var query = result?.Generated();
		if (query is null)
			return (AssertionResult<ClassDeclarationSyntax>)AssertionResult.Failed("expected DriverRunResult is null");
		if (string.IsNullOrWhiteSpace(className))
			return (AssertionResult<ClassDeclarationSyntax>)
				AssertionResult.Failed("class name cannot be null or whitespace");

		if (query.TryGetClass(className, out var declaration))
			return AssertionResult<ClassDeclarationSyntax>.Passed(declaration!);

		// If the class exists but has a different type, we could provide more detail in the failure message.
		return (AssertionResult<ClassDeclarationSyntax>)
			AssertionResult.Failed($"generated code did not contain a class named '{className}'");
	}

	/// <summary>
	/// Asserts that the generated code contains a property with the given name, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<PropertyDeclarationSyntax> HasGeneratedProperty(
		this DriverRunResult result,
		string propertyName
	)
	{
		var query = result?.Generated();
		if (query is null)
			return (AssertionResult<PropertyDeclarationSyntax>)
				AssertionResult.Failed("expected DriverRunResult is null");
		if (string.IsNullOrWhiteSpace(propertyName))
			return (AssertionResult<PropertyDeclarationSyntax>)
				AssertionResult.Failed("property name cannot be null or whitespace");

		if (query.TryGetProperty(propertyName, out var declaration))
			return AssertionResult<PropertyDeclarationSyntax>.Passed(declaration!);

		// If the property exists but has a different type, we could provide more detail in the failure message.
		return (AssertionResult<PropertyDeclarationSyntax>)
			AssertionResult.Failed($"generated code did not contain a property named '{propertyName}'");
	}

	/// <summary>
	/// Asserts that the generated code contains a field with the given name, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<FieldDeclarationSyntax> HasGeneratedField(
		this DriverRunResult result,
		string fieldName
	)
	{
		var query = result?.Generated();
		if (query is null)
			return (AssertionResult<FieldDeclarationSyntax>)AssertionResult.Failed("expected DriverRunResult is null");
		if (string.IsNullOrWhiteSpace(fieldName))
			return (AssertionResult<FieldDeclarationSyntax>)
				AssertionResult.Failed("field name cannot be null or whitespace");

		if (query.TryGetField(fieldName, out var declaration))
			return AssertionResult<FieldDeclarationSyntax>.Passed(declaration!);

		// If the field exists but has a different type, we could provide more detail in the failure message.
		return (AssertionResult<FieldDeclarationSyntax>)
			AssertionResult.Failed($"generated code did not contain a field named '{fieldName}'");
	}

	/// <summary>
	/// Asserts that the generated code contains a syntax tree with the given name, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<SyntaxTree> HasGeneratedSyntaxTree(this DriverRunResult result, string treeName)
	{
		var query = result?.Generated();
		if (query is null)
			return (AssertionResult<SyntaxTree>)AssertionResult.Failed("expected DriverRunResult is null");
		if (string.IsNullOrWhiteSpace(treeName))
			return (AssertionResult<SyntaxTree>)AssertionResult.Failed("tree name cannot be null or whitespace");

		if (query.TryGetSyntaxTree(treeName, out var tree))
			return AssertionResult<SyntaxTree>.Passed(tree!);

		// If the syntax tree exists but has a different name, we could provide more detail in the failure message.
		return (AssertionResult<SyntaxTree>)
			AssertionResult.Failed($"generated code did not contain a syntax tree named '{treeName}'");
	}

	// ---------------------------------------------------------------------------------------------
	// Fixed code (code fixes and refactorings)
	// ---------------------------------------------------------------------------------------------

	/// <summary>
	/// Asserts that the fixed code contains a method with the given name, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<MethodDeclarationSyntax> HasFixedMethod(
		this CodeFixTestResult result,
		string methodName
	) => GetMethod(result?.FixedCode(), methodName, null, "fixed code");

	/// <summary>
	/// Asserts that the fixed code contains a method with the given name and parameter types, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<MethodDeclarationSyntax> HasFixedMethod(
		this CodeFixTestResult result,
		string methodName,
		TypeReference[] parameters
	) => GetMethod(result?.FixedCode(), methodName, parameters, "fixed code");

	/// <summary>
	/// Asserts that the fixed code contains a method with the given name, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<MethodDeclarationSyntax> HasFixedMethod(
		this CodeFixFixAllResult result,
		string methodName
	) => GetMethod(result?.FixedCode(), methodName, null, "fixed code");

	/// <summary>
	/// Asserts that the fixed code contains a method with the given name, returning it.
	/// </summary>
	[GenerateAssertion]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static AssertionResult<MethodDeclarationSyntax> HasFixedMethod(
		this RefactorTestResult result,
		string methodName
	) => GetMethod(result?.FixedCode(), methodName, null, "refactored code");

	// ---------------------------------------------------------------------------------------------
	// Shared
	// ---------------------------------------------------------------------------------------------

	static AssertionResult<MethodDeclarationSyntax> GetMethod(
		CodeQuery? query,
		string methodName,
		TypeReference[]? parameters,
		string scope
	)
	{
		if (query is null)
			return (AssertionResult<MethodDeclarationSyntax>)AssertionResult.Failed("expected test result is null");
		if (string.IsNullOrWhiteSpace(methodName))
			return (AssertionResult<MethodDeclarationSyntax>)
				AssertionResult.Failed("method name cannot be null or whitespace");

		if (query.TryGetMethod(methodName, out var method, parameters))
			return AssertionResult<MethodDeclarationSyntax>.Passed(method!);

		// If the method exists but has different parameters, we could provide more detail in the failure message.
		return (AssertionResult<MethodDeclarationSyntax>)
			AssertionResult.Failed($"{scope} did not contain a method named '{methodName}'");
	}
}
