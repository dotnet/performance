using GC.Analysis.API;
using GC.Infrastructure.Commands.RunCommand;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Presentation;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace GC.Infrastructure.Commands.GCPerfSim
{
    public sealed class GCPerfSimFunctionalCommand : Command<GCPerfSimFunctionalCommand.GCPerfSimFunctionalSettings>
    {
        private static readonly string _baseSuitePath = Path.Combine("Commands", "RunCommand", "BaseSuite");
        private static readonly string _gcPerfSimBase = Path.Combine(_baseSuitePath, "GCPerfSim_Normal_Workstation.yaml");
        private static readonly ISerializer _serializer = Common.Serializer;
        private static readonly int _logicalProcessors = CreateSuitesCommand.GetAppropriateLogicalProcessors();

        public sealed class GCPerfSimFunctionalSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public string? ConfigurationPath { get; init; }

            [Description("Crank Server to target.")]
            [CommandOption("-s|--server")]
            public string? Server { get; init; }
        }

        internal static void SaveConfiguration(ConfigurationBase configuration, string outputPath, string fileName)
        {
            var serializedResult = _serializer.Serialize(configuration);
            File.WriteAllText(Path.Combine(outputPath, fileName), serializedResult);
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] GCPerfSimFunctionalSettings settings)
        {
            // I. Extract the configuration path.
            string configurationPath = settings.ConfigurationPath;

            // Parse out the yaml file -> Memory as a C# object make use of that.
            // Precondition checks.
            ConfigurationChecker.VerifyFile(configurationPath, $"{nameof(GCPerfSimFunctionalCommand)}");
            GCPerfSimFunctionalConfiguration configuration = GCPerfSimFunctionalConfigurationParser.Parse(configurationPath, true);

            // II. Create the test suite for gcperfsim functional tests.
            string gcPerfSimOutputPath = Path.Combine(configuration.output_path, "GCPerfSim");
            Core.Utilities.TryCreateDirectory(gcPerfSimOutputPath);

            string suitePath = Path.Combine(configuration.output_path, "Suites");
            string gcPerfSimSuitePath = Path.Combine(suitePath, "GCPerfSim_Functional");

            Core.Utilities.TryCreateDirectory(gcPerfSimSuitePath);

            // For each of the scenarios below:
            // a. Add the coreruns. 
            // b. Add the gcperfsim parameters that are pertinent to that run.
            // c. Add any environment variables that will be related to that run.

            // 1. Normal Server
            CreateNormalServerSuite(gcPerfSimSuitePath, configuration);

            // 2. Normal Workstation.
            CreateNormalWorkstationSuite(gcPerfSimSuitePath, configuration);

            // 3. LowMemoryContainer.
            CreateLowMemoryContainerSuite(gcPerfSimSuitePath, configuration);

            // 4. HighMemoryLoad.
            CreateHighMemoryLoadSuite(gcPerfSimSuitePath, configuration);

            // III. Execute all the functional tests.

            string[] gcperfsimConfigurationFileNames = Directory.GetFiles(gcPerfSimSuitePath, "*.yaml");

            Dictionary<string, GCPerfSimResults> yamlFileResultMap = new Dictionary<string, GCPerfSimResults>();

            foreach (string gcperfsimConfigurationFileName in gcperfsimConfigurationFileNames)
            {
                try
                {
                    GCPerfSimConfiguration gcperfsimConfiguration =
                                        GCPerfSimConfigurationParser.Parse(gcperfsimConfigurationFileName);

                    Stopwatch sw = new();
                    sw.Start();

                    AnsiConsole.Write(new Rule(gcperfsimConfiguration.Name));
                    AnsiConsole.WriteLine();

                    // run the test
                    GCPerfSimResults gcperfsimResult = GCPerfSimCommand.RunGCPerfSim(gcperfsimConfiguration, settings.Server);
                    string yamlFileName = Path.GetFileNameWithoutExtension(gcperfsimConfigurationFileName);
                    yamlFileResultMap[yamlFileName] = gcperfsimResult;

                    sw.Stop();
                    AnsiConsole.WriteLine($"Time to execute Msec: {sw.ElapsedMilliseconds}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    continue;
                }
            }

            // IV. Based on the results of the functional tests, we'd want to generate a report.
            // Looking at all the runs data, we'd want to aggregate and create a markdown table of the following type:
            // Output should live in Results.md in the output folder.
            // TODO: If the run fails, what are the commands, failures and any debugging information that'll be helpful.
            // | Yaml | Scenario | ✅ / ❌  |
            // | ---- | -------- | ------------ |
            // | Normal_Server.yaml | 0gb | Pass |
            // | Normal_Workstation.yaml | 2gb_pinning | Fail |
            string markdownPath = Path.Combine(configuration.output_path, "Results.md");
            using (StreamWriter resultWriter = new StreamWriter(markdownPath))
            {
                resultWriter.WriteLine("| Yaml | Scenario | ✅ / ❌  |");
                resultWriter.WriteLine("| ---- | -------- | ------------ |");

                StringBuilder resultContent = new StringBuilder();

                foreach (var yamlFileNameResultPair in yamlFileResultMap)
                {
                    string yamlFileName = yamlFileNameResultPair.Key;
                    GCPerfSimResults gcperfsimResult = yamlFileNameResultPair.Value;
                    foreach (var executionDetail in gcperfsimResult.ExecutionDetails)
                    {
                        string scenario = executionDetail.Key;
                        string testResult = executionDetail.Value.HasFailed == false ? "✅" : "❌";
                        resultContent.AppendLine($"| {yamlFileName} | {scenario} | {testResult} |");
                    }
                }

                resultWriter.Write(resultContent.ToString());

                foreach (var yamlFileNameResultPair in yamlFileResultMap)
                {
                    string yamlFileName = yamlFileNameResultPair.Key;
                    GCPerfSimResults gcperfsimResult = yamlFileNameResultPair.Value;

                    bool hasFailedTests = gcperfsimResult.ExecutionDetails.Any(
                        executionDetail => executionDetail.Value.HasFailed == true);

                    if (hasFailedTests == true)
                    {
                        resultWriter.AddIncompleteTestsSectionWithYamlFileName(
                            yamlFileName, new(gcperfsimResult.ExecutionDetails));
                    }
                }
            }
            
            return 0;
        }

        private void CreateNormalServerSuite(string gcPerfSimSuitePath, GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimNormalServerConfiguration = GCPerfSimConfigurationParser.Parse(_gcPerfSimBase, true);

            // modify gcperfsim_configurations
            
            // Set tc = 2 * logicalProcessors
            gcPerfSimNormalServerConfiguration.gcperfsim_configurations.Parameters["tc"] = (_logicalProcessors * 2).ToString();
            gcPerfSimNormalServerConfiguration.gcperfsim_configurations.Parameters["tagb"] = "100";
            gcPerfSimNormalServerConfiguration.gcperfsim_configurations.gcperfsim_path =
                configuration.gcperfsim_path;

            // modify environment
            gcPerfSimNormalServerConfiguration.Environment.environment_variables["COMPlus_GCServer"] = "1";
            gcPerfSimNormalServerConfiguration.Environment.environment_variables["COMPlus_GCHeapCount"] = _logicalProcessors.ToString("X");

            // modify coreruns
            gcPerfSimNormalServerConfiguration.coreruns = new Dictionary<string, CoreRunInfo>();
            gcPerfSimNormalServerConfiguration.coreruns["segments"] = configuration.coreruns["segments"];
            gcPerfSimNormalServerConfiguration.coreruns["regions"] = configuration.coreruns["regions"];

            // modify output
            gcPerfSimNormalServerConfiguration.Output.Path =
                Path.Combine(configuration.output_path, "Normal_Server");

            // modify name 
            gcPerfSimNormalServerConfiguration.Name = "Normal_Server";

            // modify trace_configurations
            gcPerfSimNormalServerConfiguration.TraceConfigurations.Type = configuration.trace_configuration_type;

            SaveConfiguration(gcPerfSimNormalServerConfiguration, gcPerfSimSuitePath, "Normal_Server.yaml");
        }

        private void CreateNormalWorkstationSuite(string gcPerfSimSuitePath, GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimNormalWorkstationConfiguration = GCPerfSimConfigurationParser.Parse(_gcPerfSimBase, true);

            // modify gcperfsim_configurations
            gcPerfSimNormalWorkstationConfiguration.gcperfsim_configurations.gcperfsim_path =
                configuration.gcperfsim_path;

            // modify coreruns
            gcPerfSimNormalWorkstationConfiguration.coreruns = new Dictionary<string, CoreRunInfo>();
            gcPerfSimNormalWorkstationConfiguration.coreruns["segments"] = configuration.coreruns["segments"];
            gcPerfSimNormalWorkstationConfiguration.coreruns["regions"] = configuration.coreruns["regions"];

            // modify output
            gcPerfSimNormalWorkstationConfiguration.Output.Path =
                Path.Combine(configuration.output_path, "Normal_Workstation");

            // modify name 
            gcPerfSimNormalWorkstationConfiguration.Name = "Normal_Workstation";

            // modify trace_configurations
            gcPerfSimNormalWorkstationConfiguration.TraceConfigurations.Type = configuration.trace_configuration_type;

            SaveConfiguration(gcPerfSimNormalWorkstationConfiguration, gcPerfSimSuitePath, "Normal_Workstation.yaml");
        }

        private void CreateLowMemoryContainerSuite(string gcPerfSimSuitePath, GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimLowMemoryContainerConfiguration = GCPerfSimConfigurationParser.Parse(_gcPerfSimBase, true);
            // modify runs

            gcPerfSimLowMemoryContainerConfiguration.Runs.Clear();

            gcPerfSimLowMemoryContainerConfiguration.Runs["server"] = new Run
            {
                override_parameters = new Dictionary<string, string>()
                {
                    { "tc",  (2 * _logicalProcessors).ToString() },
                    { "tagb", "100" },
                    { "tlgb", "0.45"}
                }
            };

            gcPerfSimLowMemoryContainerConfiguration.Runs["workstation"] = new Run
            {
                override_parameters = new Dictionary<string, string>()
                {
                    { "tc", "2" },
                    { "tlgb", "0.45" } // oom if set tlgb to 0.5 in workstation scenario
                },
                environment_variables = new Dictionary<string, string>()
                {
                    {"COMPlus_GCServer", "0" },
                    {"COMPlus_GCHeapCount", "1" },
                }
            };

            // modify gcperfsim_configurations
            gcPerfSimLowMemoryContainerConfiguration.gcperfsim_configurations.Parameters["tc"] = "16";
            gcPerfSimLowMemoryContainerConfiguration.gcperfsim_configurations.Parameters["tagb"] = "100";
            gcPerfSimLowMemoryContainerConfiguration.gcperfsim_configurations.Parameters["tlgb"] = "0.1";
            gcPerfSimLowMemoryContainerConfiguration.gcperfsim_configurations.Parameters["sohsi"] = "50";
            gcPerfSimLowMemoryContainerConfiguration.gcperfsim_configurations.gcperfsim_path =
                configuration.gcperfsim_path;

            // modify environment
            gcPerfSimLowMemoryContainerConfiguration.Environment.environment_variables["COMPlus_GCServer"] = "1";
            gcPerfSimLowMemoryContainerConfiguration.Environment.environment_variables["COMPlus_GCHeapCount"] = "4";
            gcPerfSimLowMemoryContainerConfiguration.Environment.environment_variables["COMPlus_GCHeapHardLimit"] = "0x23C34600";
            gcPerfSimLowMemoryContainerConfiguration.Environment.environment_variables["COMPlus_GCTotalPhysicalMemory"] = "0x23C34600";

            // modify coreruns
            gcPerfSimLowMemoryContainerConfiguration.coreruns = new Dictionary<string, CoreRunInfo>();
            gcPerfSimLowMemoryContainerConfiguration.coreruns["segments"] = configuration.coreruns["segments"];
            gcPerfSimLowMemoryContainerConfiguration.coreruns["regions"] = configuration.coreruns["regions"];

            // modify output
            gcPerfSimLowMemoryContainerConfiguration.Output.Path =
                Path.Combine(configuration.output_path, "LowMemoryContainer");

            // modify name 
            gcPerfSimLowMemoryContainerConfiguration.Name = "LowMemoryContainer";

            // modify trace_configurations
            gcPerfSimLowMemoryContainerConfiguration.TraceConfigurations.Type = configuration.trace_configuration_type;

            SaveConfiguration(gcPerfSimLowMemoryContainerConfiguration, gcPerfSimSuitePath, "LowMemoryContainer.yaml");
        }

        private void CreateHighMemoryLoadSuite(string gcPerfSimSuitePath, GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimHighMemoryLoadConfiguration = GCPerfSimConfigurationParser.Parse(_gcPerfSimBase, true);

            // modify runs
            gcPerfSimHighMemoryLoadConfiguration.Runs.Clear();

            gcPerfSimHighMemoryLoadConfiguration.Runs["server"] = new Run
            {
                override_parameters = new Dictionary<string, string>()
                {
                    { "tlgb", "3"},
                    { "sohsi", "50"}
                }
            };

            gcPerfSimHighMemoryLoadConfiguration.Runs["workstation"] = new Run
            {
                override_parameters = new Dictionary<string, string>()
                {
                    { "tlgb", "3" },
                    { "sohsi", "50" }
                },
                environment_variables = new Dictionary<string, string>()
                {
                    {"COMPlus_GCServer", "0" },
                }
            };

            // modify gcperfsim_configurations
            gcPerfSimHighMemoryLoadConfiguration.gcperfsim_configurations.Parameters["tc"] = (_logicalProcessors * 2).ToString();
            gcPerfSimHighMemoryLoadConfiguration.gcperfsim_configurations.Parameters["tagb"] = "100";
            gcPerfSimHighMemoryLoadConfiguration.gcperfsim_configurations.Parameters["tlgb"] = "3";
            gcPerfSimHighMemoryLoadConfiguration.gcperfsim_configurations.gcperfsim_path =
                configuration.gcperfsim_path;

            // modify environment
            gcPerfSimHighMemoryLoadConfiguration.Environment.environment_variables["COMPlus_GCServer"] = "1";
            gcPerfSimHighMemoryLoadConfiguration.Environment.environment_variables["COMPlus_GCHeapCount"] = _logicalProcessors.ToString("X");

            // TODO: add environment variables in GCPerfSimFunctionalRun.yaml
            gcPerfSimHighMemoryLoadConfiguration.Environment.environment_variables["COMPlus_GCName"] = configuration.environment_variables["COMPlus_GCName"];
            gcPerfSimHighMemoryLoadConfiguration.Environment.environment_variables["COMPlus_GCHeapHardLimit"] = "0x100000000";
            gcPerfSimHighMemoryLoadConfiguration.Environment.environment_variables["COMPlus_GCTotalPhysicalMemory"] = "0x100000000";

            // modify coreruns
            gcPerfSimHighMemoryLoadConfiguration.coreruns = new Dictionary<string, CoreRunInfo>();
            gcPerfSimHighMemoryLoadConfiguration.coreruns["segments"] = configuration.coreruns["segments"];
            gcPerfSimHighMemoryLoadConfiguration.coreruns["regions"] = configuration.coreruns["regions"];

            // modify output
            gcPerfSimHighMemoryLoadConfiguration.Output.Path =
                Path.Combine(configuration.output_path, "HighMemoryLoad");

            // modify name 
            gcPerfSimHighMemoryLoadConfiguration.Name = "HighMemory_NormalServer";

            // modify trace_configurations
            gcPerfSimHighMemoryLoadConfiguration.TraceConfigurations.Type = configuration.trace_configuration_type;

            SaveConfiguration(gcPerfSimHighMemoryLoadConfiguration, gcPerfSimSuitePath, "HighMemoryLoad.yaml");
        }
    }
}
