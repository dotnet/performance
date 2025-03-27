using GC.Infrastructure.Core.Configurations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GC.Infrastructure.Commands.ReliabilityFrameworkTest
{
    public sealed class ReliabilityFrameworkTestCreateTestsSuiteCommand :
        Command<ReliabilityFrameworkTestCreateTestsSuiteCommand.ReliabilityFrameworkTestCreateTestSuiteSettings>
    {
        private static readonly string RID = RuntimeInformation.RuntimeIdentifier;
        
        private static readonly string _baseSuitePath = Path.Combine("Commands", "RunCommand", "BaseSuite", "ReliabilityFramework");
        public sealed class ReliabilityFrameworkTestCreateTestSuiteSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context,
                                    [NotNull] ReliabilityFrameworkTestCreateTestSuiteSettings settings)
        {
            AnsiConsole.Write(new Rule("Create Test Suite For Reliability Framework Test"));
            AnsiConsole.WriteLine();

            ConfigurationChecker.VerifyFile(settings.ConfigurationPath,
                                            nameof(ReliabilityFrameworkTestCreateTestSuiteSettings));
            ReliabilityFrameworkTestCreateTestSuiteConfiguration configuration =
                ReliabilityFrameworkTestCreateTestSuiteConfigurationParser.Parse(settings.ConfigurationPath);

            Directory.CreateDirectory(configuration.OutputFolder);

            List<string> configNameList = new() { "loh", "poh", "non_induced", "finalization" };

            List<string> gcModeList = new() { "Datas", "Server", "Workstation" };

            // Build ReliabilityFramework.dll, gcperfsim.dll and Tests
            ReliabilityFrameworkTestSuiteCreator.CreateTestingAssets(configuration.ReliabilityFrameworkDll,
                                                                     RID,
                                                                     configuration.CoreRoot,
                                                                     configuration.OutputFolder,
                                                                     configuration.GCPerfSimDll,
                                                                     configuration.TestFolder);

            // Create config file
            string platformFolder = Path.Combine(configuration.OutputFolder, RID);
            Directory.CreateDirectory(platformFolder);
            foreach (string configName in configNameList)
            {
                string configFolder = Path.Combine(platformFolder, configName);
                Directory.CreateDirectory(configFolder);
                foreach (string gcMode in gcModeList)
                {
                    string gcModeFolder = Path.Combine(configFolder, gcMode);
                    Directory.CreateDirectory(gcModeFolder);

                    AnsiConsole.WriteLine($"====== Generate {configName}.config for {gcMode} mode ======");

                    string configSuffix =
                        configuration.EnableStressMode switch
                        {
                            true => "-stress",
                            false => ""
                        };
                    string configPath = Path.Combine(gcModeFolder,
                                                     $"{configName}-{gcMode}-{RID}{configSuffix}.config");
                    ReliabilityFrameworkTestSuiteCreator.GenerateTestConfig(RID,
                                                                            _baseSuitePath,
                                                                            configName,
                                                                            configuration.EnableStressMode,
                                                                            gcMode,
                                                                            configPath);

                    AnsiConsole.WriteLine($"====== Generate testing script for {gcMode} mode ======");
                    string osName = RID.Split("-")
                    .FirstOrDefault("");
                    string scriptExtension =
                        osName switch
                        {
                            "win" => ".ps1",
                            "linux" => ".sh"
                        };
                    string scriptPath = Path.Combine(gcModeFolder, $"TestingScript-{configName}-{gcMode}{scriptExtension}");

                    ReliabilityFrameworkTestSuiteCreator.GenerateTestScript(RID,
                                                                            _baseSuitePath,
                                                                            configuration.CoreRoot,
                                                                            configPath,
                                                                            gcMode,
                                                                            configuration.OutputFolder,
                                                                            scriptPath);
                }
            }

            return 0;
        }
    }
}
