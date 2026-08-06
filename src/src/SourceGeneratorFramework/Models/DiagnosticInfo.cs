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
	/// Creates a <see cref="DiagnosticInfo"/> from a descriptor and optional location.
	/// </summary>
	public static DiagnosticInfo Create(
		DiagnosticDescriptor descriptor,
		Location? location,
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
				MessageArgs: EquatableArray<string>.Create(messageArgs)
			);
		}

		var lineSpan = location.GetLineSpan();
		return new DiagnosticInfo(
			Descriptor: descriptor,
			FilePath: lineSpan.Path,
			TextSpan: location.SourceSpan,
			LinePositionSpan: lineSpan.Span,
			MessageArgs: EquatableArray<string>.Create(messageArgs)
		);
	}
}
