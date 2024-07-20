using GC.Infrastructure.Commands.RunCommand;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Presentation;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
            public required string ConfigurationPath { get; init; }

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
            GCPerfSimFunctionalConfiguration configuration = GCPerfSimFunctionalConfigurationParser.Parse(configurationPath);

            // II. Create the test suite for gcperfsim functional tests.
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

            // 5. LargePage_Server
            CreateLargePages_ServerSuite(gcPerfSimSuitePath, configuration);

            // 6. LargePage_Workstation
            CreateLargePages_WorkstationSuite(gcPerfSimSuitePath, configuration);

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

                    bool hasFailedTests = gcperfsimResult.ExecutionDetails.Any(executionDetail => executionDetail.Value.HasFailed);

                    if (hasFailedTests)
                    {
                        resultWriter.AddIncompleteTestsSectionWithYamlFileName(
                            yamlFileName, new(gcperfsimResult.ExecutionDetails));
                    }
                }
            }

            AnsiConsole.MarkupLine($"[green bold] ({DateTime.Now}) Results written to: {markdownPath} [/]");
            return 0;
        }

        private void CreateNormalServerSuite(string gcPerfSimSuitePath, GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimNormalServerConfiguration = CreateBasicGCPerfSimConfiguration(configuration);

            // Set tc = 2 * logicalProcessors
            gcPerfSimNormalServerConfiguration.gcperfsim_configurations.Parameters["tc"] = (_logicalProcessors * 2).ToString();
            gcPerfSimNormalServerConfiguration.gcperfsim_configurations.Parameters["tagb"] = "100";

            // modify environment
            gcPerfSimNormalServerConfiguration.Environment.environment_variables["DOTNET_gcServer"] = "1";
            gcPerfSimNormalServerConfiguration.Environment.environment_variables["DOTNET_GCHeapCount"] = _logicalProcessors.ToString("X");

            // modify output
            gcPerfSimNormalServerConfiguration.Output.Path =
                Path.Combine(configuration.output_path, "Normal_Server");

            // modify name 
            gcPerfSimNormalServerConfiguration.Name = "Normal_Server";

            SaveConfiguration(gcPerfSimNormalServerConfiguration, gcPerfSimSuitePath, "Normal_Server.yaml");
        }

        private void CreateNormalWorkstationSuite(string gcPerfSimSuitePath, GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimNormalWorkstationConfiguration = CreateBasicGCPerfSimConfiguration(configuration);

            // modify gcperfsim_configurations
            gcPerfSimNormalWorkstationConfiguration.gcperfsim_configurations.Parameters["tc"] = "2";
            gcPerfSimNormalWorkstationConfiguration.gcperfsim_configurations.Parameters["tagb"] = "100";

            // modify output
            gcPerfSimNormalWorkstationConfiguration.Output.Path =
                Path.Combine(configuration.output_path, "Normal_Workstation");

            // modify name 
            gcPerfSimNormalWorkstationConfiguration.Name = "Normal_Workstation";

            SaveConfiguration(gcPerfSimNormalWorkstationConfiguration, gcPerfSimSuitePath, "Normal_Workstation.yaml");
        }

        private void CreateLowMemoryContainerSuite(string gcPerfSimSuitePath, GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimLowMemoryContainerConfiguration = CreateBasicGCPerfSimConfiguration(configuration);

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
                    {"DOTNET_gcServer", "0" },
                    {"DOTNET_GCHeapCount", "1" },
                }
            };

            // modify gcperfsim_configurations
            gcPerfSimLowMemoryContainerConfiguration.gcperfsim_configurations.Parameters["tc"] = (_logicalProcessors * 2).ToString();
            gcPerfSimLowMemoryContainerConfiguration.gcperfsim_configurations.Parameters["tagb"] = "100";
            gcPerfSimLowMemoryContainerConfiguration.gcperfsim_configurations.Parameters["tlgb"] = "0.1";
            gcPerfSimLowMemoryContainerConfiguration.gcperfsim_configurations.Parameters["sohsi"] = "50";
            gcPerfSimLowMemoryContainerConfiguration.gcperfsim_configurations.gcperfsim_path =
                configuration.gcperfsim_path;

            // modify environment
            gcPerfSimLowMemoryContainerConfiguration.Environment.environment_variables["DOTNET_gcServer"] = "1";
            gcPerfSimLowMemoryContainerConfiguration.Environment.environment_variables["DOTNET_GCHeapCount"] = "4";
            gcPerfSimLowMemoryContainerConfiguration.Environment.environment_variables["DOTNET_GCHeapHardLimit"] = "0x23C34600";
            gcPerfSimLowMemoryContainerConfiguration.Environment.environment_variables["DOTNET_GCTotalPhysicalMemory"] = "0x23C34600";

            // modify output
            gcPerfSimLowMemoryContainerConfiguration.Output.Path =
                Path.Combine(configuration.output_path, "LowMemoryContainer");

            // modify name 
            gcPerfSimLowMemoryContainerConfiguration.Name = "LowMemoryContainer";

            SaveConfiguration(gcPerfSimLowMemoryContainerConfiguration, gcPerfSimSuitePath, "LowMemoryContainer.yaml");
        }

        private void CreateHighMemoryLoadSuite(string gcPerfSimSuitePath, GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimHighMemoryLoadConfiguration = CreateBasicGCPerfSimConfiguration(configuration);

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
                    {"DOTNET_gcServer", "0" },
                }
            };

            // modify gcperfsim_configurations
            gcPerfSimHighMemoryLoadConfiguration.gcperfsim_configurations.Parameters["tc"] = (_logicalProcessors * 2).ToString();
            gcPerfSimHighMemoryLoadConfiguration.gcperfsim_configurations.Parameters["tagb"] = "100";
            gcPerfSimHighMemoryLoadConfiguration.gcperfsim_configurations.Parameters["tlgb"] = "3";
            gcPerfSimHighMemoryLoadConfiguration.gcperfsim_configurations.gcperfsim_path =
                configuration.gcperfsim_path;

            // modify environment
            gcPerfSimHighMemoryLoadConfiguration.Environment.environment_variables["DOTNET_gcServer"] = "1";
            gcPerfSimHighMemoryLoadConfiguration.Environment.environment_variables["DOTNET_GCHeapCount"] = _logicalProcessors.ToString("X");

            // add environment variables in GCPerfSimFunctionalRun.yaml
            gcPerfSimHighMemoryLoadConfiguration.Environment.environment_variables["DOTNET_GCHeapHardLimit"] = "0x100000000";
            gcPerfSimHighMemoryLoadConfiguration.Environment.environment_variables["DOTNET_GCTotalPhysicalMemory"] = "0x100000000";

            // modify output
            gcPerfSimHighMemoryLoadConfiguration.Output.Path =
                Path.Combine(configuration.output_path, "HighMemoryLoad");

            // modify name 
            gcPerfSimHighMemoryLoadConfiguration.Name = "HighMemoryLoad";

            SaveConfiguration(gcPerfSimHighMemoryLoadConfiguration, gcPerfSimSuitePath, "HighMemoryLoad.yaml");
        }

        private void CreateLargePages_ServerSuite(string gcPerfSimSuitePath, GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimLargePages_ServerConfiguration = CreateBasicGCPerfSimConfiguration(configuration);

            // Set tc = 2 * logicalProcessors
            gcPerfSimLargePages_ServerConfiguration.gcperfsim_configurations.Parameters["tc"] = (_logicalProcessors * 2).ToString();
            gcPerfSimLargePages_ServerConfiguration.gcperfsim_configurations.Parameters["tagb"] = "100";

            // modify environment
            gcPerfSimLargePages_ServerConfiguration.Environment.environment_variables["DOTNET_gcServer"] = "1";
            gcPerfSimLargePages_ServerConfiguration.Environment.environment_variables["DOTNET_GCHeapCount"] = _logicalProcessors.ToString("X");
            gcPerfSimLargePages_ServerConfiguration.Environment.environment_variables["DOTNET_GCLargePages"] = "1";
            gcPerfSimLargePages_ServerConfiguration.Environment.environment_variables["DOTNET_GCHeapHardLimitSOH"] = "0x800000000";
            gcPerfSimLargePages_ServerConfiguration.Environment.environment_variables["DOTNET_GCHeapHardLimitLOH"] = "0x400000000";
            gcPerfSimLargePages_ServerConfiguration.Environment.environment_variables["DOTNET_GCHeapHardLimitPOH"] = "0x100000000";

            // modify output
            gcPerfSimLargePages_ServerConfiguration.Output.Path =
                Path.Combine(configuration.output_path, "LargePages_Server");

            // modify name 
            gcPerfSimLargePages_ServerConfiguration.Name = "LargePages_Server";

            SaveConfiguration(gcPerfSimLargePages_ServerConfiguration, gcPerfSimSuitePath, "LargePages_Server.yaml");
        }

        private void CreateLargePages_WorkstationSuite(string gcPerfSimSuitePath, GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimLargePages_WorkstationConfiguration = CreateBasicGCPerfSimConfiguration(configuration);

            // Set tc = 2 * logicalProcessors
            gcPerfSimLargePages_WorkstationConfiguration.gcperfsim_configurations.Parameters["tc"] = (_logicalProcessors * 2).ToString();
            gcPerfSimLargePages_WorkstationConfiguration.gcperfsim_configurations.Parameters["tagb"] = "100";

            // modify environment
            gcPerfSimLargePages_WorkstationConfiguration.Environment.environment_variables["DOTNET_gcServer"] = "1";
            gcPerfSimLargePages_WorkstationConfiguration.Environment.environment_variables["DOTNET_GCHeapCount"] = _logicalProcessors.ToString("X");
            gcPerfSimLargePages_WorkstationConfiguration.Environment.environment_variables["DOTNET_GCLargePages"] = "1";
            gcPerfSimLargePages_WorkstationConfiguration.Environment.environment_variables["DOTNET_GCHeapHardLimitSOH"] = "0x800000000";
            gcPerfSimLargePages_WorkstationConfiguration.Environment.environment_variables["DOTNET_GCHeapHardLimitLOH"] = "0x400000000";
            gcPerfSimLargePages_WorkstationConfiguration.Environment.environment_variables["DOTNET_GCHeapHardLimitPOH"] = "0x100000000";

            // modify output
            gcPerfSimLargePages_WorkstationConfiguration.Output.Path =
                Path.Combine(configuration.output_path, "LargePages_Workstation");

            // modify name 
            gcPerfSimLargePages_WorkstationConfiguration.Name = "LargePages_Workstation";

            SaveConfiguration(gcPerfSimLargePages_WorkstationConfiguration, gcPerfSimSuitePath, "LargePages_Workstation.yaml");
        }

        private GCPerfSimConfiguration CreateBasicGCPerfSimConfiguration(GCPerfSimFunctionalConfiguration configuration)
        {
            GCPerfSimConfiguration gcPerfSimNormalWorkstationConfiguration = GCPerfSimConfigurationParser.Parse(_gcPerfSimBase, true);

            // modify gcperfsim_configurations
            gcPerfSimNormalWorkstationConfiguration.gcperfsim_configurations.gcperfsim_path =
                configuration.gcperfsim_path;

            // modify coreruns
            gcPerfSimNormalWorkstationConfiguration.coreruns = new Dictionary<string, CoreRunInfo>();
            foreach (var keyValuePair in configuration.coreruns)
            {
                gcPerfSimNormalWorkstationConfiguration.coreruns[keyValuePair.Key] = keyValuePair.Value;
            }

            // modify trace_configurations
            gcPerfSimNormalWorkstationConfiguration.TraceConfigurations.Type = configuration.trace_configuration_type;

            // load environment variables by deep copying them.
            gcPerfSimNormalWorkstationConfiguration.Environment.environment_variables = new Dictionary<string, string>();
            foreach (var c in configuration.Environment.environment_variables)
            {
                gcPerfSimNormalWorkstationConfiguration.Environment.environment_variables[c.Key] = c.Value;
            }

            return gcPerfSimNormalWorkstationConfiguration;
        }
    }
}
