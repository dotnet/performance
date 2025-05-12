using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.ReliabilityFrameworkTest;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GC.Infrastructure.Commands.ReliabilityFrameworkTest
{
    public class ReliabilityFrameworkTestAggregateCommand : 
        Command<ReliabilityFrameworkTestAggregateCommand.ReliabilityFrameworkTestAggregateSettings>
    {
        public sealed class ReliabilityFrameworkTestAggregateSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, 
                                    [NotNull] ReliabilityFrameworkTestAggregateSettings settings)
        {
            AnsiConsole.Write(new Rule("Aggregate Analysis Results For Reliability Framework Test"));
            AnsiConsole.WriteLine();

            ConfigurationChecker.VerifyFile(settings.ConfigurationPath,
                                            nameof(ReliabilityFrameworkTestAggregateSettings));
            ReliabilityFrameworkTestAnalyzeConfiguration configuration =
                ReliabilityFrameworkTestAnalyzeConfigurationParser.Parse(settings.ConfigurationPath);

            ReliabilityFrameworkTestResultAggregator.AggregateResult(configuration);
            return 0;
        }

    }
}
