using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Analysis.Microbenchmarks;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using GC.Infrastructure.Core.Presentation.Microbenchmarks;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GC.Infrastructure.Commands.Microbenchmark
{
    public sealed class MicrobenchmarkAnalyzeCommand : Command<MicrobenchmarkAnalyzeCommand.MicrobenchmarkAnalyzeSettings>
    {
        public sealed class MicrobenchmarkAnalyzeSettings : CommandSettings
        {
            [Description("Configuration Path.")]
            [CommandOption("-c|--configuration")]
            public string? ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] MicrobenchmarkAnalyzeSettings settings)
        {
            ConfigurationChecker.VerifyFile(settings.ConfigurationPath, nameof(MicrobenchmarkAnalyzeCommand));
            MicrobenchmarkConfiguration configuration = MicrobenchmarkConfigurationParser.Parse(settings.ConfigurationPath);

            var comparisonResultsGroupedName = ExecuteAnalysis(configuration);

            Presentation.Present(configuration, comparisonResultsGroupedName, new()); // Execution details aren't available for the analysis-only mode.
            return 0;
        }

        public static List<MicrobenchmarkComparisonResults> ExecuteAnalysis(MicrobenchmarkConfiguration configuration)
        {
            Run? run = configuration.Runs.Values.FirstOrDefault();
            if (run == null)
            {
                throw new InvalidOperationException("No runs found in the configuration.");
            }
            string outputPathForRun = Path.Combine(configuration.Output.Path, run.Name);
            var benchmarkFullNameJsonMap = MicrobenchmarkResultComparison.MapBenchmarkFullNameToJsonForRun(outputPathForRun);
            List<MicrobenchmarkComparisonResult> comparisonResultForAllBenchmarks = new();

            foreach (var benchmarkFullName in benchmarkFullNameJsonMap.Keys)
            {
                AnsiConsole.Markup($"[bold green] ({DateTime.Now}) Analyzing Microbenchmarks: {benchmarkFullName} [/]\n");
                List<MicrobenchmarkComparisonResult> comparisonResultsForBenchmark = MicrobenchmarkResultComparison.CompareMicrobenchmarkResultForBenchmark(configuration, benchmarkFullName);
                comparisonResultForAllBenchmarks.AddRange(comparisonResultsForBenchmark);
            }

            return MicrobenchmarkResultComparison.GroupComparisonResultsByName(configuration, comparisonResultForAllBenchmarks);
        }
    }
}
