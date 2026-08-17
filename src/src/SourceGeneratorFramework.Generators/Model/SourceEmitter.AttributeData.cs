using Microsoft.CodeAnalysis.Text;

namespace Purview.SourceGeneratorFramework.Generators.Model;

partial class SourceEmitter
{
	static SourceText GenerateAttribute()
	{
		var writer = CreateWriter(
			nameof(AttributeDataModelGenerator),
			GeneratorTypeLibrary.Attirbutes.GenerateAttribute
		);

		return writer
			.XmlSummary("Generates parsing members for an attribute-data model.")
			.WriteAttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.GenerateAttribute),
				AttributeTargets.Struct,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary("Initializes the attribute for the target attribute type.")
						.WriteConstructor(
							new(GeneratorTypeLibrary.Attirbutes.GenerateAttribute)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								Parameters = [new("targetAttribute", PurviewTypeLibrary.System.Type)],
							},
							constructorWriter =>
								constructorWriter.WriteLine(
									$"TargetAttribute = targetAttribute ?? throw new global::System.ArgumentNullException(nameof(targetAttribute));"
								)
						);

					bodyWriter
						.XmlSummary("Initializes the attribute for the target attribute name.")
						.WriteConstructor(
							new(GeneratorTypeLibrary.Attirbutes.GenerateAttribute)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								Parameters = [new("targetAttributeName", PurviewTypeLibrary.System.String)],
							},
							constructorWriter =>
								constructorWriter.WriteLine(
									$"TargetAttributeName = targetAttributeName ?? throw new global::System.ArgumentNullException(nameof(targetAttributeName));"
								)
						);

					bodyWriter
						.XmlSummary("Gets the attribute type represented by the generated model.")
						.WriteProperty(
							new("TargetAttribute", PurviewTypeLibrary.System.Type.MakeNullable())
							{
								Accessibility = TypeDeclarationAccessibility.Public,
							}
						);

					bodyWriter
						.XmlSummary("Gets the attribute type name represented by the generated model.")
						.WriteProperty(
							new("TargetAttributeName", PurviewTypeLibrary.System.String.MakeNullable())
							{
								Accessibility = TypeDeclarationAccessibility.Public,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets whether derived attribute types are accepted.")
						.WriteProperty(
							new("MatchByInheritance", PurviewTypeLibrary.System.Boolean)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets whether the attribute should be automatically discovered.")
						.WriteProperty(
							new("AutoDiscover", PurviewTypeLibrary.System.Boolean)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
							}
						);
				}
			);
	}

	static SourceText PropertyAttribute()
	{
		var writer = CreateWriter(
			nameof(AttributeDataModelGenerator),
			GeneratorTypeLibrary.Attirbutes.PropertyAttribute
		);

		return writer
			.XmlSummary("Marks a record parameter as a named attribute argument.")
			.WriteAttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.PropertyAttribute),
				AttributeTargets.Parameter,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary(
							$"Initializes a new instance of the <see cref=\"{GeneratorTypeLibrary.Attirbutes.PropertyAttribute}\"/> class."
						)
						.WriteConstructor(
							new(GeneratorTypeLibrary.Attirbutes.PropertyAttribute)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								Parameters =
								[
									new("defaultValue", PurviewTypeLibrary.System.Object.MakeNullable())
									{
										DefaultValue = "null",
									},
								],
							},
							writeBody => writeBody.WriteLine("DefaultValue = defaultValue;")
						);

					bodyWriter
						.XmlSummary("Gets or sets an optional named-property mapping.")
						.WriteProperty(
							new("Name", PurviewTypeLibrary.System.String.MakeNullable())
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets the value used when the named argument is not specified.")
						.WriteProperty(
							new("DefaultValue", PurviewTypeLibrary.System.Object.MakeNullable())
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary(
							"Gets or sets a value indicating whether the property represents an enum whose type is not known to the generator."
						)
						.WriteProperty(
							new("IsEnum", PurviewTypeLibrary.System.Boolean)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
							}
						);
				}
			);
	}

	static SourceText ArgumentAttribute()
	{
		var writer = CreateWriter(
			nameof(AttributeDataModelGenerator),
			GeneratorTypeLibrary.Attirbutes.ArgumentAttribute
		);

		return writer
			.XmlSummary("Marks a record parameter as a constructor argument.")
			.WriteAttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.ArgumentAttribute),
				AttributeTargets.Parameter,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary(
							$"Initializes a new instance of the <see cref=\"{GeneratorTypeLibrary.Attirbutes.ArgumentAttribute}\"/> class."
						)
						.WriteConstructor(
							new(GeneratorTypeLibrary.Attirbutes.ArgumentAttribute)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
							},
							bodyWriter => bodyWriter.Comment("Empty")
						);

					bodyWriter.WriteConstructor(
						new(GeneratorTypeLibrary.Attirbutes.ArgumentAttribute)
						{
							Accessibility = TypeDeclarationAccessibility.Public,
							Parameters =
							[
								new("name", PurviewTypeLibrary.System.String),
								new("defaultValue", PurviewTypeLibrary.System.Object.MakeNullable())
								{
									DefaultValue = "null",
								},
							],
						},
						writerBody =>
							writer
								.WriteLine(
									"Name = name ?? throw new global::System.ArgumentNullException(nameof(name));"
								)
								.WriteLine("DefaultValue = defaultValue;")
					);

					bodyWriter.WriteConstructor(
						new(GeneratorTypeLibrary.Attirbutes.ArgumentAttribute)
						{
							Accessibility = TypeDeclarationAccessibility.Public,
							Parameters =
							[
								new("index", PurviewTypeLibrary.System.Int32),
								new("defaultValue", PurviewTypeLibrary.System.Object.MakeNullable())
								{
									DefaultValue = "null",
								},
							],
						},
						writerBody => writer.WriteLine("Index = index;").WriteLine("DefaultValue = defaultValue;")
					);

					bodyWriter
						.XmlSummary("Gets or sets the constructor parameter name.")
						.WriteProperty(
							new("Name", PurviewTypeLibrary.System.String.MakeNullable())
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets the constructor argument index.")
						.WriteProperty(
							new("Index", PurviewTypeLibrary.System.Int32)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
								Initializer = "-1",
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets the value used when the constructor argument is not specified.")
						.WriteProperty(
							new("DefaultValue", PurviewTypeLibrary.System.Object.MakeNullable())
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary(
							"Gets or sets a value indicating whether the argument represents an enum whose type is not known to the generator."
						)
						.WriteProperty(
							new("IsEnum", PurviewTypeLibrary.System.Boolean)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
							}
						);
				}
			);
	}

	static SourceText NestedModelAttribute()
	{
		var writer = CreateWriter(
			nameof(AttributeDataModelGenerator),
			GeneratorTypeLibrary.Attirbutes.NestedModelAttribute
		);
		return writer
			.XmlSummary("Marks a record parameter as a nested generated attribute-data model.")
			.WriteAttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.NestedModelAttribute),
				AttributeTargets.Parameter,
				bodyWriter => bodyWriter.Comment("Empty")
			);
	}

	static SourceText ExcludeAttribute()
	{
		var writer = CreateWriter(
			nameof(AttributeDataModelGenerator),
			GeneratorTypeLibrary.Attirbutes.ExcludeAttribute
		);
		return writer
			.XmlSummary("Excludes a record parameter from the generated attribute-data model.")
			.WriteAttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.ExcludeAttribute),
				AttributeTargets.Parameter,
				bodyWriter => bodyWriter.Comment("Empty")
			);
	}

	static SourceText GenericTypeArgumentAttribute()
	{
		var writer = CreateWriter(
			nameof(AttributeDataModelGenerator),
			GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute
		);

		return writer
			.XmlSummary("Marks a record parameter as a generic type argument of the attribute class.")
			.WriteAttributeClass(
				new(GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute),
				AttributeTargets.Parameter,
				bodyWriter =>
				{
					bodyWriter
						.XmlSummary("Initializes a new instance marking the first type argument.")
						.WriteConstructor(
							new(GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
							},
							constructorWriter => constructorWriter.Comment("Empty")
						);

					bodyWriter
						.XmlSummary("Initializes a new instance marking the type argument at the specified index.")
						.WriteConstructor(
							new(GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								Parameters = [new("index", PurviewTypeLibrary.System.Int32)],
							},
							constructorWriter => constructorWriter.WriteLine("Index = index;")
						);

					bodyWriter
						.XmlSummary(
							"Initializes a new instance marking the type argument with the specified type parameter name."
						)
						.WriteConstructor(
							new(GeneratorTypeLibrary.Attirbutes.GenericTypeArgumentAttribute)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								Parameters = [new("name", PurviewTypeLibrary.System.String)],
							},
							constructorWriter =>
								constructorWriter.WriteLine(
									"Name = name ?? throw new global::System.ArgumentNullException(nameof(name));"
								)
						);

					bodyWriter
						.XmlSummary("Gets or sets the type parameter name.")
						.WriteProperty(
							new("Name", PurviewTypeLibrary.System.String.MakeNullable())
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
							}
						);

					bodyWriter
						.XmlSummary("Gets or sets the type argument index.")
						.WriteProperty(
							new("Index", PurviewTypeLibrary.System.Int32)
							{
								Accessibility = TypeDeclarationAccessibility.Public,
								IsInitOnly = true,
								Initializer = "-1",
							}
						);
				}
			);
	}
}
