using System.Diagnostics.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Provides common <see cref="TypeValueObject"/> instances for use by source generators.
/// </summary>
public static class PurviewTypeLibrary
{
	/// <summary>
	/// Common types from the <c>System</c> namespace.
	/// </summary>
	[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
	[SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
	public static class System
	{
		/// <summary>
		/// <see cref="Attribute"/>.
		/// </summary>
		public static readonly TypeValueObject Attribute = TypeValueObject.Create<Attribute>();

		/// <summary>
		/// <see cref="Type"/>.
		/// </summary>
		public static readonly TypeValueObject Type = TypeValueObject.Create<Type>();
	}

	/// <summary>
	/// Common types from the <c>Microsoft</c> namespace hierarchy.
	/// </summary>
	[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
	[SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
	public static class Microsoft
	{
		/// <summary>
		/// Common types from the <c>Microsoft.CodeAnalysis</c> namespace.
		/// </summary>
		[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
		[SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
		public static class CodeAnalysis
		{
			/// <summary>
			/// <see cref="EmbeddedAttribute"/>.
			/// </summary>
			public static readonly TypeValueObject EmbeddedAttribute =
				TypeValueObject.Create<global::Microsoft.CodeAnalysis.EmbeddedAttribute>();
		}
	}
}
