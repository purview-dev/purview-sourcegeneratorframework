using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Purview.SourceGeneratorFramework.Benchmarks;

public static class Program
{
	public static void Main(string[] args)
	{
		var config = ManualConfig
			.CreateEmpty()
			.AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance))
			.AddLogger(ConsoleLogger.Default)
			.AddExporter(MarkdownExporter.Default, HtmlExporter.Default)
			.AddColumnProvider(DefaultColumnProviders.Instance)
			.WithOptions(ConfigOptions.DisableOptimizationsValidator);

		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
	}
}
