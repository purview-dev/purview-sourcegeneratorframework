using System.Collections.Immutable;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Describes one invocation in a <see cref="MethodChainBuilder"/>. The receiver of every segment is
/// implicitly the result of the preceding invocation.
/// </summary>
/// <param name="MethodName">The method name, without a leading receiver.</param>
/// <param name="Arguments">The argument expressions.</param>
/// <param name="GenericArguments">The generic type arguments, or empty for none.</param>
readonly record struct MethodChainSegment(
	string MethodName,
	ImmutableArray<string?> Arguments,
	ImmutableArray<TypeReference> GenericArguments
);

/// <summary>
/// Accumulates the invocations of a chained method-call expression, where the result of each call is
/// the receiver of the next. Configure it through the <c>configure</c> callback of
/// <see cref="CodeWriter.MethodCallChain"/> and <see cref="CodeWriter.AwaitedMethodCallChain"/>.
/// </summary>
/// <example>
/// <code>writer.Assignment("var value", value =&gt; value.MethodCallChain(
/// 	"builder.Configuration.GetSection", [$"{name}.SectionName"],
/// 	chain =&gt; chain.Method("Get", genericArguments: [optionsType]).Postfix("?? new()")));</code>
/// </example>
public sealed class MethodChainBuilder
{
	internal string RootMethod { get; }
	internal ImmutableArray<string?> RootArguments { get; }
	internal ImmutableArray<TypeReference> RootGenericArguments { get; }
	internal List<MethodChainSegment> Segments { get; } = [];
	internal string? PostfixExpression { get; private set; }

	internal MethodChainBuilder(
		string rootMethod,
		IEnumerable<string?>? rootArguments,
		IEnumerable<TypeReference>? rootGenericArguments
	)
	{
		RootMethod = rootMethod;
		RootArguments = rootArguments is null ? [] : [.. rootArguments];
		RootGenericArguments = rootGenericArguments is null ? [] : [.. rootGenericArguments];
	}

	/// <summary>
	/// Appends an invocation to the chain. The receiver is implicitly the result of the previous call.
	/// </summary>
	/// <param name="methodName">The method name, without a receiver.</param>
	/// <param name="arguments">The argument expressions, or <see langword="null"/> for a no-argument call.</param>
	/// <param name="genericArguments">The generic type arguments, or <see langword="null"/> for none.</param>
	/// <returns>The current builder.</returns>
	/// <example><code>chain.Method("Get", genericArguments: [optionsType])</code></example>
	public MethodChainBuilder Method(
		string methodName,
		IEnumerable<string?>? arguments = null,
		IEnumerable<TypeReference>? genericArguments = null
	)
	{
		if (string.IsNullOrWhiteSpace(methodName))
			throw new ArgumentException("Method name cannot be null or whitespace.", nameof(methodName));

		var argumentList = arguments?.ToArray() ?? [];
		for (var index = 0; index < argumentList.Length; index++)
		{
			if (string.IsNullOrWhiteSpace(argumentList[index]))
				throw new ArgumentException("Argument values cannot be null or whitespace.", nameof(arguments));
		}

		Segments.Add(new(methodName, [.. argumentList], genericArguments is null ? [] : [.. genericArguments]));
		return this;
	}

	/// <summary>
	/// Appends a trailing expression to the result of the last invocation, such as <c>?? new()</c> or <c>!</c>.
	/// The value is written verbatim, so include any leading separator required, e.g. <c>" ?? new()"</c>.
	/// </summary>
	/// <param name="expression">The postfix expression, without a trailing semicolon.</param>
	/// <returns>The current builder.</returns>
	/// <example><code>chain.Postfix(" ?? new()")</code></example>
	public MethodChainBuilder Postfix(string expression)
	{
		if (string.IsNullOrWhiteSpace(expression))
			throw new ArgumentException("Postfix expression cannot be null or whitespace.", nameof(expression));

		PostfixExpression = expression;
		return this;
	}
}
