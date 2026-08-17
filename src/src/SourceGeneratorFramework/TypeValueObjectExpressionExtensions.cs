namespace Purview.SourceGeneratorFramework;

/// <summary>Provides expression-building helpers for structured type descriptors.</summary>
public static class TypeValueObjectExpressionExtensions
{
	/// <summary>Returns a fully qualified reference to a static member on the specified type.</summary>
	/// <param name="type">The type that declares the static member.</param>
	/// <param name="memberName">The static field, property, method, or nested-type name.</param>
	/// <returns>A C# expression in the form <c>global::Namespace.Type.Member</c>.</returns>
	public static string StaticMember(this TypeValueObject type, string memberName)
	{
		if (type == TypeValueObject.Empty)
			throw new ArgumentException("The declaring type cannot be empty.", nameof(type));
		if (string.IsNullOrWhiteSpace(memberName))
			throw new ArgumentException("Member name cannot be null or whitespace.", nameof(memberName));

		return $"{type.RenderFullName}.{memberName}";
	}
}
