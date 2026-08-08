using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Extensions;

/// <summary>
/// Provides extension methods for extracting values from <see cref="AttributeData"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Usage",
	"CA2208:Instantiate argument exceptions correctly"
)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Naming",
	"CA1708:Identifiers should differ by more than case"
)]
public static partial class AttributeDataExtensions
{
	extension(AttributeData attribute)
	{
		/// <summary>
		/// Gets the value of a named argument, returning the default if the argument is not present.
		/// </summary>
		public T? GetNamedArgument<T>(string name, T? defaultValue = default)
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
		public bool TryGetNamedArgument<T>(string name, out T? value)
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
		/// Tries to get a constructor argument by its declared parameter name.
		/// </summary>
		public bool TryGetConstructorArgument<T>(string parameterName, out T? value)
		{
			if (string.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentException(
					"Parameter name cannot be null or whitespace.",
					nameof(parameterName)
				);
			}

			var constructor = attribute.AttributeConstructor;
			if (constructor is null)
			{
				value = default;
				return false;
			}

			for (var index = 0; index < constructor.Parameters.Length; index++)
			{
				if (
					string.Equals(
						constructor.Parameters[index].Name,
						parameterName,
						StringComparison.Ordinal
					)
				)
				{
					return attribute.TryGetConstructorArgument(index, out value);
				}
			}

			value = default;
			return false;
		}

		/// <summary>
		/// Gets the value of a constructor argument at the specified index, returning the default if out of range.
		/// </summary>
		public T? GetConstructorArgument<T>(int index, T? defaultValue = default)
		{
			if (attribute == null)
				throw new ArgumentNullException(nameof(attribute));

			// All valid...
			return TryGetConstructorArgument(attribute, index, out T? value) ? value : defaultValue;
		}

		/// <summary>
		/// Tries to get the value of a constructor argument at the specified index.
		/// </summary>
		public bool TryGetConstructorArgument<T>(int index, out T? value)
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
	}
}
