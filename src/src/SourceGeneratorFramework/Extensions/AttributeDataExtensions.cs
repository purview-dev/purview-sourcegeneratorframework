using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Extensions;

/// <summary>
/// Provides extension methods for extracting values from <see cref="AttributeData"/>.
/// </summary>
public static class AttributeDataExtensions
{
	/// <summary>
	/// Gets the value of a named argument, returning the default if the argument is not present.
	/// </summary>
	public static T? GetNamedArgument<T>(
		this AttributeData attribute,
		string name,
		T? defaultValue = default
	)
	{
		if (attribute == null)
			throw new ArgumentNullException(nameof(attribute));
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException(
				"Argument name cannot be null or whitespace.",
				nameof(name)
			);

		// All valid...
		return TryGetNamedArgument(attribute, name, out T? value) ? value : defaultValue;
	}

	/// <summary>
	/// Tries to get the value of a named argument.
	/// </summary>
	public static bool TryGetNamedArgument<T>(
		this AttributeData attribute,
		string name,
		out T? value
	)
	{
		if (attribute == null)
			throw new ArgumentNullException(nameof(attribute));
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException(
				"Argument name cannot be null or whitespace.",
				nameof(name)
			);

		foreach (var namedArg in attribute.NamedArguments)
		{
			if (string.Equals(namedArg.Key, name, StringComparison.Ordinal))
			{
				value = As<T>(namedArg.Value);
				return true;
			}
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Gets the value of a constructor argument at the specified index, returning the default if out of range.
	/// </summary>
	public static T? GetConstructorArgument<T>(
		this AttributeData attribute,
		int index,
		T? defaultValue = default
	)
	{
		if (attribute == null)
			throw new ArgumentNullException(nameof(attribute));

		// All valid...
		return TryGetConstructorArgument(attribute, index, out T? value) ? value : defaultValue;
	}

	/// <summary>
	/// Tries to get the value of a constructor argument at the specified index.
	/// </summary>
	public static bool TryGetConstructorArgument<T>(
		this AttributeData attribute,
		int index,
		out T? value
	)
	{
		if (attribute == null)
			throw new ArgumentNullException(nameof(attribute));

		if (index < 0 || index >= attribute.ConstructorArguments.Length)
		{
			value = default;
			return false;
		}

		value = As<T>(attribute.ConstructorArguments[index]);
		return true;
	}

	/// <summary>
	/// Converts a <see cref="TypedConstant"/> to the specified type.
	/// </summary>
	public static T? As<T>(this TypedConstant constant)
	{
		if (constant.IsNull)
			return default;

		var targetType = typeof(T);
		if (targetType == typeof(TypedConstant))
			return (T?)(object)constant;

		if (targetType == typeof(ITypeSymbol) || targetType == typeof(ISymbol))
		{
			return constant.Kind == TypedConstantKind.Type && constant.Value is T typedValue
				? typedValue
				: default;
		}

		if (constant.Kind == TypedConstantKind.Array)
		{
			var values = constant.Values.Select(As<T>).ToArray();
			if (targetType.IsArray)
			{
				var elementType = targetType.GetElementType();
				var array = Array.CreateInstance(elementType, values.Length);
				for (var i = 0; i < values.Length; i++)
					array.SetValue(values[i], i);
				return (T?)(object?)array;
			}
			return (T?)(object?)values;
		}

		var value = constant.Value;
		if (value == null)
			return default;

		if (targetType.IsEnum)
		{
			return value is string stringValue
				? (T?)Enum.Parse(targetType, stringValue)
				: (T?)Enum.ToObject(targetType, value);
		}

		if (value is T t)
			return t;

		try
		{
			return (T?)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
		}
		catch
		{
			return default;
		}
	}
}
