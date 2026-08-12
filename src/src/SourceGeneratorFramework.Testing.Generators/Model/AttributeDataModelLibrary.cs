using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing.Abstractions;

namespace Purview.SourceGeneratorFramework.Testing.Generators.Model;

static class AttributeDataModelLibrary
{
	public static IncrementalValuesProvider<GeneratorResult<AttributeDataModelTarget>> GetTargets(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		return IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TypeLibrary.GenerateAttributeDataModelAttribute,
			(ctx, ct) =>
			{
				var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.TargetNode, ct);
				if (symbol is not INamedTypeSymbol { TypeKind: TypeKind.Struct } structSymbol)
					return GeneratorResult<AttributeDataModelTarget>.Empty;

				return BuildTarget(structSymbol, logger, ct);
			}
		);
	}

	static GeneratorResult<AttributeDataModelTarget> BuildTarget(
		INamedTypeSymbol structSymbol,
		GenerationLogger? logger,
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

		var excludedNames = new HashSet<string>(StringComparer.Ordinal);
		var explicitProperties = ReadExplicitProperties(
			structSymbol,
			excludedNames,
			diagnostics,
			logger,
			cancellationToken
		);
		var discoveredProperties =
			autoDiscover && targetAttributeType is not null
				? DiscoverProperties(
					targetAttributeType,
					explicitProperties,
					excludedNames,
					diagnostics,
					logger,
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
		HashSet<string> excludedNames,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		GenerationLogger? logger,
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

			var propertyName = ToPascalCase(parameter.Name);
			var propertyType = parameter.Type;
			if (!IsSupportedType(propertyType))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						AttributeDataModelDiagnosticDescriptors.PropertyTypeNotSupported,
						parameter.Locations.FirstOrDefault(static loc => loc.IsInSource),
						propertyName,
						TypeHelpers.ToFullyQualifiedDisplayString(propertyType)
					)
				);
				continue;
			}

			var info = ReadParameterAttributes(parameter, propertyName, logger);

			if (info.IsExcluded)
			{
				excludedNames.Add(propertyName);
				continue;
			}

			if (info.IsNestedModel && !IsGeneratedAttributeModel(propertyType))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						AttributeDataModelDiagnosticDescriptors.NestedModelNotGenerated,
						parameter.Locations.FirstOrDefault(static loc => loc.IsInSource),
						TypeHelpers.ToFullyQualifiedDisplayString(propertyType)
					)
				);
			}

			var sources = info.Sources;
			if (sources.IsEmpty)
			{
				sources = ImmutableArray.Create(
					new PropertySource(AttributePropertySource.NamedArgument, propertyName, -1)
				);
				logger?.Debug(
					$"Parameter '{propertyName}' has no source attribute; defaulting to named argument."
				);
			}

			var (modelTypeName, isNonNullableReferenceType) = GetModelTypeInfo(
				propertyType,
				autoDiscover: false
			);
			var defaultValueExpression = GetDefaultValueExpression(
				info.DefaultValue,
				modelTypeName,
				propertyType,
				diagnostics
			);

			properties.Add(
				new AttributeDataModelProperty(
					PropertyName: propertyName,
					FullyQualifiedTypeName: modelTypeName,
					Sources: sources,
					DefaultValueExpression: defaultValueExpression,
					HasDefaultValue: info.HasDefaultValue,
					IsExplicit: true,
					IsNonNullableReferenceType: isNonNullableReferenceType,
					IsNestedModel: info.IsNestedModel,
					NestedModelTypeName: info.IsNestedModel ? modelTypeName : null
				)
			);
		}

		return properties.ToImmutable();
	}

	static ParameterAttributeInfo ReadParameterAttributes(
		IParameterSymbol parameter,
		string propertyName,
		GenerationLogger? logger
	)
	{
		var sources = ImmutableArray.CreateBuilder<PropertySource>();
		var isExcluded = false;
		var isNestedModel = false;
		object? defaultValue = null;
		var hasDefaultValue = false;

		var excludeAttribute = GetAttribute(
			parameter,
			TypeLibrary.AttributeExcludePropertyAttribute
		);
		if (excludeAttribute is not null)
		{
			isExcluded = true;
			logger?.Debug($"Parameter '{propertyName}' is excluded from the attribute model.");
		}

		var nestedModelAttribute = GetAttribute(
			parameter,
			TypeLibrary.AttributeNestedModelPropertyAttribute
		);
		if (nestedModelAttribute is not null)
		{
			if (isExcluded)
			{
				logger?.Debug(
					$"Parameter '{propertyName}' has both [AttributeExclude] and [AttributeNestedModelProperty]; excluding takes precedence."
				);
			}
			else
			{
				isNestedModel = true;
				defaultValue = GetNamedArgument(
					nestedModelAttribute,
					"DefaultValue",
					(object?)null
				);
				hasDefaultValue = defaultValue is not null;
				sources.Add(new PropertySource(AttributePropertySource.NestedModel, null, -1));
			}
		}

		var hasExclusive = isExcluded || isNestedModel;

		var ctorAttribute = GetAttribute(parameter, TypeLibrary.AttributeCtorPropertyAttribute);
		if (ctorAttribute is not null && !hasExclusive)
		{
			var ctorName = GetCtorPropertyName(ctorAttribute);
			var ctorIndex = GetCtorPropertyIndex(ctorAttribute);
			var ctorDefaultValue = GetNamedArgument(ctorAttribute, "DefaultValue", (object?)null);

			if (ctorName is not null)
			{
				sources.Add(
					new PropertySource(AttributePropertySource.ConstructorName, ctorName, -1)
				);
				logger?.Debug(
					$"Parameter '{propertyName}' maps to constructor parameter '{ctorName}'."
				);
			}
			else if (ctorIndex >= 0)
			{
				sources.Add(
					new PropertySource(AttributePropertySource.ConstructorIndex, null, ctorIndex)
				);
				logger?.Debug(
					$"Parameter '{propertyName}' maps to constructor argument index {ctorIndex}."
				);
			}

			if (ctorDefaultValue is not null)
			{
				defaultValue = ctorDefaultValue;
				hasDefaultValue = true;
			}
		}
		else if (ctorAttribute is not null)
		{
			logger?.Debug(
				$"Parameter '{propertyName}' has conflicting attributes; [AttributeCtorProperty] is ignored."
			);
		}

		var namedAttribute = GetAttribute(parameter, TypeLibrary.AttributeNamedPropertyAttribute);
		if (namedAttribute is not null && !hasExclusive)
		{
			var namedName = GetNamedArgument(namedAttribute, "Name", (string?)null);
			var namedDefaultValue = GetNamedArgument(namedAttribute, "DefaultValue", (object?)null);

			sources.Add(
				new PropertySource(
					AttributePropertySource.NamedArgument,
					namedName ?? propertyName,
					-1
				)
			);
			logger?.Debug(
				$"Parameter '{propertyName}' maps to named argument '{namedName ?? propertyName}'."
			);

			if (namedDefaultValue is not null)
			{
				defaultValue = namedDefaultValue;
				hasDefaultValue = true;
			}
		}
		else if (namedAttribute is not null)
		{
			logger?.Debug(
				$"Parameter '{propertyName}' has conflicting attributes; [AttributeNamedProperty] is ignored."
			);
		}

		return new ParameterAttributeInfo(
			isExcluded,
			isNestedModel,
			sources.ToImmutable(),
			defaultValue,
			hasDefaultValue
		);
	}

	sealed record ParameterAttributeInfo(
		bool IsExcluded,
		bool IsNestedModel,
		ImmutableArray<PropertySource> Sources,
		object? DefaultValue,
		bool HasDefaultValue
	);

	static string? GetCtorPropertyName(AttributeData attributeData)
	{
		if (
			attributeData.ConstructorArguments.Length > 0
			&& attributeData.ConstructorArguments[0].Value is string name
		)
			return name;

		return GetNamedArgument(attributeData, "Name", (string?)null);
	}

	static int GetCtorPropertyIndex(AttributeData attributeData)
	{
		if (
			attributeData.ConstructorArguments.Length > 0
			&& attributeData.ConstructorArguments[0].Value is int index
		)
			return index;

		return GetNamedArgument(attributeData, "Index", -1);
	}

	static ImmutableArray<AttributeDataModelProperty> DiscoverProperties(
		ITypeSymbol targetAttributeType,
		ImmutableArray<AttributeDataModelProperty> explicitProperties,
		HashSet<string> excludedNames,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		GenerationLogger? logger,
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

				if (excludedNames.Contains(propertyName))
				{
					logger?.Debug(
						$"Skipping discovered constructor parameter '{propertyName}' because it is explicitly excluded."
					);
					continue;
				}

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
					diagnostics
				);

				discovered.Add(
					new AttributeDataModelProperty(
						PropertyName: propertyName,
						FullyQualifiedTypeName: modelTypeName,
						Sources: ImmutableArray.Create(
							new PropertySource(
								AttributePropertySource.ConstructorName,
								parameter.Name,
								i
							)
						),
						DefaultValueExpression: defaultValueExpression,
						HasDefaultValue: parameter.HasExplicitDefaultValue,
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

			if (excludedNames.Contains(propertyName))
			{
				logger?.Debug(
					$"Skipping discovered named property '{propertyName}' because it is explicitly excluded."
				);
				continue;
			}

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
				property.Type,
				diagnostics
			);

			discovered.Add(
				new AttributeDataModelProperty(
					PropertyName: propertyName,
					FullyQualifiedTypeName: modelTypeName,
					Sources: ImmutableArray.Create(
						new PropertySource(AttributePropertySource.NamedArgument, property.Name, -1)
					),
					DefaultValueExpression: defaultValueExpression,
					HasDefaultValue: false,
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
		ITypeSymbol originalType,
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

		return $"default({modelTypeName})";
	}

	static string GetInferredDefaultExpression(
		IParameterSymbol parameter,
		string modelTypeName,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics
	)
	{
		if (parameter.HasExplicitDefaultValue)
		{
			return GetDefaultValueExpression(
				parameter.ExplicitDefaultValue,
				modelTypeName,
				parameter.Type,
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
