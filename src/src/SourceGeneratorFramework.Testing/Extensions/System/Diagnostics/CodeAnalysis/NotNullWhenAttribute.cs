#if NETSTANDARD2_0

using System.ComponentModel;

// netstandard2.0 declares NotNullWhenAttribute as internal, which makes it unusable as a
// public/override attribute; a public definition is required when targeting netstandard2.0.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Diagnostics.CodeAnalysis;

#pragma warning restore IDE0130

[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public sealed class NotNullWhenAttribute(bool returnValue) : Attribute
{
	public bool ReturnValue { get; } = returnValue;
}

#endif
