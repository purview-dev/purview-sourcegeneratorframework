using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Logging;

namespace Purview.SourceGeneratorFramework.Generators.Model;

static class AttributeDataModelLibrary
{
	static readonly TypeValueObject SystemType = new(typeof(Type));

	public static IncrementalValuesProvider<GeneratorResult<AttributeDataModelTarget>> GetTargets(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		return IncrementalPipeline
			.ForAttributeWithMetadataName(
				context,
				GeneratorTypeLibrary.GenerateAttribute,
				(ctx, ct) =>
				{
					var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.TargetNode, ct);
					return symbol is not INamedTypeSymbol { TypeKind: TypeKind.Struct } structSymbol
						? GeneratorResult<AttributeDataModelTarget>.Empty
						: BuildTarget(structSymbol, logger, ct);
				}
			)
			.WithTrackingName("GetAttributeDataTargets");
	}

	static GeneratorResult<AttributeDataModelTarget> BuildTarget(
		INamedTypeSymbol structSymbol,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
		var generateAttribute = GetAttribute(structSymbol, GeneratorTypeLibrary.GenerateAttribute);
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
				: [];

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

		return diagnostics.Count > 0
			? GeneratorResult<AttributeDataModelTarget>.Fail([.. diagnostics])
			: GeneratorResult<AttributeDataModelTarget>.Ok(target);
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

			if (info.IsTypeArgument && !IsTypeSymbolType(propertyType))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						AttributeDataModelDiagnosticDescriptors.TypeArgumentPropertyTypeInvalid,
						parameter.Locations.FirstOrDefault(static loc => loc.IsInSource),
						propertyName,
						TypeHelpers.ToFullyQualifiedDisplayString(propertyType)
					)
				);
			}

			if (info.IsEnum && propertyType.SpecialType != SpecialType.System_String)
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						AttributeDataModelDiagnosticDescriptors.IsEnumRequiresStringType,
						parameter.Locations.FirstOrDefault(static loc => loc.IsInSource),
						propertyName,
						TypeHelpers.ToFullyQualifiedDisplayString(propertyType)
					)
				);
			}

			var sources = info.Sources;
			if (sources.IsEmpty)
			{
				sources = [new(AttributePropertySource.NamedArgument, propertyName, -1)];
				logger?.Debug($"Parameter '{propertyName}' has no source attribute; defaulting to named argument.");
			}

			var (modelTypeName, isNonNullableReferenceType) = GetModelTypeInfo(propertyType, autoDiscover: false);
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
					IsEnum: info.IsEnum,
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
		var isTypeArgument = false;
		object? defaultValue;
		bool hasDefaultValue;

		var excludeAttribute = GetAttribute(parameter, GeneratorTypeLibrary.ExcludeAttribute);
		if (excludeAttribute is not null)
		{
			isExcluded = true;
			logger?.Debug($"Parameter '{propertyName}' is excluded from the attribute model.");
		}

		var nestedModelInfo = ReadNestedModelAttributeInfo(parameter, propertyName, isExcluded, logger);
		var isNestedModel = nestedModelInfo.IsNestedModel;
		if (nestedModelInfo.IsNestedModel)
		{
			defaultValue = nestedModelInfo.DefaultValue;
			hasDefaultValue = nestedModelInfo.HasDefaultValue;
			sources.AddRange(nestedModelInfo.Sources);
		}
		else
		{
			var (IsTypeArgument, Sources, DefaultValue, HasDefaultValue) = ReadTypeArgumentAttributeInfo(
				parameter,
				propertyName,
				isExcluded,
				logger
			);

			isTypeArgument = IsTypeArgument;
			defaultValue = DefaultValue;
			hasDefaultValue = HasDefaultValue;
			sources.AddRange(Sources);
		}

		var hasExclusive = isExcluded || isNestedModel || isTypeArgument;

		var ctorAttribute = GetAttribute(parameter, GeneratorTypeLibrary.ArgumentAttribute);
		var isEnum = false;
		if (ctorAttribute is not null && !hasExclusive)
		{
			var ctorName = GetCtorPropertyName(ctorAttribute);
			var ctorIndex = GetCtorPropertyIndex(ctorAttribute);
			var ctorDefaultValue = GetNamedArgument(ctorAttribute, "DefaultValue", (object?)null);
			isEnum = GetNamedArgument(ctorAttribute, "IsEnum", false);

			if (ctorName is not null)
			{
				sources.Add(new PropertySource(AttributePropertySource.ConstructorName, ctorName, -1));
				logger?.Debug($"Parameter '{propertyName}' maps to constructor parameter '{ctorName}'.");
			}
			else if (ctorIndex >= 0)
			{
				sources.Add(new PropertySource(AttributePropertySource.ConstructorIndex, null, ctorIndex));
				logger?.Debug($"Parameter '{propertyName}' maps to constructor argument index {ctorIndex}.");
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
				$"Parameter '{propertyName}' has conflicting attributes; [{GeneratorTypeLibrary.ArgumentAttribute.RenderTypeName}] is ignored."
			);
		}

		var namedAttribute = GetAttribute(parameter, GeneratorTypeLibrary.PropertyAttribute);
		if (namedAttribute is not null && !hasExclusive)
		{
			var namedName = GetNamedArgument(namedAttribute, "Name", (string?)null);
			var namedDefaultValue = GetNamedArgument(namedAttribute, "DefaultValue", (object?)null);
			isEnum = isEnum || GetNamedArgument(namedAttribute, "IsEnum", false);

			sources.Add(new PropertySource(AttributePropertySource.NamedArgument, namedName ?? propertyName, -1));
			logger?.Debug($"Parameter '{propertyName}' maps to named argument '{namedName ?? propertyName}'.");

			if (namedDefaultValue is not null)
			{
				defaultValue = namedDefaultValue;
				hasDefaultValue = true;
			}
		}
		else if (namedAttribute is not null)
		{
			logger?.Debug(
				$"Parameter '{propertyName}' has conflicting attributes; [{GeneratorTypeLibrary.PropertyAttribute.RenderTypeName}] is ignored."
			);
		}

		return new ParameterAttributeInfo(
			isExcluded,
			isNestedModel,
			isTypeArgument,
			sources.ToImmutable(),
			defaultValue,
			hasDefaultValue,
			isEnum
		);
	}

	sealed record ParameterAttributeInfo(
		bool IsExcluded,
		bool IsNestedModel,
		bool IsTypeArgument,
		ImmutableArray<PropertySource> Sources,
		object? DefaultValue,
		bool HasDefaultValue,
		bool IsEnum
	);

	static (
		bool IsNestedModel,
		ImmutableArray<PropertySource> Sources,
		object? DefaultValue,
		bool HasDefaultValue
	) ReadNestedModelAttributeInfo(
		IParameterSymbol parameter,
		string propertyName,
		bool isExcluded,
		GenerationLogger? logger
	)
	{
		var nestedModelAttribute = GetAttribute(parameter, GeneratorTypeLibrary.NestedModelAttribute);
		if (nestedModelAttribute is null)
			return (false, [], null, false);

		if (isExcluded)
		{
			logger?.Debug(
				$"Parameter '{propertyName}' has both [{GeneratorTypeLibrary.ExcludeAttribute.RenderTypeName}] and [{GeneratorTypeLibrary.NestedModelAttribute.RenderTypeName}]; excluding takes precedence."
			);
			return (false, [], null, false);
		}

		var defaultValue = GetNamedArgument(nestedModelAttribute, "DefaultValue", (object?)null);
		var sources = ImmutableArray.CreateBuilder<PropertySource>();
		sources.Add(new PropertySource(AttributePropertySource.NestedModel, null, -1));
		logger?.Debug($"Parameter '{propertyName}' is a nested model.");

		return (true, sources.ToImmutable(), defaultValue, defaultValue is not null);
	}

	static (
		bool IsTypeArgument,
		ImmutableArray<PropertySource> Sources,
		object? DefaultValue,
		bool HasDefaultValue
	) ReadTypeArgumentAttributeInfo(
		IParameterSymbol parameter,
		string propertyName,
		bool isExcluded,
		GenerationLogger? logger
	)
	{
		var typeArgumentAttribute = GetAttribute(parameter, GeneratorTypeLibrary.GenericTypeArgumentAttribute);
		if (typeArgumentAttribute is null)
			return (false, [], null, false);

		if (isExcluded)
		{
			logger?.Debug(
				$"Parameter '{propertyName}' has both [{GeneratorTypeLibrary.ExcludeAttribute.RenderTypeName}] and [{GeneratorTypeLibrary.GenericTypeArgumentAttribute.RenderTypeName}]; excluding takes precedence."
			);
			return (false, [], null, false);
		}

		var defaultValue = GetNamedArgument(typeArgumentAttribute, "DefaultValue", (object?)null);
		var hasDefaultValue = defaultValue is not null;

		var sources = ImmutableArray.CreateBuilder<PropertySource>();
		var typeArgName = GetNamedArgument(typeArgumentAttribute, "Name", (string?)null);
		var typeArgIndex = GetNamedArgument(typeArgumentAttribute, "Index", -1);
		if (typeArgName is not null)
		{
			sources.Add(new PropertySource(AttributePropertySource.TypeArgument, typeArgName, -1));
			logger?.Debug($"Parameter '{propertyName}' maps to generic type argument '{typeArgName}'.");
		}
		else
		{
			sources.Add(
				new PropertySource(AttributePropertySource.TypeArgument, null, typeArgIndex >= 0 ? typeArgIndex : 0)
			);
			logger?.Debug($"Parameter '{propertyName}' maps to generic type argument index {typeArgIndex}.");
		}

		return (true, sources.ToImmutable(), defaultValue, hasDefaultValue);
	}

	static string? GetCtorPropertyName(AttributeData attributeData)
	{
		return
			attributeData.ConstructorArguments.Length > 0 && attributeData.ConstructorArguments[0].Value is string name
			? name
			: GetNamedArgument(attributeData, "Name", (string?)null);
	}

	static int GetCtorPropertyIndex(AttributeData attributeData)
	{
		return attributeData.ConstructorArguments.Length > 0 && attributeData.ConstructorArguments[0].Value is int index
			? index
			: GetNamedArgument(attributeData, "Index", -1);
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
			return [];

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

				var (modelTypeName, isNonNullableReferenceType) = GetModelTypeInfo(parameter.Type, autoDiscover: true);
				var defaultValueExpression = GetInferredDefaultExpression(parameter, modelTypeName, diagnostics);

				discovered.Add(
					new AttributeDataModelProperty(
						PropertyName: propertyName,
						FullyQualifiedTypeName: modelTypeName,
						Sources: [new PropertySource(AttributePropertySource.ConstructorName, parameter.Name, i)],
						DefaultValueExpression: defaultValueExpression,
						HasDefaultValue: parameter.HasExplicitDefaultValue,
						IsExplicit: false,
						IsNonNullableReferenceType: isNonNullableReferenceType,
						IsNestedModel: false,
						IsEnum: false,
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

			var (modelTypeName, isNonNullableReferenceType) = GetModelTypeInfo(property.Type, autoDiscover: true);
			var defaultValueExpression = GetDefaultValueExpression(null, modelTypeName, property.Type, diagnostics);

			discovered.Add(
				new AttributeDataModelProperty(
					PropertyName: propertyName,
					FullyQualifiedTypeName: modelTypeName,
					Sources: [new PropertySource(AttributePropertySource.NamedArgument, property.Name, -1)],
					DefaultValueExpression: defaultValueExpression,
					HasDefaultValue: false,
					IsExplicit: false,
					IsNonNullableReferenceType: isNonNullableReferenceType,
					IsNestedModel: false,
					IsEnum: false,
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
		var explicitNames = new HashSet<string>(explicitProperties.Select(static p => p.PropertyName));

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
		return parameter.HasExplicitDefaultValue
			? GetDefaultValueExpression(parameter.ExplicitDefaultValue, modelTypeName, parameter.Type, diagnostics)
			: $"default({modelTypeName})";
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
			expression = $"(global::{enumTypeName}){Convert.ToString(value, CultureInfo.InvariantCulture)}";
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
			return ("global::Microsoft.CodeAnalysis.INamedTypeSymbol?", false);

		var knownType = KnownLangTypes.Get(typeSymbol.SpecialType);
		var typeName = knownType.IsEmpty ? TypeHelpers.ToFullyQualifiedDisplayString(typeSymbol) : knownType.Keyword;

		var isNonNullableReferenceType = false;

		if (
			typeSymbol.IsReferenceType
			&& typeSymbol.NullableAnnotation == NullableAnnotation.Annotated
			&& !typeName.EndsWith("?", StringComparison.Ordinal)
		)
		{
			typeName += "?";
		}
		else if (autoDiscover && typeSymbol.IsReferenceType && !typeName.EndsWith("?", StringComparison.Ordinal))
		{
			typeName += "?";
		}
		else if (typeSymbol.IsReferenceType && typeSymbol.NullableAnnotation != NullableAnnotation.Annotated)
		{
			isNonNullableReferenceType = true;
		}

		return (typeName, isNonNullableReferenceType);
	}

	static bool IsSystemType(ITypeSymbol typeSymbol) => SystemType.Equals(typeSymbol);

	static bool IsTypeSymbolType(ITypeSymbol typeSymbol)
	{
		if (typeSymbol is not INamedTypeSymbol namedType)
			return false;

		var namespaceName = namedType.ContainingNamespace.IsGlobalNamespace
			? null
			: namedType.ContainingNamespace.ToDisplayString();

		return namespaceName == "Microsoft.CodeAnalysis"
			&& namedType.Name is "ITypeSymbol" or "INamedTypeSymbol" or "ISymbol";
	}

	static bool IsSupportedType(ITypeSymbol typeSymbol)
	{
		return typeSymbol.TypeKind is not TypeKind.Array and not TypeKind.Pointer and not TypeKind.FunctionPointer;
	}

	static bool IsGeneratedAttributeModel(ITypeSymbol typeSymbol)
	{
		return typeSymbol is not INamedTypeSymbol namedType || namedType.TypeKind != TypeKind.Struct
			? false
			: GetAttribute(namedType, GeneratorTypeLibrary.GenerateAttribute) is not null;
	}

	static AttributeData? GetAttribute(ISymbol symbol, TypeValueObject attributeType)
	{
		foreach (var attribute in symbol.GetAttributes())
		{
			if (attribute.AttributeClass is not null && attributeType.Equals(attribute.AttributeClass))
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
