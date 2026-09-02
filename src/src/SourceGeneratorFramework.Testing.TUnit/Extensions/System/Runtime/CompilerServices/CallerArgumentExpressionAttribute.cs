#if NETSTANDARD2_0

using System.ComponentModel;

// netstandard2.0 declares CallerArgumentExpressionAttribute as internal, so TUnit's generated
// assertion code cannot reference it when targeting netstandard2.0.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices;

#pragma warning restore IDE0130

[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
{
	public string ParameterName { get; } = parameterName;
}

#endif
