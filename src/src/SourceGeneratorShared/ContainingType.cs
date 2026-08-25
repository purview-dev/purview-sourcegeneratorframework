namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Represents a single link in a nested type's containing-type chain.
/// </summary>
/// <remarks>
/// Deliberately minimal: only the name and the containing type's own generic arity are needed to identify
/// and render a link in the chain, or to match it against a declaration or symbol. Unlike
/// <see cref="TypeIdentity"/> it carries no namespace, generic arguments or further nesting, so building
/// a chain never recurses into argument resolution.
/// </remarks>
public readonly record struct ContainingType(string Name, int GenericArity)
{
	/// <summary>
	/// Gets the CLR metadata name, including the generic arity suffix when required.
	/// </summary>
	public string MetadataName => GenericArity == 0 ? Name : $"{Name}`{GenericArity}";

	/// <summary>
	/// Gets the type name suitable for use in generated code, using open placeholders for any generic
	/// parameters since a containing link does not track the constructed argument shapes.
	/// </summary>
	public string RenderTypeName => GenericArity == 0 ? Name : $"{Name}<{new string(',', GenericArity - 1)}>";
}
