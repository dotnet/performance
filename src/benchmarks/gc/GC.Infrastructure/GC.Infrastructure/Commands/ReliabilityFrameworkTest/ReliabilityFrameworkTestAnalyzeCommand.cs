using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.ReliabilityFrameworkTest;
using Spectre.Console;
using Spectre.Console.Cli;



namespace GC.Infrastructure.Commands.ReliabilityFrameworkTest
{
    public sealed class ReliabilityFrameworkTestAnalyzeCommand :
        Command<ReliabilityFrameworkTestAnalyzeCommand.ReliabilityFrameworkTestAnalyzeSettings>
    {
        public sealed class ReliabilityFrameworkTestAnalyzeSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, 
                                    [NotNull] ReliabilityFrameworkTestAnalyzeSettings settings)
        {
            AnsiConsole.Write(new Rule("Analyze Dumps Collected In Reliability Framework Test"));
            AnsiConsole.WriteLine();

            ConfigurationChecker.VerifyFile(settings.ConfigurationPath,
                                            nameof(ReliabilityFrameworkTestAnalyzeSettings));
            ReliabilityFrameworkTestAnalyzeConfiguration configuration =
                ReliabilityFrameworkTestAnalyzeConfigurationParser.Parse(settings.ConfigurationPath);

            Directory.CreateDirectory(configuration.AnalyzeOutputFolder);

            ReliabilityFrameworkTestDumpAnalyzer.AnalyzeDumps(configuration);

            return 0;
        }
    }
}
