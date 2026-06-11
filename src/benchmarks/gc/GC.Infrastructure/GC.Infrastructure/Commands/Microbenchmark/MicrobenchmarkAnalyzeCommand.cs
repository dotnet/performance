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

            var comparisonResultsGroupedByName = ExecuteAnalysis(configuration);

            Present(configuration, comparisonResultsGroupedByName, new()); // Execution details aren't available for the analysis-only mode.
            return 0;
        }

        public static IReadOnlyList<MicrobenchmarkComparisonResults> ExecuteAnalysis(MicrobenchmarkConfiguration configuration)
        {
            var bdnJsonResults = MicrobenchmarkResultComparison.LoadBdnJsonResults(configuration);
            AnsiConsole.MarkupLine($"[bold green] ({DateTime.Now}) {bdnJsonResults.Count} BDN results loaded.[/]");
            var microbenchmarkResults = MicrobenchmarkResultComparison.AnalyzeMicrobenchmarkResults(configuration, bdnJsonResults);
            AnsiConsole.MarkupLine($"[bold green] ({DateTime.Now}) Analysis completed.[/]");
            var comparisonResults = MicrobenchmarkResultComparison.CompareMicrobenchmarkResults(configuration, microbenchmarkResults);
            
            return MicrobenchmarkResultComparison.GroupComparisonResultsByName(configuration, comparisonResults);
        }

        public static void Present(MicrobenchmarkConfiguration configuration, 
                                   IReadOnlyList<MicrobenchmarkComparisonResults> comparisonResultsGroupedByName,
                                   Dictionary<string, ProcessExecutionDetails> executionDetails)
        {
            foreach (var format in configuration.Output.Formats)
            {
                if (string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase))
                {
                    string outputPath = Path.Combine(configuration.Output.Path, "Results.md");
                    Markdown.GenerateTable(configuration, comparisonResultsGroupedByName, executionDetails, outputPath);
                    AnsiConsole.MarkupLine($"[bold green] ({DateTime.Now}) Results written to {Markup.Escape(outputPath)}.[/]");
                    continue;
                }

                if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
                {
                    string outputPath = Path.Combine(configuration.Output.Path, "Results.json");
                    Json.Generate(configuration, comparisonResultsGroupedByName, outputPath);
                    AnsiConsole.MarkupLine($"[bold green] ({DateTime.Now}) Results written to {Markup.Escape(outputPath)}.[/]");
                    continue;
                }
            }
        }
    }
}
