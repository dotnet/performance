using GC.Infrastructure.Commands.ASPNetBenchmarks;
using GC.Infrastructure.Commands.GCPerfSim;
using GC.Infrastructure.Commands.Microbenchmark;
using GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GC.Infrastructure.Commands.RunCommand
{
    public sealed class RunSuiteCommand : Command<RunSuiteCommand.RunSuiteCommandSettings>
    {
        public sealed class RunSuiteCommandSettings : CommandSettings
        {
            [Description("SuiteBasePath")]
            [CommandOption("-p|--suiteBasePath")]
            public string SuiteBasePath { get; set; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] RunSuiteCommandSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.SuiteBasePath) || !Directory.Exists(settings.SuiteBasePath))
            {
                throw new ArgumentNullException($"{nameof(RunSuiteCommandSettings)}: {nameof(settings.SuiteBasePath)} was either null or the directory doesn't exists.");
            }

            Dictionary<string, string> configuration = new();
            configuration["GCPerfSim"] = Path.Combine(settings.SuiteBasePath, "GCPerfSim");
            configuration["Microbenchmarks"] = Path.Combine(settings.SuiteBasePath, "Microbenchmarks");
            configuration["ASPNetBenchmarks"] = Path.Combine(settings.SuiteBasePath, "ASPNetBenchmarks");

            RunSuite(configuration);
            return 0;
        }

        public static void RunSuite(Dictionary<string, string> configuration)
        {
            string gcperfsimBase = configuration["GCPerfSim"];
            string[] configurations = Directory.GetFiles(gcperfsimBase, "*.yaml");
            foreach (var c in configurations)
            {
                try
                {
                    GCPerfSimConfiguration config = GCPerfSimConfigurationParser.Parse(c);
                    GCPerfSimResults comparisonResult = GCPerfSimCommand.RunGCPerfSim(config, null);
                }

                catch (Exception e)
                {
                    AnsiConsole.Write($"[red] GCPerfSim Configuration: {c} failed with {e.Message} [/]");
                }
            }

            string microbenchmarkBase = configuration["Microbenchmarks"];
            configurations = Directory.GetFiles(microbenchmarkBase, "*.yaml");
            foreach (var c in configurations)
            {
                try
                {
                    MicrobenchmarkConfiguration config = MicrobenchmarkConfigurationParser.Parse(c);
                    MicrobenchmarkCommand.RunMicrobenchmarks(config);
                }

                catch (Exception e)
                {
                    AnsiConsole.Write($"[red] Microbenchmark Configuration: {c} failed with {e.Message} [/]");
                }
            }

            // Run all ASPNet Benchmarks.
            string aspnetBenchmarks = configuration["ASPNetBenchmarks"];
            configurations = Directory.GetFiles(aspnetBenchmarks, "*.yaml");
            foreach (var c in configurations)
            {
                try
                {
                    ASPNetBenchmarksConfiguration config = ASPNetBenchmarksConfigurationParser.Parse(c);
                    AspNetBenchmarksCommand.RunASPNetBenchmarks(config);
                }

                catch (Exception e)
                {
                    AnsiConsole.Write($"[red] ASPNet Configuration: {c} failed with {e.Message} [/]");
                }
            }
        }
    }
}
