using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Extensions;

partial class AttributeDataExtensions
{
	extension(AttributeData attribute)
	{
		/// <summary>
		/// Gets the display name of an enum named argument, returning the default if the argument is not present or not an enum.
		/// </summary>
		public string? GetEnumNamedArgument(string name, string? defaultValue = null)
		{
			if (
				attribute.TryGetNamedArgument<TypedConstant>(name, out var value)
				&& !value.IsNull
				&& value.Kind == TypedConstantKind.Enum
			)
			{
				return value.ToEnumString();
			}

			// Return the default value if the named argument is not present or not an enum.
			return defaultValue;
		}

		/// <summary>
		/// Gets the display name of an enum constructor argument by parameter name, returning the default if the argument is not present or not an enum.
		/// </summary>
		public string? GetEnumConstructorArgument(string name, string? defaultValue = null)
		{
			if (
				attribute.TryGetConstructorArgument<TypedConstant>(name, out var value)
				&& !value.IsNull
				&& value.Kind == TypedConstantKind.Enum
			)
			{
				return value.ToEnumString();
			}

			// Return the default value if the constructor argument is not present or not an enum.
			return defaultValue;
		}

		/// <summary>
		/// Gets the display name of an enum constructor argument by index, returning the default if the argument is not present or not an enum.
		/// </summary>
		public string? GetEnumConstructorArgument(int index, string? defaultValue = null)
		{
			if (
				attribute.TryGetConstructorArgument<TypedConstant>(index, out var value)
				&& !value.IsNull
				&& value.Kind == TypedConstantKind.Enum
			)
			{
				return value.ToEnumString();
			}

			// Return the default value if the constructor argument is not present or not an enum.
			return defaultValue;
		}
	}

	extension(TypedConstant constant)
	{
		/// <summary>
		/// Converts the typed constant to a fully-qualified enum member display string (e.g. "Namespace.Type.Member").
		/// </summary>
		public string? ToEnumString()
		{
			if (constant.IsNull || constant.Kind != TypedConstantKind.Enum)
				return null;

			var type = constant.Type;
			if (type is null)
				return null;

			var typeName = TypeHelpers.ToFullyQualifiedDisplayString(type);
			var value = constant.Value;

			foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
			{
				if (field.HasConstantValue && field.ConstantValue?.Equals(value) == true)
					return $"{typeName}.{field.Name}";
			}

			return $"{typeName}.{Convert.ToString(value, CultureInfo.InvariantCulture)}";
		}
	}
}
