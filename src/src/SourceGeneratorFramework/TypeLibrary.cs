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
	[SuppressMessage("Naming", "CA1720:Identifier contains type name")]
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

		/// <summary>
		/// <see cref="bool"/>.
		/// </summary>
		public static readonly TypeValueObject Boolean = TypeValueObject.Create<bool>();

		/// <summary>
		/// <see cref="byte"/>.
		/// </summary>
		public static readonly TypeValueObject Byte = TypeValueObject.Create<byte>();

		/// <summary>
		/// <see cref="sbyte"/>.
		/// </summary>
		public static readonly TypeValueObject SByte = TypeValueObject.Create<sbyte>();

		/// <summary>
		/// <see cref="char"/>.
		/// </summary>
		public static readonly TypeValueObject Char = TypeValueObject.Create<char>();

		/// <summary>
		/// <see cref="decimal"/>.
		/// </summary>
		public static readonly TypeValueObject Decimal = TypeValueObject.Create<decimal>();

		/// <summary>
		/// <see cref="double"/>.
		/// </summary>
		public static readonly TypeValueObject Double = TypeValueObject.Create<double>();

		/// <summary>
		/// <see cref="float"/>.
		/// </summary>
		public static readonly TypeValueObject Float = TypeValueObject.Create<float>();

		/// <summary>
		/// <see cref="int"/>.
		/// </summary>
		public static readonly TypeValueObject Int32 = TypeValueObject.Create<int>();

		/// <summary>
		/// <see cref="uint"/>.
		/// </summary>
		public static readonly TypeValueObject UInt32 = TypeValueObject.Create<uint>();

		/// <summary>
		/// <see cref="long"/>.
		/// </summary>
		public static readonly TypeValueObject Int64 = TypeValueObject.Create<long>();

		/// <summary>
		/// <see cref="ulong"/>.
		/// </summary>
		public static readonly TypeValueObject UInt64 = TypeValueObject.Create<ulong>();

		/// <summary>
		/// <see cref="short"/>.
		/// </summary>
		public static readonly TypeValueObject Int16 = TypeValueObject.Create<short>();

		/// <summary>
		/// <see cref="ushort"/>.
		/// </summary>
		public static readonly TypeValueObject UInt16 = TypeValueObject.Create<ushort>();

		/// <summary>
		/// <see cref="string"/>.
		/// </summary>
		public static readonly TypeValueObject String = TypeValueObject.Create<string>();

		/// <summary>
		/// <see cref="object"/>.
		/// </summary>
		public static readonly TypeValueObject Object = TypeValueObject.Create<object>();

		/// <summary>
		/// <see cref="void"/>.
		/// </summary>
		public static readonly TypeValueObject Void = new("void", null);

		/// <summary>
		/// <see cref="nint"/>.
		/// </summary>
		public static readonly TypeValueObject IntPtr = TypeValueObject.Create<nint>();

		/// <summary>
		/// <see cref="nuint"/>.
		/// </summary>
		public static readonly TypeValueObject UIntPtr = TypeValueObject.Create<nuint>();

		/// <summary>
		/// <see cref="global::System.Action"/>.
		/// </summary>
		public static readonly TypeValueObject Action = TypeValueObject.Create<Action>();

		/// <summary>
		/// <see cref="Func{TResult}"/>.
		/// </summary>
		public static readonly TypeValueObject Func = new(nameof(Func), "System");
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
