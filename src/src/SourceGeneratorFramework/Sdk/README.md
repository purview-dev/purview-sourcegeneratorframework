# Purview.SourceGeneratorFramework

Core helpers, models, and MSBuild integration for writing incremental C# source generators with Roslyn.

## Referencing a generator project

Roslyn must receive both a source-generator assembly and its framework runtime dependency as
analyzer inputs. Use an analyzer project reference:

```xml
<ProjectReference
	Include="..\MyGenerator\MyGenerator.csproj"
	PrivateAssets="all"
	OutputItemType="Analyzer"
	ReferenceOutputAssembly="false"
/>
```

The Purview SDK automatically invokes `GetSourceGeneratorAnalyzerFiles`, which returns both the
generator and its framework dependency without adding either file to the consuming application's
runtime references. Specifying `Targets="GetSourceGeneratorAnalyzerFiles"` explicitly remains
supported but is not required.

### Referencing a generator from its test project

A test project can need the source-generator project in two different roles at the same time:

- as an analyzer, so the generator runs against the test project and its generated attributes and
  other types can be used directly by test source files; and
- as a normal assembly reference, so the test code can name and instantiate the generator type
  through `Purview.SourceGeneratorFramework.Testing`.

Add two project references with deliberately different metadata:

```xml
<ItemGroup>
  <!-- Run the generator against this test project. -->
  <ProjectReference
    Include="..\MyGenerator\MyGenerator.csproj"
    PrivateAssets="all"
    OutputItemType="Analyzer"
    ReferenceOutputAssembly="false"
  />

  <!-- Make MyGenerator available to the test code and test runner. -->
  <ProjectReference
    Include="..\MyGenerator\MyGenerator.csproj"
    PrivateAssets="all"
    ReferenceOutputAssembly="true"
  />
</ItemGroup>
```

Do not put `OutputItemType="Analyzer"` on the normal reference. The Purview SDK automatically
uses `GetSourceGeneratorAnalyzerFiles` for the analyzer reference and supplies the generator's
runtime dependencies to Roslyn.

Because the second reference is a normal assembly reference, the generator's Roslyn dependencies
also become visible to the test compilation. For a multi-target test project, build the generator
against the oldest Roslyn version that supports its API usage and is compatible with the oldest
test target. This framework supports Roslyn 4.13; prefer
`IncrementalGeneratorInitializationContext.RegisterEmbeddedAttribute(...)` over Roslyn 4.14's
`IncrementalGeneratorPostInitializationContext.AddEmbeddedAttributeDefinition()` when the tests
must also target .NET 8. Do not centrally pin `System.Collections.Immutable` to a newer runtime
version merely to make the generator load.

## Installation

```bash
dotnet add package Purview.SourceGeneratorFramework
```

## What's included

- **`CodeWriter`** — allocation-conscious helper for building generated C# source files with indentation, namespaces, type declarations, comments, and XML documentation.
- **`IncrementalPipeline`** — extension methods for composing `IncrementalValueProvider<T>` and `IncrementalValuesProvider<T>` pipelines, including attribute-based discovery, generation context creation, and disable-property checks.
- **`GenerationContext`** — a base execution-services context that carries the Roslyn `Compilation`, immutable generator settings, optional logging, and a factory for independently owned `CodeWriter` instances.
- **`GeneratorResult<T>`** — a value-or-diagnostics result type for incremental source generator transforms.
- **`TypeValueObject`**, **`TargetSymbolDescriptor`**, **`EquatableArray<T>`**, **`DiagnosticInfo`** — reusable models for generator inputs and outputs.
- **`SymbolResolver`**, **`TypeHelpers`**, **`EmbeddedResources`** — helper classes for common symbol and resource tasks.
- **`AttributeDataModelGenerator`** — bundled source generator that emits `readonly record struct` attribute parser models from `[GenerateAttributeDataModel]` declarations, eliminating repetitive `FromAttributeData` boilerplate. Supports manual mapping, auto-discovery, nested models, and inheritance matching.
- **Bundled Roslyn analyzers** — `Purview.SourceGeneratorFramework.Analyzers` ships as an analyzer asset inside the `Purview.SourceGeneratorFramework` package and reports diagnostics such as `PSGFR11` (prefer `ForAttributeWithMetadataName`), `PSGFR12` (use `IIncrementalGenerator`), and `PSGFR14` (avoid `RegisterImplementationSourceOutput`).
- **MSBuild `.props` / `.targets`** — automatically adds `global using` directives for the main namespaces and supports packaging source generators that reference this framework.

## Usage

Reference the package from a Roslyn source generator project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IsRoslynComponent>true</IsRoslynComponent>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Purview.SourceGeneratorFramework" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Implement `IIncrementalGenerator` and use the framework helpers to build a pipeline:

```csharp
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Helpers;
using Purview.SourceGeneratorFramework.Models;

[Generator]
public sealed class MyGenerator : IIncrementalGenerator
{
    static readonly TypeValueObject AttributeType = new("MyAttribute", "MyNamespace");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var contextProvider = IncrementalPipeline.DefaultGenerationContextValueProvider(
            context,
            nameof(MyGenerator),
            "1.0.0"
        );

        var targets = IncrementalPipeline.ForAttributeWithMetadataName(
            context,
            AttributeType,
            static (ctx, ct) => ctx.TargetSymbol.Name
        );

        context.RegisterSourceOutput(
            targets.CombineWithContext(contextProvider),
            static (spc, pair) =>
            {
                var (name, generationContext) = pair;
                var writer = generationContext.CreateCodeWriter();
                writer.WriteAutoGeneratedHeader();
                writer.WriteFileScopedNamespace("MyNamespace");
                using (
                    writer.WriteClassScope(
                        new TypeDeclarationOptions(name)
                        {
                            Accessibility = TypeDeclarationAccessibility.Public,
                            IsStatic = true,
                        }
                    )
                )
                {
                    writer.WriteLine("// generated content");
                }

                spc.AddSource($"{name}.g.cs", writer.ToString());
            }
        );
    }
}
```

See [`SourceGeneratorFramework.ExampleGenerator`](../SourceGeneratorFramework.ExampleGenerator) for a complete reference implementation.

## Generated attributes and determinism

`CodeWriter` automatically stamps generated declarations with `[GeneratedCode]`, `[CompilerGenerated]`, and `[ExcludeFromCodeCoverage]` (where applicable) using the generator identity supplied to its constructor. The header written by `WriteAutoGeneratedHeader()` is deterministic and does not include a timestamp, so the same inputs always produce the same source.

### The `#nullable enable` directive

`WriteAutoGeneratedHeader()` emits `#nullable enable` according to a `NullableDirectiveMode`. The same mode also controls whether nullable *reference* annotations are rendered by type writing, so the directive and the emitted annotations always agree:

- `Auto` (default) — the framework reads the target compilation's nullable context when the pipeline creates the generation context and emits the directive only when nullable annotations are enabled. When the state is unknown (for example in post-initialization outputs or tests), the directive is still emitted. An explicitly configured `GenerationSettings.IsNullableContextEnabled` value takes precedence over the compilation's state.
- `Always` — always emit `#nullable enable` and always render nullable reference annotations, even when the target compilation disables nullable.
- `Disable` — never emit the directive and never render nullable reference annotations.

Override it per call, or set a generator-wide default on `GenerationSettings`:

```csharp
writer.WriteAutoGeneratedHeader(nullableDirective: NullableDirectiveMode.Disable);

var settings = GenerationSettings.Create<MyGenerator>() with
{
    NullableDirectiveMode = NullableDirectiveMode.Always,
};
```

If your generated output must use nullable reference annotations regardless of the consuming project's `<Nullable>` setting, configure `NullableDirectiveMode.Always` (or set `IsNullableContextEnabled = true`) on the `GenerationSettings` passed into the pipeline; the configured value is honoured instead of the compilation's state.

### Nullable reference annotations in generated types

`TypeReference.Nullable()` and `TypeIdentity.MakeNullable()` produce nullable annotations. When the target compilation does not support nullable, a nullable *reference* annotation (`string?`) is invalid outside a nullable context and is elided, while a nullable *value* type (`int?`) is always emitted.

Two mechanisms cooperate:

- **Context-aware composition** — pass the available `GenerationSettings` or `CodeWriter` to only append the annotation when nullable is enabled or unknown:

```csharp
writer.WriteType(PurviewTypeLibrary.System.String.MakeNullable(writer)); // elides "?" when nullable is off
```

- **Context-aware rendering** — the writer elides reference annotations when it renders with nullable disabled. `WriteType(TypeReference)` renders a bare reference using the writer's nullable context, and `RenderFullNameForNullable(bool)` exposes the same behavior for direct string building:

```csharp
writer.WriteType(TypeIdentity.Create<string>().MakeNullable()); // "string" when nullable is off, "string?" when on
```

Composition and rendering both resolve `NullableDirectiveMode` together with `IsNullableContextEnabled`, so an `Always` mode keeps the `?` even for a nullable-disabled target, and a `Disable` mode strips it even when the target enables nullable.

The analyzer `PSGFR16` (suggestion) flags bare `Nullable()`/`MakeNullable()` calls and its code fix passes the first in-scope `CodeWriter` or `GenerationSettings`, including project-wide "Fix all" support.

### Comparing references with or without annotations

`Equals`/`==` on `TypeReference`/`TypeIdentity` is structural, so `IEnumerable<KeyValuePair<string, object>>` does not equal `IEnumerable<KeyValuePair<string, object?>>`. Mixed comparisons (`TypeIdentity == TypeReference`) are also structural — a reference equals an identity only when it is an unmodified reference to that identity. When a nullable *reference* annotation should be treated as metadata, use `Similar`, which ignores provable reference annotations (but keeps nullable *value* types significant, so `int?` is never similar to `int`):

```csharp
reference1.Similar(reference2);        // reference to reference
reference.Similar(symbol);             // reference to ISymbol/ITypeSymbol (uses the same matching as Matches)
```

### XML documentation `cref` names

XML documentation references generic types with `{}` instead of `<>`. Use `XmlCommentWriter.ToXmlCref` or the `XmlCref`/`XmlException`/`XmlSee`/`XmlSeeAlso` overloads that accept a `TypeIdentity`/`TypeReference`:

```csharp
writer.XmlCref(new TypeIdentity(typeof(List<>)).MakeGeneric(TypeIdentity.Create<string>()), "content");
// /// <cref cref="global::System.Collections.Generic.List{global::System.String}">content</cref>
```

## Thin source-output registration

For per-target pipelines that return `GeneratorResult<T>`, use `IncrementalPipeline.RegisterSourceOutput` to combine targets with the generation context, report diagnostics, and run the generator callback only for successful results:

```csharp
var targets = IncrementalPipeline.ForAttributeWithMetadataName(
    context,
    AttributeType,
    static (ctx, ct) =>
    {
        var symbol = ctx.TargetSymbol;
        return symbol is null
            ? GeneratorResult<string>.Empty
            : GeneratorResult<string>.Ok(symbol.Name);
    }
);

var contextProvider = IncrementalPipeline.DefaultGenerationContextValueProvider(
    context,
    nameof(MyGenerator),
    "1.0.0"
);

IncrementalPipeline.RegisterSourceOutput(
    context,
    targets,
    contextProvider,
    static (spc, name, generationContext) =>
    {
        var writer = generationContext.CreateCodeWriter();
        writer.WriteLine($"// generated {name}");
        spc.AddSource($"{name}.g.cs", writer.ToString());
    }
);
```

## Keep CodeWriter out of incremental contexts

Treat `GenerationContext` values as cached incremental-pipeline state and each `CodeWriter` as
mutable, output-scoped execution state. Create the writer inside the registered source-output
callback, after the incremental cache boundary. Creating it in the callback and passing it to
emitter/helper methods called from that same callback is the intended pattern; the only thing that
is forbidden is persisting the writer in pipeline state, where Roslyn caches it:

```csharp
IncrementalPipeline.RegisterSourceOutput(
    context,
    targets,
    contextProvider,
    static (spc, target, generationContext) =>
    {
        var writer = generationContext.CreateCodeWriter();
        EmitTarget(generationContext, writer, target);
        spc.AddSource($"{target.Name}.g.cs", writer.ToString());
    }
);
```

This separation is intentional:

- Roslyn caches the complete value published by an incremental provider. It does not provide a way
  to exclude one property of that value from caching.
- `CodeWriter` is mutable. Caching one can retain previously written source when the context is
  reused for another output or generator run.
- Source-output callbacks may process independent targets concurrently. Sharing a writer can mix
  their output and introduce data races.
- A fresh writer gives each generated source independent scope tracking and deterministic ownership.

These rules also apply to custom contexts: **never add or assign a `CodeWriter` property or field on
a class derived from `GenerationContext`**. A custom context is still produced by an incremental
provider and cached as one complete value. Store only compilation-derived services and immutable
configuration there, and call `CreateCodeWriter()` in the output callback.

When emitter methods need both logging/context services and writing, either pass the context and
output-scoped writer separately, or compose them into a short-lived output wrapper created inside
the callback. Such a wrapper must never be returned from an incremental provider:

```csharp
public sealed class GenerationOutputContext<TContext> : ISourceGenLogger
    where TContext : GenerationContext
{
    public GenerationOutputContext(TContext generation)
    {
        Generation = generation;
        Writer = generation.CreateCodeWriter();
    }

    public TContext Generation { get; }
    public CodeWriter Writer { get; }

    public void Log(
        SourceGenLogLevel level,
        int indentation,
        string message,
        params object[] args) =>
        Generation.Log(level, indentation, message, args);
}
```

The wrapper reduces emitter parameter noise without extending the writer's lifetime into Roslyn's
incremental cache.

## Attribute model generation

The package includes `AttributeDataModelGenerator`, which generates `readonly record struct` parser models for .NET attributes. Instead of hand-writing `FromAttributeData` methods for every attribute you inspect, declare a `readonly partial record struct` with `[GenerateAttributeDataModel]` and let the generator fill in the `Empty` sentinel, `FromAttributeData` overloads, and property extraction logic.

```csharp
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Generators;
using System.ComponentModel.DataAnnotations;

namespace MySourceGenerator.Models;

[GenerateAttributeDataModel(typeof(ValidationAttribute), MatchByInheritance = true)]
public readonly partial record struct ValidationAttributeData(
    [Property] string? ErrorMessage,
    [Property] string? ErrorMessageResourceName,
    [Property] ITypeSymbol? ErrorMessageResourceType
);

[GenerateAttributeDataModel(typeof(RequiredAttribute))]
public readonly partial record struct RequiredAttributeData(
    [Property] bool AllowEmptyStrings,
    [NestedModel] ValidationAttributeData ValidationAttribute
);
```

Supported mapping attributes:
- `[Property]` — reads a named attribute property (the property name is inferred from the parameter name unless overridden with `Name = ...`).
- `[Argument]` — reads a constructor argument by parameter name.
- `[Argument(int index)]` — reads a constructor argument by position.
- `[NestedModel]` — populates a nested `[GenerateAttributeDataModel]` type.
- `[GenericTypeArgument]` — reads a generic type argument of the attribute class.

You can also target an attribute by fully-qualified name, which is useful when the attribute type is not available in the generator project (e.g., `LengthAttribute` in .NET 8+ or a self-generated attribute):

```csharp
[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.RequiredAttribute")]
public readonly partial record struct RequiredAttributeData(
    [Property] bool AllowEmptyStrings
);
```

Enable auto-discovery with `[GenerateAttributeDataModel(typeof(MyAttribute), AutoDiscover = true)]` to generate properties for every constructor parameter and public named property. Auto-discovery requires the `Type` overload. Override defaults with `[Property(DefaultValue = ...)]` or `[Argument(DefaultValue = ...)]`, or rely on inferred defaults from optional constructor parameters.

See [`SourceGeneratorFramework.Generators`](../SourceGeneratorFramework.Generators) for full documentation and additional examples.

## Generic type identities and references

`TypeIdentity` distinguishes an open generic definition from a constructed generic type:

```csharp
var openDictionary = new TypeIdentity(typeof(Dictionary<,>));

var stringToIntDictionary = openDictionary.MakeGeneric(
    new TypeIdentity(typeof(string)),
    new TypeIdentity(typeof(int))
);

TypeReference openReference = openDictionary.AsTypeReference();
TypeReference typedReference = stringToIntDictionary.AsTypeReference();
```

Use the open identity when any construction is acceptable. Its `Matches(ITypeSymbol)` method—and
`TypeHelpers.Is`/`IsDerivedFromExpectedBase`—matches symbols such as `Dictionary<string, int>` and
`Dictionary<Guid, Widget>` by generic definition and arity. Use the constructed identity when the
arguments matter.

Structural equality remains exact: an open identity is not equal to a constructed identity, and
`Dictionary<string, int>` is not equal to `Dictionary<string, long>`. Symbol matching is
deliberately asymmetric: an open expected identity can match a constructed symbol.

String arguments to `MakeGeneric` are literal type names, not wildcards:

```csharp
// Describes ResourceKitBase<TResource>, where the argument is literally named TResource.
var resourceKitBase = new TypeIdentity("ResourceKitBase", "Example");
var parameterized = resourceKitBase.MakeGeneric("TResource");

// Describes the open List<> definition and matches any List<T> construction.
var openList = new TypeIdentity(typeof(List<>));
```

For contract-aware comparisons, a constructed expected argument may be an interface or base type.
`TypeHelpers.Is` and the symbol overload of `IsDerivedFromExpectedBase` accept an actual generic
argument that implements or inherits from that expected contract. The syntax-only overload cannot
inspect semantic relationships; it compares only the declared base type name.

`TypeReference` adds use-site composition—nullable annotations, arrays, pointers, generic
parameters and nested constructions—around a `TypeIdentity`. It preserves the identity's generic
matching behavior, but its modifiers must also match.

## Structured member declarations

Methods, properties, fields, and constructors use immutable value-type declaration options. The
descriptor itself does not allocate an object; strings and `ImmutableArray` values are references
owned by the caller.

```csharp
using (writer.WriteMethodScope(
    new MethodDeclarationOptions(
        "CreateAsync",
        new TypeReferenceOptions("Task").MakeGeneric(new TypeReferenceOptions("Result"))
    )
    {
        Accessibility = TypeDeclarationAccessibility.Public,
        IsStatic = true,
        IsAsync = true,
        Parameters =
        [
            new("request", new TypeReferenceOptions("Request")),
            new("cancellationToken", new TypeReferenceOptions("CancellationToken")),
        ],
    }))
{
    writer.WriteLine("return await ExecuteAsync(request, cancellationToken);");
}

writer.WriteProperty(
    new PropertyDeclarationOptions("Name", new TypeReferenceOptions("string"))
    {
        Accessibility = TypeDeclarationAccessibility.Public,
        HasSetter = true,
        IsInitOnly = true,
        Initializer = "string.Empty",
    }
);

writer.WriteField(
    new FieldDeclarationOptions("Instance", "Service")
    {
        Accessibility = TypeDeclarationAccessibility.Private,
        IsStatic = true,
        IsReadOnly = true,
        Initializer = "new()",
    }
);
```

`WriteMethod` folds long parameter lists automatically. `WriteProperty` supports automatic
accessors, expression bodies, and callback-generated getter/setter bodies. Structured methods and
constructors return a disposable body scope; callback overloads are available when a complete
member should be written in one call.

`TypeDeclarationOptions.Kind` supports classes, structs, record classes, record structs,
interfaces, enums, and delegates. The matching `WriteInterface`, `WriteEnum`, and `WriteDelegate`
helpers set the kind automatically. Interface inheritance is supplied through `Interfaces`, enums
can specify `EnumUnderlyingType`, and delegates use `DelegateReturnType` and
`DelegateParameters`. Generic delegate and interface constraints use the existing `GenericTypes`
model.

Attributes and parameters are structured as well; raw declaration fragments are not accepted:

```csharp
new MethodDeclarationOptions("TryGet", "bool")
{
    Accessibility = TypeDeclarationAccessibility.Public,
    Attributes = [new("Obsolete")],
    ReturnAttributes = [new("NotNull")],
    Parameters =
    [
        new("value", "string?")
        {
            Modifier = ParameterModifier.Out,
            Attributes =
            [
                new("NotNullWhen")
                {
                    Arguments = [new("true")],
                },
            ],
        },
    ],
};
```

Every type, method, constructor, property, and field declaration exposes `Attributes`. Methods also
expose `ReturnAttributes`; parameters expose their own `Attributes`. `AttributeArgumentOptions`
supports positional arguments, constructor-named arguments using `Name`, and property assignments
using `Name` with `IsPropertyAssignment = true`.

All declaration type positions use `TypeReferenceOptions`. Nullability is therefore composed rather
than embedded in a type string:

```csharp
var widget = new TypeReferenceOptions("Widget").Nullable();
var result = new TypeReferenceOptions("global::System.Collections.Generic.Dictionary")
    .MakeGeneric(new TypeReferenceOptions("string"), widget)
    .MakeArray()
    .Nullable();

new ParameterDeclarationOptions(
    "items",
    new TypeReferenceOptions("global::System.Collections.Generic.List")
        .MakeGeneric(widget)
)
{
    IsNullable = true,
    DefaultValue = "null",
};
```

For parameters, `IsNullable = true` is a convenience equivalent to calling `.Nullable()` on the
parameter's `TypeReferenceOptions`. If both are used, only one nullable annotation is emitted.

`TypeReferenceOptions` supports nullable annotations, nested constructed generics, open generic
arity, multidimensional and jagged arrays, pointers, and construction from `Type`, Roslyn
`ITypeSymbol`, or `TypeValueObject`. Arbitrary expressions such as default values and initializers
remain strings because they are expressions rather than type syntax.

Set `TypeDeclarationOptions.IsAbstract` for abstract classes or record classes. It takes precedence
over the default `IsSealed = true`, so callers do not need to disable sealing explicitly. Abstract
static classes and abstract non-class declarations are rejected.

Roslyn accessibility values can be converted in both directions:

```csharp
TypeDeclarationAccessibility? declarationAccessibility =
    symbol.DeclaredAccessibility.ToTypeDeclarationAccessibility();

Accessibility roslynAccessibility =
    TypeDeclarationAccessibility.ProtectedInternal.ToRoslynAccessibility();
```

Both conversions are non-throwing. Roslyn `NotApplicable` maps to `null`; declaration `File` and
unknown future values map to Roslyn `NotApplicable`, because Roslyn models file-local types
separately from `Accessibility`.

Member spacing is tracked automatically at each declaration level:

- Consecutive fields are grouped without a blank line.
- A field followed by any other member has one blank line between them.
- Methods, constructors, properties, and nested types are separated from the following member by
  one blank line.
- An existing blank line is retained without adding another one.

Body-bearing declarations are registered when their returned scope is disposed. This means the
next member is formatted correctly only after the preceding method, constructor, or type has been
closed. If XML documentation or attributes were written after the previous member, the separator
is inserted before that trivia so it remains attached to the declaration it documents.

## Detecting undisposed CodeWriter scopes

`CodeWriter` can detect block or indentation scopes that have not been disposed before generated source is materialized. This validation is intended for development and automated tests and is disabled by default.

Enable it in the project consuming the source generator:

```xml
<PropertyGroup>
  <PurviewSourceGeneratorFrameworkValidateCodeWriterScopes>true</PurviewSourceGeneratorFrameworkValidateCodeWriterScopes>
</PropertyGroup>
```

The default generation-context provider reads the property automatically:

```csharp
var contextProvider =
    IncrementalPipeline.DefaultGenerationContextValueProvider(
        context,
        nameof(MyGenerator),
        "1.0.0"
    );
```

Create a fresh writer through the generation context so it inherits the setting:

```csharp
var writer = generationContext.CreateCodeWriter();
```

`CreateCodeWriter()` returns a new, independently owned instance on every call. The writer is not
stored on `GenerationContext`; keep it scoped to the source-output operation that owns the generated
source. `CodeWriter.ThrowOnUnclosedScopes` is read-only, and its configuration is supplied through
the writer constructor by the context factory.

When validation is enabled, calling `ToString()` or implicitly converting a writer to Roslyn `SourceText` throws a `CodeWriterScopeValidationException` if `OpenScopeCount` is not zero. The dedicated exception allows generator error handlers to rethrow this framework invariant failure instead of reducing it to a generic generator diagnostic:

```text
Cannot create generated source while 1 disposable scope(s) remain open. Dispose every scope before calling ToString().

Open scope #1: block — public sealed class Example
   at MyGenerator.Generate(...)
```

The exception's `OpenScopes` collection exposes the scope kind, block header, and opening stack
trace programmatically. Stack traces are captured only when validation is enabled, avoiding this
diagnostic allocation during normal generator execution.

Both `BlockScope` and `IndentScope` are tracked. Prefer `using` or callback-based blocks so scopes are always closed:

```csharp
writer.WriteBlock(
    "if (value is null)",
    body => body.WriteLine("return;")
);
```

### Custom generation contexts

Scope validation is applied to every context returned by `GenerationContextValueProvider`. Custom
contexts do not need to accept or read the build property themselves:

```csharp
public sealed class MyGenerationContext : GenerationContext
{
    public MyGenerationContext(
        Compilation compilation,
        GenerationSettings settings,
        ISourceGenLogger? logger)
        : base(compilation, settings, logger)
    {
    }
}
```

Do not add a `CodeWriter` to `MyGenerationContext`. Custom contexts have the same incremental-cache
lifetime as the default context, so a writer stored on one can be reused across independent outputs.

Use the ordinary context-provider overload. The framework combines the compiler-visible property
with the compilation and supplies the resulting immutable settings to the custom context factory:

```csharp
var contextProvider = IncrementalPipeline.GenerationContextValueProvider(
    context,
    nameof(MyGenerator),
    "1.0.0",
    factory: static (compilation, settings, logger, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new MyGenerationContext(compilation, settings, logger);
    },
    disablePropertyName: "MyGenerator_Disable"
);
```

The provider resolves scope validation, generator disabling, and test logging from analyzer-config
properties before invoking the factory. The supplied logger is created internally only when logging
is enabled and a sink is registered for that run.

### Generators embedded in another package

If the generator assembly is embedded in a different NuGet package, the outer package must make the property compiler-visible to its consumers. Build assets from `Purview.SourceGeneratorFramework` are not automatically copied into the outer package.

Include this in a `.props` file imported by the outer package:

```xml
<Project>
  <PropertyGroup>
    <PurviewSourceGeneratorFrameworkValidateCodeWriterScopes
      Condition="'$(PurviewSourceGeneratorFrameworkValidateCodeWriterScopes)' == ''"
    >false</PurviewSourceGeneratorFrameworkValidateCodeWriterScopes>
  </PropertyGroup>

  <ItemGroup>
    <CompilerVisibleProperty Include="PurviewSourceGeneratorFrameworkValidateCodeWriterScopes">
      <Description>Throws when generated source is materialized while CodeWriter scopes remain undisposed.</Description>
    </CompilerVisibleProperty>
  </ItemGroup>
</Project>
```

Pack that file using the outer package's ID so NuGet imports it automatically:

```xml
<None
  Include="Sdk\Sdk.props"
  Pack="true"
  PackagePath="buildTransitive\$(PackageId).props"
  Visible="false"
/>
```

Framework-based generator tests enable scope validation by default. Disable it for a test only
when partial source materialization is intentional:

```csharp
new SourceGeneratorTestOptions
{
    ValidateCodeWriterScopes = false,
};
```

## Disabling a generator at build time

Pass the generator's compiler-visible disable property to the context provider. Its resolved value is
included in `GenerationSettings` automatically:

```xml
<PropertyGroup>
  <MyGenerator_Disable>true</MyGenerator_Disable>
</PropertyGroup>
```

```csharp
var contextProvider = IncrementalPipeline.DefaultGenerationContextValueProvider(
    context,
    nameof(MyGenerator),
    "1.0.0",
    disablePropertyName: "MyGenerator_Disable"
);

// In the output stage:
if (generationContext.Settings.IsSourceGeneratorDisabled)
    return;
```

`IsDisabledValueProvider` remains available when expensive upstream transforms must be filtered
before they are combined with the generation context.

## Test logging

Framework logging is disabled in ordinary compiler runs. The testing integration enables it by
registering an isolated sink and supplying a per-run session ID through analyzer config. Context
providers create the internal logger automatically; generators do not implement a logging interface
and no logging-support source is generated.

The sink registry stores callbacks only. It never buffers log entries. If logging is disabled, the
session ID is missing, or no matching sink is registered, the provider supplies no logger and log
calls are discarded without storing entries. Test sinks own any entries they choose to capture and
are removed when the test run completes.

## Querying generated and fixed code

Every test result exposes a `CodeQuery` so tests can locate syntax nodes in the produced code. Queries
default to generated code first for source-generator runs, with a `Get`/`Has`/`TryGet` pairing (`Get`
throws `SyntaxNotFoundException` when nothing matches; `Has` returns `bool`).

```csharp
// Source generators — generated trees first, or the whole output compilation.
var method = result.Generated.GetMethod("DoWork");       // MethodDeclarationSyntax (throws if absent)
bool hasMethod = result.Generated.HasMethod("DoWork");   // true/false
result.Generated.TryGetMethod("DoWork", out var maybe);
var inOutput = result.Output.GetClass("Generated_Service");   // full output compilation

// Match parameter types using TypeReference, resolving through the compilation's semantic model.
result.Generated.HasMethod("DoWork", TypeReference.Create<int>(), TypeReference.Create<int>().Nullable(), complexType);
result.Generated.HasReturnType("Compute", TypeReference.Create<int>());
result.Generated.GetMethod("Format").HasParameters(query, TypeReference.Create<string>(), objectReference);

// Other declaration kinds.
result.Generated.GetClass("X"); result.Generated.HasClass("X");
result.Generated.GetProperty("P"); result.Generated.HasField("_f");
result.Generated.GetInterface("I"); result.Generated.GetEnum("E");
result.Generated.GetTypeDeclaration("Record");
result.Generated.GetNamespace("Example.Nested");
result.Generated.GetSyntaxTree("Service.g.cs"); result.Generated.Has<T>(predicate);

// Types can be located in any namespace or a specific one.
result.Generated.GetClass("Widget");                 // anywhere
result.Generated.GetClass("Widget", "Example.Models"); // within a namespace

// Chain from a type declaration to inspect its members, matching return/property/parameter types.
var service = result.Generated.GetClass("ServiceCollectionExtensions"); // or any namespace
service.HasProperty(query, "Count", intType);                          // property + type
service.HasIndexer(query, stringType, intType);                        // indexer + return + index param
service.HasMethod(query, "Add", intType, complexType);                 // method + parameter types
service.HasMethodReturnType(query, "Add", stringType);                 // method + return type
service.HasConstructor(query, stringType);                             // ctor + parameter types
service.GetMethod(query, "Add").HasReturnType(query, stringType);
service.GetProperty(query, "Name").HasType(query, stringType);
```

Analyzer and code-fix results expose the same API:

```csharp
analyzerResult.Code.HasMethod("M");                 // input compilation
codeFixResult.FixedCode.HasMethod("M");             // parsed fixed source
fixAllResult.FixedCode.HasClass("X");               // post-fix solution documents
refactorResult.FixedCode.HasMethod("M");            // post-refactor documents
```

Refactoring tests run a `CodeRefactoringProvider` through `TUnitRefactoringTestBase`, selecting the
trigger node with a `NodeSelector` (or an explicit `Span`):

```csharp
public class MyRefactoringTests : TUnitRefactoringTestBase<MyRefactoringProvider>
{
    [Test]
    public Task AddsAttribute(CancellationToken ct) => RefactorAsync(
        source,
        new RefactorTestOptions
        {
            NodeSelector = query => query.GetMethod("DoWork"),
            EquivalenceKey = MyRefactoringProvider.EquivalenceKey,
        },
        ct);
}
```

TUnit assertion extensions return the requested syntax node when awaited:

```csharp
MethodDeclarationSyntax method = await Assert.That(result).HasGeneratedMethod("DoWork");
await Assert.That(method.Identifier.ValueText).IsEqualTo("DoWork");

var method2 = await Assert.That(result).HasGeneratedMethod("DoWork", [intType, nullableInt, complexType]);
ClassDeclarationSyntax cls = await Assert.That(result).HasGeneratedClass("Service");
FieldDeclarationSyntax field = await Assert.That(result).HasGeneratedField("Name");
SyntaxTree tree = await Assert.That(result).HasGeneratedSyntaxTree("Service.g.cs");

// Code fix / refactoring results:
var fixedMethod = await Assert.That(codeFixResult).HasFixedMethod("DoWork");
var refactoredMethod = await Assert.That(refactorResult).HasFixedMethod("DoWork");
```

## Analyzers

The `Purview.SourceGeneratorFramework` package includes the `Purview.SourceGeneratorFramework.Analyzers` assembly as an analyzer asset. The diagnostics are enabled automatically when you reference `Purview.SourceGeneratorFramework` from a source generator project.

| Rule | Summary |
|------|---------|
| `PSGFR11` | Prefer `SyntaxProvider.ForAttributeWithMetadataName` over `CreateSyntaxProvider` for attribute-based detection. |
| `PSGFR12` | Use `IIncrementalGenerator` / `RegisterSourceOutput` instead of `ISourceGenerator`. |
| `PSGFR14` | Avoid `RegisterImplementationSourceOutput` unless implementation-only output is required. |

## License

This project is licensed under the MIT license.
