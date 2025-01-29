using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Presentation.GCPerfSim;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GC.Infrastructure.Commands.GCPerfSim
{
    public sealed class GCPerfSimCompareCommand : Command<GCPerfSimCompareCommand.GCPerfSimCompareSettings>
    {
        public sealed class GCPerfSimCompareSettings : CommandSettings
        {
            [Description("Path to Baseline Trace")]
            [CommandOption("-b|--baseline")]
            public required string BaselinePath { get; set; }

            [Description("Path to Comparand Trace")]
            [CommandOption("-c|--comparand")]
            public required string ComparandPath { get; set; }

            [Description("Path to Output")]
            [CommandOption("-o|--output")]
            public required string OutputPath { get; set; }

        }

        public override int Execute([NotNull] CommandContext context, [NotNull] GCPerfSimCompareSettings settings)
        {
            if (string.IsNullOrEmpty(settings.BaselinePath) || !File.Exists(settings.BaselinePath))
            {
                throw new ArgumentException($"{nameof(GCPerfSimCompareCommand)}: Baseline Path to the trace hasn't been provided or doesn't exist.");
            }

            if (string.IsNullOrEmpty(settings.ComparandPath) || !File.Exists(settings.ComparandPath))
            {
                throw new ArgumentException($"{nameof(GCPerfSimCompareCommand)}: Comparand Path to the trace hasn't been provided or doesn't exist.");
            }

            var comparisons = AnalyzeTrace.GetComparisons(settings.BaselinePath, settings.ComparandPath);

            // The first ones here will have the values.
            ResultItem baseline = comparisons.First().Value.Baseline;
            ResultItem run = comparisons.First().Value.Comparand;

            if (Path.GetExtension(settings.OutputPath) == ".json")
            {
                Json.GenerateComparisonDictionary(baseline, run, settings.OutputPath);
            }
            else
            {
                Markdown.GenerateComparisonTable(baseline, run, settings.OutputPath);
            }
            AnsiConsole.MarkupLine($"[green bold] ({DateTime.Now}) Results written to {settings.OutputPath} [/]");
            return 0;
        }
    }
}
