namespace Purview.SourceGeneratorFramework.Examples;

/// <summary>
/// Generates service registration extension methods for types annotated with <see cref="GenerateServiceAttribute"/>.
/// </summary>
[Generator]
public partial class ServiceRegistrationGenerator : IIncrementalGenerator
{
	const string GeneratorName = "ServiceRegistrationGenerator";
	const string GeneratorVersion = "1.0.0";

	/// <summary>
	/// Initializes the generator pipeline.
	/// </summary>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterEmbeddedAttribute(GeneratorName, GeneratorVersion);

		context.RegisterPostInitializationOutput(spc =>
			ServiceRegistrationEmitter.EmitAttributeAndEnum(spc, GeneratorName, GeneratorVersion)
		);

		var isDisabled = IncrementalPipeline.IsDisabledValueProvider(
			context,
			ServiceRegistrationGeneratorPropertyLibrary.DisableServiceRegistrationGenerator
		);

		var emitServiceInfo = IncrementalPipeline.IsDisabledValueProvider(
			context,
			ServiceRegistrationGeneratorPropertyLibrary.EmitServiceRegistrationInfo
		);

		var generationContext = IncrementalPipeline.GenerationContextValueProvider(
			context,
			GeneratorName,
			GeneratorVersion,
			(compilation, validateScopes, _, _, _) =>
				new GenerationContext(compilation, GeneratorName, GeneratorVersion, validateScopes),
			_logger
		);

		var targets = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			ServiceRegistrationGeneratorTypeLibrary.GenerateServiceAttribute,
			CreateServiceTarget
		);

		var model = generationContext
			.CollectWith(
				targets,
				(ctx, targetsArray, _) =>
					new ServiceRegistrationGenerationModel(ctx, new EquatableArray<ServiceTarget>(targetsArray))
			)
			.CombineWith(isDisabled, (m, disabled, _) => m with { IsDisabled = disabled })
			.CombineWith(emitServiceInfo, (m, emit, _) => m with { EmitServiceInfo = emit });

		context.RegisterSourceOutput(model, (spc, m) => ServiceRegistrationEmitter.Execute(spc, m, _logger));
	}

	static ServiceTarget CreateServiceTarget(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
	{
		var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.TargetNode, ct);
		if (symbol is null)
			return ServiceTarget.Empty;

		var attributeData = ctx.Attributes.FirstOrDefault(a =>
			ServiceRegistrationGeneratorTypeLibrary.GenerateServiceAttribute.Equals(a.AttributeClass)
		);
		if (attributeData is null)
			return ServiceTarget.Empty;

		var model = GenerateServiceAttributeData.FromAttributeData(attributeData);
		if (!model.Exists)
			return ServiceTarget.Empty;

		var lifetime = model.Lifetime ?? "Purview.SourceGeneratorFramework.Examples.ServiceLifetime.Singleton";
		var memberName = lifetime.Substring(lifetime.LastIndexOf('.') + 1);

		return new ServiceTarget(
			TypeName: TypeHelpers.ToFullyQualifiedDisplayString(symbol),
			ClassName: symbol.Name,
			Name: model.Name ?? symbol.Name,
			LifetimeMemberName: memberName
		);
	}
}

/// <summary>
/// Describes a discovered service target.
/// </summary>
readonly record struct ServiceTarget(string TypeName, string ClassName, string Name, string LifetimeMemberName)
{
	/// <summary>
	/// An empty <see cref="ServiceTarget"/>.
	/// </summary>
	public static readonly ServiceTarget Empty;
}

/// <summary>
/// Aggregated generation inputs for the service registration generator.
/// </summary>
readonly record struct ServiceRegistrationGenerationModel(
	GenerationContext Context,
	EquatableArray<ServiceTarget> Targets,
	bool IsDisabled = false,
	bool EmitServiceInfo = false
);
