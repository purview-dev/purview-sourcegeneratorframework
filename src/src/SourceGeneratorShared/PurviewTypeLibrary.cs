using System.Diagnostics.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Provides common <see cref="TypeIdentity"/> instances for use by source generators.
/// </summary>
public static class PurviewTypeLibrary
{
	/// <summary>
	/// Common types from the <c>System</c> namespace.
	/// </summary>
	[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
	[SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
	[SuppressMessage("Naming", "CA1720:Identifier contains type name")]
	public static class System
	{
		/// <summary>
		/// <see cref="Attribute"/>.
		/// </summary>
		public static readonly TypeIdentity Attribute = TypeIdentity.Create<Attribute>();

		/// <summary>
		/// <see cref="Type"/>.
		/// </summary>
		public static readonly TypeIdentity Type = TypeIdentity.Create<Type>();

		/// <summary>
		/// <see cref="bool"/>.
		/// </summary>
		public static readonly TypeIdentity Boolean = TypeIdentity.Create<bool>();

		/// <summary>
		/// <see cref="byte"/>.
		/// </summary>
		public static readonly TypeIdentity Byte = TypeIdentity.Create<byte>();

		/// <summary>
		/// <see cref="sbyte"/>.
		/// </summary>
		public static readonly TypeIdentity SByte = TypeIdentity.Create<sbyte>();

		/// <summary>
		/// <see cref="char"/>.
		/// </summary>
		public static readonly TypeIdentity Char = TypeIdentity.Create<char>();

		/// <summary>
		/// <see cref="decimal"/>.
		/// </summary>
		public static readonly TypeIdentity Decimal = TypeIdentity.Create<decimal>();

		/// <summary>
		/// <see cref="double"/>.
		/// </summary>
		public static readonly TypeIdentity Double = TypeIdentity.Create<double>();

		/// <summary>
		/// <see cref="float"/>.
		/// </summary>
		public static readonly TypeIdentity Float = TypeIdentity.Create<float>();

		/// <summary>
		/// <see cref="int"/>.
		/// </summary>
		public static readonly TypeIdentity Int32 = TypeIdentity.Create<int>();

		/// <summary>
		/// <see cref="uint"/>.
		/// </summary>
		public static readonly TypeIdentity UInt32 = TypeIdentity.Create<uint>();

		/// <summary>
		/// <see cref="long"/>.
		/// </summary>
		public static readonly TypeIdentity Int64 = TypeIdentity.Create<long>();

		/// <summary>
		/// <see cref="ulong"/>.
		/// </summary>
		public static readonly TypeIdentity UInt64 = TypeIdentity.Create<ulong>();

		/// <summary>
		/// <see cref="short"/>.
		/// </summary>
		public static readonly TypeIdentity Int16 = TypeIdentity.Create<short>();

		/// <summary>
		/// <see cref="ushort"/>.
		/// </summary>
		public static readonly TypeIdentity UInt16 = TypeIdentity.Create<ushort>();

		/// <summary>
		/// <see cref="string"/>.
		/// </summary>
		public static readonly TypeIdentity String = TypeIdentity.Create<string>();

		/// <summary>
		/// <see cref="object"/>.
		/// </summary>
		public static readonly TypeIdentity Object = TypeIdentity.Create<object>();

		/// <summary>
		/// <see cref="void"/>.
		/// </summary>
		public static readonly TypeIdentity Void = new("void", null);

		/// <summary>
		/// <see cref="nint"/>.
		/// </summary>
		public static readonly TypeIdentity IntPtr = TypeIdentity.Create<nint>();

		/// <summary>
		/// <see cref="nuint"/>.
		/// </summary>
		public static readonly TypeIdentity UIntPtr = TypeIdentity.Create<nuint>();

		/// <summary>
		/// <see cref="global::System.Action"/>.
		/// </summary>
		public static readonly TypeIdentity Action = TypeIdentity.Create<Action>();

		/// <summary>
		/// <see cref="Func{TResult}"/>.
		/// </summary>
		public static readonly TypeIdentity Func = new(nameof(Func), "System");
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
			public static readonly TypeIdentity EmbeddedAttribute = new(
				nameof(EmbeddedAttribute),
				"Microsoft.CodeAnalysis"
			);
		}
	}
}
