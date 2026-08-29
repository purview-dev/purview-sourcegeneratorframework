namespace Purview.SourceGeneratorFramework.Testing;

/// <summary>
/// Creates <see cref="CodeWriter"/> instances for tests, supplying a default generator identity
/// so the production constructor can require one without test boilerplate.
/// </summary>
public static class CodeWriterFactory
{
	const string DefaultGeneratorName = "TestGenerator";
	const string DefaultGeneratorVersion = "1.0.0";

	/// <summary>
	/// Creates a <see cref="CodeWriter"/> with a default test identity and validation enabled.
	/// </summary>
	public static CodeWriter ForTests(
		int initialCapacity = 4096,
		bool throwOnUnclosedScopes = true,
		GenerationSettings? settings = null
	) =>
		new(
			settings ?? new(DefaultGeneratorName, DefaultGeneratorVersion),
			initialCapacity: initialCapacity,
			throwOnUnclosedScopes: throwOnUnclosedScopes
		);
}
