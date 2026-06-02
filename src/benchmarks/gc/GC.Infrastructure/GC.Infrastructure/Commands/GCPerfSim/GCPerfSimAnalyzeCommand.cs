using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Presentation.GCPerfSim;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GC.Infrastructure.Commands.GCPerfSim
{
    public sealed class GCPerfSimAnalyzeCommand : Command<GCPerfSimAnalyzeCommand.GCPerfSimAnalyzeSettings>
    {
        public sealed class GCPerfSimAnalyzeSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] GCPerfSimAnalyzeSettings settings)
        {
            AnsiConsole.Write(new Rule("GCPerfSim Analyze"));
            AnsiConsole.WriteLine();

            ConfigurationChecker.VerifyFile(settings.ConfigurationPath, nameof(GCPerfSimAnalyzeSettings));
            GCPerfSimConfiguration configuration = GCPerfSimConfigurationParser.Parse(settings.ConfigurationPath);

            Core.Utilities.TryCreateDirectory(configuration.Output!.Path);

            // TODO: Fill in at least the repro steps if you are simply analyzing the results.
            var comparisonResultGroupedByRunName = ExecuteAnalysis(configuration);
            Present(configuration, comparisonResultGroupedByRunName, new());
            return 0;
        }

        public static IReadOnlyCollection<GCTraceMetricComparisonResults> ExecuteAnalysis(GCPerfSimConfiguration configuration)
        {
            var allTraceFiles = GCTraceMetricComparison.GetAllTraceFiles(configuration);
            var allGCTraceMetrics = GCTraceMetricComparison.AnalyzeGCPerfsimResults(configuration, allTraceFiles);
            var allComparisonResults = GCTraceMetricComparison.CompareGCTraceMetrics(configuration, allGCTraceMetrics);
            var comparisonResultGroupedByRunName = GCTraceMetricComparison.GroupComparisonResultsByRunName(allComparisonResults);
            return comparisonResultGroupedByRunName;
        }

        public static void Present(GCPerfSimConfiguration configuration,
                                   IEnumerable<GCTraceMetricComparisonResults> comparisonResultGroupedByRunName,
                                   Dictionary<string, ProcessExecutionDetails> executionDetails)
        {
            foreach (var format in configuration.Output.Formats)
            {
                if (format == "markdown")
                {
                    string outputPath = Path.Combine(configuration.Output.Path, "Results.md");
                    Markdown.GenerateForAnalyzeCommand(configuration, comparisonResultGroupedByRunName, executionDetails, outputPath);
                    AnsiConsole.MarkupLine($"[bold green] ({DateTime.Now}) Results written to {Markup.Escape(outputPath)}.[/]");
                    continue;
                }

                if (format == "json")
                {
                    string outputPath = Path.Combine(configuration.Output.Path, "Results.json");
                    Json.GenerateForAnalyzeCommand(comparisonResultGroupedByRunName, outputPath);
                    AnsiConsole.MarkupLine($"[bold green] ({DateTime.Now}) Results written to {Markup.Escape(outputPath)}.[/]");
                    continue;
                }
            }
        }
    }
}
