using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GC.Infrastructure.Core.Analysis.Microbenchmarks;
using GC.Infrastructure.Core.Presentation.Microbenchmarks;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;

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
            IReadOnlyList<MicrobenchmarkComparisonResults> comparisonResults = MicrobenchmarkResultsAnalyzer.GetComparisons(configuration);
            Presentation.Present(configuration, new()); // Execution details aren't available for the analysis-only mode.
            return 0;
        }
    }
}
