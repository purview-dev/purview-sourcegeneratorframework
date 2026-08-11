using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Purview.SourceGeneratorFramework.Models;

/// <summary>
/// A serializable representation of a <see cref="Diagnostic"/> that can be carried through incremental source generator pipelines.
/// </summary>
public sealed record DiagnosticInfo(
	DiagnosticDescriptor Descriptor,
	string FilePath,
	TextSpan TextSpan,
	LinePositionSpan LinePositionSpan,
	ImmutableArray<LinePositionSpan> AdditionalLinePositions,
	EquatableArray<string> MessageArgs
)
{
	/// <summary>
	/// Converts this <see cref="DiagnosticInfo"/> back into a Roslyn <see cref="Diagnostic"/>.
	/// </summary>
	public Diagnostic ToDiagnostic()
	{
		var location = Location.Create(FilePath, TextSpan, LinePositionSpan);

		var args = MessageArgs;
		var objArgs = new object?[args.Count];
		for (var i = 0; i < args.Count; i++)
			objArgs[i] = args[i];

		return Diagnostic.Create(Descriptor, location, objArgs);
	}

	/// <summary>
	/// Creates a <see cref="DiagnosticInfo"/> from a descriptor and a target symbol descriptor.
	/// </summary>
	/// <param name="descriptor">The diagnostic descriptor.</param>
	/// <param name="messageArgs">The message arguments.</param>
	/// <returns>A <see cref="DiagnosticInfo"/> instance.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static DiagnosticInfo Create(
		DiagnosticDescriptor descriptor,
		params string[] messageArgs
	) => Create(descriptor, location: null, additionalLocations: null, messageArgs: messageArgs);

	/// <summary>
	/// Creates a <see cref="DiagnosticInfo"/> from a descriptor and a target symbol descriptor.
	/// </summary>
	/// <param name="descriptor">The diagnostic descriptor.</param>
	/// <param name="target">The target symbol descriptor.</param>
	/// <param name="messageArgs">The message arguments.</param>
	/// <returns>A <see cref="DiagnosticInfo"/> instance.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static DiagnosticInfo Create(
		DiagnosticDescriptor descriptor,
		TargetSymbolDescriptor target,
		params string[] messageArgs
	)
	{
		if (target is null)
			throw new ArgumentNullException(nameof(target));

		var location = target.Declaration?.GetLocation();
		ImmutableArray<Location> additionalLocations = [];
		if (location is null)
		{
			location = target.Symbol.Locations.FirstOrDefault(static loc => loc.IsInSource)
				is { } firstLocation
				? location = firstLocation
				: location = null;
			additionalLocations =
			[
				.. target.Symbol.Locations.Skip(1).Where(static loc => loc.IsInSource),
			];
		}
		else
		{
			additionalLocations = [.. target.Symbol.Locations.Where(static loc => loc.IsInSource)];
		}

		return Create(
			descriptor,
			location,
			additionalLocations: [.. additionalLocations],
			messageArgs
		);
	}

	/// <summary>
	/// Creates a <see cref="DiagnosticInfo"/> from a descriptor and optional location.
	/// </summary>
	/// <param name="descriptor">The diagnostic descriptor.</param>
	/// <param name="location">The location of the diagnostic.</param>
	/// <param name="messageArgs">The message arguments.</param>
	/// <returns>A <see cref="DiagnosticInfo"/> instance.</returns>
	public static DiagnosticInfo Create(
		DiagnosticDescriptor descriptor,
		Location? location,
		params string[] messageArgs
	) => Create(descriptor, location, additionalLocations: null, messageArgs);

	/// <summary>
	/// Creates a <see cref="DiagnosticInfo"/> from a descriptor and optional location.
	/// </summary>
	/// <param name="descriptor">The diagnostic descriptor.</param>
	/// <param name="location">The location of the diagnostic.</param>
	/// <param name="additionalLocations">Additional locations of the diagnostic.</param>
	/// <param name="messageArgs">The message arguments.</param>
	/// <returns>A <see cref="DiagnosticInfo"/> instance.</returns>
	public static DiagnosticInfo Create(
		DiagnosticDescriptor descriptor,
		Location? location,
		ImmutableArray<Location>? additionalLocations = null,
		params string[] messageArgs
	)
	{
		if (location is null)
		{
			return new DiagnosticInfo(
				Descriptor: descriptor,
				FilePath: string.Empty,
				TextSpan: default,
				LinePositionSpan: default,
				AdditionalLinePositions: additionalLocations is null
					? []
					: [.. additionalLocations.Value.Select(static loc => loc.GetLineSpan().Span)],
				MessageArgs: EquatableArray<string>.Create(messageArgs)
			);
		}

		var lineSpan = location.GetLineSpan();
		return new(
			Descriptor: descriptor,
			FilePath: lineSpan.Path,
			TextSpan: location.SourceSpan,
			LinePositionSpan: lineSpan.Span,
			AdditionalLinePositions: additionalLocations is null
				? []
				: [.. additionalLocations.Value.Select(static loc => loc.GetLineSpan().Span)],
			MessageArgs: EquatableArray<string>.Create(messageArgs)
		);
	}
}
