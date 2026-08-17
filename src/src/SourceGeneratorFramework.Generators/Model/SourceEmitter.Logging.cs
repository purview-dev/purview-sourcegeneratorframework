using Microsoft.CodeAnalysis.Text;

namespace Purview.SourceGeneratorFramework.Generators.Model;

partial class SourceEmitter
{
	static SourceText SourceGenLogger()
	{
		var writer = CreateWriter(
			typeof(LoggingSupportGenerator).FullName,
			GeneratorTypeLibrary.Logging.SourceGenLogger
		);

		var actionType = GeneratorTypeLibrary.System.Action.MakeGeneric(
			GeneratorTypeLibrary.System.String,
			GeneratorTypeLibrary.Logging.SourceGenLogLevel
		);
		return writer.WriteClass(
			new(GeneratorTypeLibrary.Logging.SourceGenLogger)
			{
				PrimaryConstructorParameters = [new("action", actionType)],
				Interfaces = [GeneratorTypeLibrary.Logging.ISourceGenLogger],
			},
			bodyWriter =>
			{
				bodyWriter.WriteField(
					new("_logger", actionType)
					{
						IsReadOnly = true,
						Initializer = $"action ?? throw new global::System.ArgumentNullException(nameof(action))",
					}
				);

				bodyWriter.WriteMethod(
					new("Log")
					{
						Accessibility = TypeDeclarationAccessibility.Public,
						Parameters =
						[
							new("level", GeneratorTypeLibrary.Logging.SourceGenLogLevel),
							new("indentation", GeneratorTypeLibrary.System.Int32),
							new("message", GeneratorTypeLibrary.System.String),
							new("args", GeneratorTypeLibrary.System.Object.MakeArray()) { IsParams = true },
						],
					},
					bodyWriter =>
					{
						bodyWriter.WriteBlock(
							"if (message is null)",
							bodyWriter =>
								bodyWriter.WriteLine(
									$"throw new global::System.ArgumentNullException(nameof(message));"
								)
						);

						bodyWriter.WriteBlock(
							"if (args is not null && args.Length > 0)",
							bodyWriter =>
								bodyWriter.WriteLine(
									"message = string.Format(global::System.Globalization.CultureInfo.InvariantCulture, message, args);"
								)
						);

						bodyWriter
							.NewLine()
							.Comment("We're using 2 spaces per indentation level.")
							.WriteLine(
								"_logger(indentation <= 0 ? message : new string(' ', indentation * 2) + message, level);"
							);
					}
				);
			}
		);
	}
}
