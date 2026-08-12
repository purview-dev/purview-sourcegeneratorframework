using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Purview.SourceGeneratorFramework.Testing.Generators.Model;

static class AttributeDataModelLibrary
{
	public static IncrementalValuesProvider<GeneratorResult<AttributeDataModelTarget>> GetTargets(
		IncrementalGeneratorInitializationContext context
	)
	{
		return IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.GenerateAttributeDataModelAttribute,
			static (ctx, ct) =>
			{
				var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.TargetNode, ct);
				if (symbol is not INamedTypeSymbol { TypeKind: TypeKind.Struct } structSymbol)
					return GeneratorResult<AttributeDataModelTarget>.Empty;

				return BuildTarget(structSymbol, ct);
			}
		);
	}

	static GeneratorResult<AttributeDataModelTarget> BuildTarget(
		INamedTypeSymbol structSymbol,
		CancellationToken cancellationToken
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
		var generateAttribute = GetAttribute(
			structSymbol,
			TypeLibrary.GenerateAttributeDataModelAttribute
		);
		if (generateAttribute is null)
			return GeneratorResult<AttributeDataModelTarget>.Empty;

		if (generateAttribute.ConstructorArguments.Length == 0)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					AttributeDataModelDiagnosticDescriptors.TargetAttributeNotResolved,
					structSymbol.Locations.FirstOrDefault(static loc => loc.IsInSource),
					structSymbol.Name
				)
			);

			return GeneratorResult<AttributeDataModelTarget>.Fail([.. diagnostics]);
		}

		var firstArgument = generateAttribute.ConstructorArguments[0].Value;
		ITypeSymbol? targetAttributeType = null;
		TypeValueObject targetAttribute;

		if (firstArgument is ITypeSymbol typeSymbol)
		{
			targetAttributeType = typeSymbol;
			targetAttribute = new TypeValueObject(typeSymbol);
		}
		else if (firstArgument is string targetAttributeName)
		{
			targetAttribute = ParseTypeValueObject(targetAttributeName);
		}
		else
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					AttributeDataModelDiagnosticDescriptors.TargetAttributeNotResolved,
					structSymbol.Locations.FirstOrDefault(static loc => loc.IsInSource),
					structSymbol.Name
				)
			);

			return GeneratorResult<AttributeDataModelTarget>.Fail([.. diagnostics]);
		}

		var matchByInheritance = GetNamedArgument(generateAttribute, "MatchByInheritance", false);
		var autoDiscover = GetNamedArgument(generateAttribute, "AutoDiscover", false);

		if (autoDiscover && targetAttributeType is null)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					AttributeDataModelDiagnosticDescriptors.AutoDiscoverRequiresType,
					structSymbol.Locations.FirstOrDefault(static loc => loc.IsInSource)
				)
			);
		}

		var explicitProperties = ReadExplicitProperties(
			structSymbol,
			diagnostics,
			cancellationToken
		);
		var discoveredProperties =
			autoDiscover && targetAttributeType is not null
				? DiscoverProperties(
					targetAttributeType,
					explicitProperties,
					diagnostics,
					cancellationToken
				)
				: ImmutableArray<AttributeDataModelProperty>.Empty;

		var mergedProperties = MergeProperties(explicitProperties, discoveredProperties);

		var target = new AttributeDataModelTarget(
			Namespace: structSymbol.ContainingNamespace.IsGlobalNamespace
				? null
				: structSymbol.ContainingNamespace.ToDisplayString(),
			StructName: structSymbol.Name,
			Accessibility: structSymbol.DeclaredAccessibility.ToTypeDeclarationAccessibility(),
			TargetAttribute: targetAttribute,
			MatchByInheritance: matchByInheritance,
			AutoDiscover: autoDiscover,
			Properties: new EquatableArray<AttributeDataModelProperty>(mergedProperties),
			Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutable())
		);

		if (diagnostics.Count > 0)
			return GeneratorResult<AttributeDataModelTarget>.Fail([.. diagnostics]);

		return GeneratorResult<AttributeDataModelTarget>.Ok(target);
	}

	static ImmutableArray<AttributeDataModelProperty> ReadExplicitProperties(
		INamedTypeSymbol structSymbol,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		CancellationToken cancellationToken
	)
	{
		var properties = ImmutableArray.CreateBuilder<AttributeDataModelProperty>();
		var constructor = structSymbol.InstanceConstructors.FirstOrDefault();
		if (constructor is null)
			return properties.ToImmutable();

		foreach (var parameter in constructor.Parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var attributeProperty = GetAttribute(parameter, TypeLibrary.AttributePropertyAttribute);
			if (attributeProperty is null)
				continue;

			var source = GetPropertySource(attributeProperty);
			var name = GetNamedArgument(attributeProperty, "Name", (string?)null);
			var index = GetNamedArgument(attributeProperty, "Index", -1);
			var defaultValue = GetNamedArgument(attributeProperty, "DefaultValue", (object?)null);

			var propertyType = parameter.Type;
			if (!IsSupportedType(propertyType))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						AttributeDataModelDiagnosticDescriptors.PropertyTypeNotSupported,
						parameter.Locations.FirstOrDefault(static loc => loc.IsInSource),
						ToPascalCase(parameter.Name),
						TypeHelpers.ToFullyQualifiedDisplayString(propertyType)
					)
				);
				continue;
			}

			var (modelTypeName, isNonNullableReferenceType) = GetModelTypeInfo(
				propertyType,
				autoDiscover: false
			);
			var defaultValueExpression = GetDefaultValueExpression(
				defaultValue,
				modelTypeName,
				isNonNullableReferenceType,
				propertyType,
				ToPascalCase(parameter.Name),
				diagnostics
			);

			var isNestedModel = source == AttributePropertySource.NestedModel;
			if (isNestedModel && !IsGeneratedAttributeModel(propertyType))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						AttributeDataModelDiagnosticDescriptors.NestedModelNotGenerated,
						parameter.Locations.FirstOrDefault(static loc => loc.IsInSource),
						modelTypeName
					)
				);
			}

			properties.Add(
				new AttributeDataModelProperty(
					PropertyName: ToPascalCase(parameter.Name),
					FullyQualifiedTypeName: modelTypeName,
					Source: source,
					MappedName: name,
					ConstructorIndex: index,
					DefaultValueExpression: defaultValueExpression,
					IsExplicit: true,
					IsNonNullableReferenceType: isNonNullableReferenceType,
					IsNestedModel: isNestedModel,
					NestedModelTypeName: isNestedModel ? modelTypeName : null
				)
			);
		}

		return properties.ToImmutable();
	}

	static ImmutableArray<AttributeDataModelProperty> DiscoverProperties(
		ITypeSymbol targetAttributeType,
		ImmutableArray<AttributeDataModelProperty> explicitProperties,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		CancellationToken cancellationToken
	)
	{
		if (targetAttributeType is not INamedTypeSymbol namedType)
			return ImmutableArray<AttributeDataModelProperty>.Empty;

		var discovered = ImmutableArray.CreateBuilder<AttributeDataModelProperty>();
		var discoveredNames = new HashSet<string>(StringComparer.Ordinal);

		foreach (var constructor in namedType.InstanceConstructors)
		{
			cancellationToken.ThrowIfCancellationRequested();

			for (var i = 0; i < constructor.Parameters.Length; i++)
			{
				var parameter = constructor.Parameters[i];
				var propertyName = ToPascalCase(parameter.Name);

				if (discoveredNames.Contains(propertyName))
					continue;

				if (explicitProperties.Any(p => p.PropertyName == propertyName))
					continue;

				if (!IsSupportedType(parameter.Type))
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							AttributeDataModelDiagnosticDescriptors.PropertyTypeNotSupported,
							Location.None,
							propertyName,
							TypeHelpers.ToFullyQualifiedDisplayString(parameter.Type)
						)
					);
					continue;
				}

				discoveredNames.Add(propertyName);

				var (modelTypeName, isNonNullableReferenceType) = GetModelTypeInfo(
					parameter.Type,
					autoDiscover: true
				);
				var defaultValueExpression = GetInferredDefaultExpression(
					parameter,
					modelTypeName,
					propertyName,
					diagnostics
				);

				discovered.Add(
					new AttributeDataModelProperty(
						PropertyName: propertyName,
						FullyQualifiedTypeName: modelTypeName,
						Source: AttributePropertySource.ConstructorName,
						MappedName: parameter.Name,
						ConstructorIndex: i,
						DefaultValueExpression: defaultValueExpression,
						IsExplicit: false,
						IsNonNullableReferenceType: isNonNullableReferenceType,
						IsNestedModel: false,
						NestedModelTypeName: null
					)
				);
			}
		}

		foreach (var property in namedType.GetMembers().OfType<IPropertySymbol>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (property.IsStatic || property.DeclaredAccessibility != Accessibility.Public)
				continue;

			var propertyName = property.Name;
			if (discoveredNames.Contains(propertyName))
				continue;

			if (explicitProperties.Any(p => p.PropertyName == propertyName))
				continue;

			if (!IsSupportedType(property.Type))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						AttributeDataModelDiagnosticDescriptors.PropertyTypeNotSupported,
						Location.None,
						propertyName,
						TypeHelpers.ToFullyQualifiedDisplayString(property.Type)
					)
				);
				continue;
			}

			discoveredNames.Add(propertyName);

			var (modelTypeName, isNonNullableReferenceType) = GetModelTypeInfo(
				property.Type,
				autoDiscover: true
			);
			var defaultValueExpression = GetDefaultValueExpression(
				null,
				modelTypeName,
				isNonNullableReferenceType: false,
				property.Type,
				propertyName,
				diagnostics
			);

			discovered.Add(
				new AttributeDataModelProperty(
					PropertyName: propertyName,
					FullyQualifiedTypeName: modelTypeName,
					Source: AttributePropertySource.NamedArgument,
					MappedName: property.Name,
					ConstructorIndex: -1,
					DefaultValueExpression: defaultValueExpression,
					IsExplicit: false,
					IsNonNullableReferenceType: isNonNullableReferenceType,
					IsNestedModel: false,
					NestedModelTypeName: null
				)
			);
		}

		return discovered.ToImmutable();
	}

	static ImmutableArray<AttributeDataModelProperty> MergeProperties(
		ImmutableArray<AttributeDataModelProperty> explicitProperties,
		ImmutableArray<AttributeDataModelProperty> discoveredProperties
	)
	{
		if (explicitProperties.IsEmpty)
			return discoveredProperties;

		if (discoveredProperties.IsEmpty)
			return explicitProperties;

		var merged = ImmutableArray.CreateBuilder<AttributeDataModelProperty>();
		var explicitNames = new HashSet<string>(
			explicitProperties.Select(static p => p.PropertyName)
		);

		merged.AddRange(explicitProperties);
		foreach (var discovered in discoveredProperties)
		{
			if (!explicitNames.Contains(discovered.PropertyName))
				merged.Add(discovered);
		}

		return merged.ToImmutable();
	}

	static string GetDefaultValueExpression(
		object? defaultValue,
		string modelTypeName,
		bool isNonNullableReferenceType,
		ITypeSymbol originalType,
		string propertyName,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics
	)
	{
		if (defaultValue is not null)
		{
			if (TryFormatValue(defaultValue, originalType, out var expression))
				return expression;

			diagnostics.Add(
				DiagnosticInfo.Create(
					AttributeDataModelDiagnosticDescriptors.DefaultValueNotSupported,
					Location.None,
					defaultValue.ToString() ?? "null",
					TypeHelpers.ToFullyQualifiedDisplayString(originalType)
				)
			);
		}

		if (isNonNullableReferenceType)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					AttributeDataModelDiagnosticDescriptors.NonNullableReferenceTypeRequiresDefault,
					Location.None,
					propertyName
				)
			);
		}

		return $"default({modelTypeName})";
	}

	static string GetInferredDefaultExpression(
		IParameterSymbol parameter,
		string modelTypeName,
		string propertyName,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics
	)
	{
		if (parameter.HasExplicitDefaultValue)
		{
			return GetDefaultValueExpression(
				parameter.ExplicitDefaultValue,
				modelTypeName,
				isNonNullableReferenceType: false,
				parameter.Type,
				propertyName,
				diagnostics
			);
		}

		return $"default({modelTypeName})";
	}

	static bool TryFormatValue(object? value, ITypeSymbol typeSymbol, out string expression)
	{
		expression = string.Empty;

		if (value is null)
		{
			expression = "null";
			return true;
		}

		if (value is string s)
		{
			expression = $"\"{EscapeString(s)}\"";
			return true;
		}

		if (value is bool b)
		{
			expression = b ? "true" : "false";
			return true;
		}

		if (value is ITypeSymbol typeValue)
		{
			expression = $"typeof(global::{TypeHelpers.ToFullyQualifiedDisplayString(typeValue)})";
			return true;
		}

		if (typeSymbol.TypeKind == TypeKind.Enum)
		{
			var enumTypeName = TypeHelpers.ToFullyQualifiedDisplayString(typeSymbol);
			expression =
				$"(global::{enumTypeName}){Convert.ToString(value, CultureInfo.InvariantCulture)}";
			return true;
		}

		if (value is IFormattable formattable)
		{
			expression = formattable.ToString(null, CultureInfo.InvariantCulture);
			return expression is not null;
		}

		return false;
	}

	static string EscapeString(string value)
	{
		var builder = new StringBuilder(value.Length);
		foreach (var c in value)
		{
			builder.Append(
				c switch
				{
					'"' => "\\\"",
					'\\' => "\\\\",
					'\n' => "\\n",
					'\r' => "\\r",
					'\t' => "\\t",
					_ => c.ToString(),
				}
			);
		}
		return builder.ToString();
	}

	static (string TypeName, bool IsNonNullableReferenceType) GetModelTypeInfo(
		ITypeSymbol typeSymbol,
		bool autoDiscover
	)
	{
		if (IsSystemType(typeSymbol))
			return ("global::Microsoft.CodeAnalysis.ITypeSymbol?", false);

		string typeName;
		if (TypeHelpers.TryGetKeyword(typeSymbol.SpecialType, out var keyword))
		{
			typeName = keyword!;
		}
		else
		{
			typeName = TypeHelpers.ToFullyQualifiedDisplayString(typeSymbol);
		}

		var isNonNullableReferenceType = false;

		if (
			typeSymbol.IsReferenceType
			&& typeSymbol.NullableAnnotation == NullableAnnotation.Annotated
			&& !typeName.EndsWith("?", StringComparison.Ordinal)
		)
		{
			typeName += "?";
		}
		else if (
			autoDiscover
			&& typeSymbol.IsReferenceType
			&& !typeName.EndsWith("?", StringComparison.Ordinal)
		)
		{
			typeName += "?";
		}
		else if (
			typeSymbol.IsReferenceType
			&& typeSymbol.NullableAnnotation != NullableAnnotation.Annotated
		)
		{
			isNonNullableReferenceType = true;
		}

		return (typeName, isNonNullableReferenceType);
	}

	static bool IsSystemType(ITypeSymbol typeSymbol)
	{
		if (typeSymbol is not INamedTypeSymbol namedType)
			return false;

		var namespaceName = namedType.ContainingNamespace.IsGlobalNamespace
			? null
			: namedType.ContainingNamespace.ToDisplayString();

		return namespaceName == "System" && namedType.Name == "Type";
	}

	static bool IsSupportedType(ITypeSymbol typeSymbol)
	{
		return typeSymbol.TypeKind
			is not TypeKind.Array
				and not TypeKind.Pointer
				and not TypeKind.FunctionPointer;
	}

	static bool IsGeneratedAttributeModel(ITypeSymbol typeSymbol)
	{
		if (typeSymbol is not INamedTypeSymbol namedType || namedType.TypeKind != TypeKind.Struct)
			return false;

		return GetAttribute(namedType, TypeLibrary.GenerateAttributeDataModelAttribute) is not null;
	}

	static AttributeData? GetAttribute(ISymbol symbol, TypeValueObject attributeType)
	{
		foreach (var attribute in symbol.GetAttributes())
		{
			if (
				attribute.AttributeClass is not null
				&& attributeType.Equals(attribute.AttributeClass)
			)
				return attribute;
		}
		return null;
	}

	static TypeValueObject ParseTypeValueObject(string fullyQualifiedName)
	{
		var lastDot = fullyQualifiedName.LastIndexOf('.');
		if (lastDot < 0)
			return new TypeValueObject(fullyQualifiedName, null);

		var typeName = fullyQualifiedName.Substring(lastDot + 1);
		var namespaceName = fullyQualifiedName.Substring(0, lastDot);
		return new TypeValueObject(typeName, namespaceName);
	}

	static AttributePropertySource GetPropertySource(AttributeData attributeData)
	{
		foreach (var arg in attributeData.NamedArguments)
		{
			if (arg.Key != "Source")
				continue;

			var value = arg.Value.Value;
			if (value is int intValue)
				return (AttributePropertySource)intValue;
		}

		if (attributeData.ConstructorArguments.Length > 0)
		{
			var value = attributeData.ConstructorArguments[0].Value;
			if (value is int intValue)
				return (AttributePropertySource)intValue;
		}

		return AttributePropertySource.NamedArgument;
	}

	static T? GetNamedArgument<T>(AttributeData attributeData, string name, T? defaultValue)
	{
		foreach (var arg in attributeData.NamedArguments)
		{
			if (arg.Key != name)
				continue;

			var value = arg.Value.Value;
			if (value is T typedValue)
				return typedValue;

			if (value is null)
				return defaultValue;

			try
			{
				return (T?)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
			}
			catch
			{
				return defaultValue;
			}
		}
		return defaultValue;
	}

	static string ToPascalCase(string value)
	{
		if (string.IsNullOrEmpty(value))
			return value;

		var builder = new StringBuilder(value.Length);
		var newWord = true;
		foreach (var c in value)
		{
			if (c == '_')
			{
				newWord = true;
				continue;
			}

			builder.Append(newWord ? char.ToUpperInvariant(c) : c);
			newWord = false;
		}
		return builder.ToString();
	}
}
