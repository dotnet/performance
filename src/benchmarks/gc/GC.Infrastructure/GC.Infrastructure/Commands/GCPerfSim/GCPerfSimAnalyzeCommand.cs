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
    internal sealed class GCPerfSimAnalyzeCommand : Command<GCPerfSimAnalyzeCommand.GCPerfSimAnalyzeSettings>
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
            ExecuteAnalysis(configuration, new Dictionary<string, ProcessExecutionDetails>());
            return 0;
        }

        public static IReadOnlyList<ComparisonResult> ExecuteAnalysis(GCPerfSimConfiguration configuration, Dictionary<string, ProcessExecutionDetails> executionDetails)
        {
            string outputPath = Path.Combine(configuration.Output!.Path, "Results.md");
            IReadOnlyList<ComparisonResult> results = Markdown.GenerateTable(configuration, executionDetails, outputPath);
            AnsiConsole.MarkupLine($"[green bold] ({DateTime.Now}) Results written to {outputPath} [/]");
            return results;
        }
    }
}
