using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;

namespace GC.Infrastructure.Commands.RunCommand
{
    public sealed class CreateSuitesCommand : Command<CreateSuitesCommand.CreateSuitesSettings>
    {
        private static readonly string _baseSuitePath = Path.Combine("Commands", "RunCommand", "BaseSuite");
        // Removed the high volatility configuration.  
        //private static readonly string _gcPerfSimBase      = Path.Combine(_baseSuitePath, "GCPerfSim_Normal_Workstation.yaml");
        private static readonly string _gcPerfSimBaseLowVolatility = Path.Combine(_baseSuitePath, "LowVolatilityRuns.yaml");
        private static readonly string _microbenchmarkBase = Path.Combine(_baseSuitePath, "Microbenchmarks.yaml");
        private static readonly string _aspNetBase = Path.Combine(_baseSuitePath, "ASPNetBenchmarks.yaml");
        private static readonly ISerializer _serializer = Common.Serializer;

        public sealed class CreateSuitesSettings : CommandSettings
        {
            [Description("Configuration")]
            [CommandOption("-c|--configuration")]
            public string ConfigurationPath { get; set; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] CreateSuitesSettings settings)
        {
            AnsiConsole.Write(new Rule("Creating Suites"));
            ConfigurationChecker.VerifyFile(settings.ConfigurationPath, nameof(CreateSuitesCommand));
            InputConfiguration configuration = InputConfigurationParser.Parse(settings.ConfigurationPath);
            Dictionary<string, string> configurationMap = CreateSuites(configuration);
            var configurationRoot = new Tree("[underline] Suite Created: [/]");
            foreach (var c in configurationMap)
            {
                configurationRoot.AddNode($"[blue] {c.Key} at {c.Value} [/]");
            }
            AnsiConsole.Write(configurationRoot);
            AnsiConsole.WriteLine();

            return 0;
        }

        public static Dictionary<string, string> CreateSuites(InputConfiguration configuration)
        {
            // Setup.
            Core.Utilities.TryCreateDirectory(configuration.output_path);

            // Ensure all pertinent directories are created.
            string suitePath = Path.Combine(configuration.output_path, "Suites");
            Core.Utilities.TryCreateDirectory(suitePath);

            // Copy over the symbols if they exist.
            if (configuration.symbol_path != null)
            {
                string outputSymbolPath = Path.Combine(configuration.output_path, "Symbols");
                Core.Utilities.TryCreateDirectory(outputSymbolPath);

                foreach (var paths in configuration.symbol_path)
                {
                    string pathToSymbols = Path.Combine(outputSymbolPath, paths.Key);
                    Core.Utilities.TryCreateDirectory(pathToSymbols);
                    Core.Utilities.CopyFilesRecursively(paths.Value, pathToSymbols);
                }
            }

            // Copy over the source directory if they exist.
            if (configuration.source_path != null)
            {
                string outputSourcePath = Path.Combine(configuration.output_path, "Sources");
                Core.Utilities.TryCreateDirectory(outputSourcePath);

                foreach (var paths in configuration.source_path)
                {
                    string pathToSource = Path.Combine(outputSourcePath, paths.Key);
                    Core.Utilities.TryCreateDirectory(pathToSource);
                    Core.Utilities.CopyFilesRecursively(paths.Value, pathToSource);
                }
            }

            Dictionary<string, string> configurationMap = new();

            // Create suites for:
            // 1. GCPerfSim
            // 2. Microbenchmarks
            // 3. ASP.NET
            string gcPerfSimBase = CreateGCPerfSimSuite(configuration, suitePath);
            configurationMap["GCPerfSim"] = gcPerfSimBase;

            string microbenchmarkBase = CreateMicrobenchmarkSuite(configuration, suitePath);
            configurationMap["Microbenchmark"] = microbenchmarkBase;

            string aspnetBenchmarkBase = CreateASPNetBenchmarkSuite(configuration, suitePath);
            configurationMap["ASPNetBenchmarks"] = aspnetBenchmarkBase;

            return configurationMap;
        }

        internal static string CreateASPNetBenchmarkSuite(InputConfiguration inputConfiguration, string suitePath)
        {
            string aspnetBenchmarks = Path.Combine(suitePath, "ASPNETBenchmarks");
            Core.Utilities.TryCreateDirectory(aspnetBenchmarks);

            ASPNetBenchmarksConfiguration configuration = ASPNetBenchmarksConfigurationParser.Parse(_aspNetBase);

            // Copy over the pertinent resources.
            string destinationASPNetBenchmark = Path.Combine(aspnetBenchmarks, "ASPNetBenchmarks.csv");
            Core.Utilities.TryCopyFile(sourcePath: Path.Combine(_baseSuitePath, "ASPNetBenchmarks.csv"),
                                       destinationPath: destinationASPNetBenchmark);

            string outputPath = Path.Combine(inputConfiguration.output_path, "ASPNetBenchmarks");
            Core.Utilities.TryCreateDirectory(outputPath);

            // Add runs.
            configuration.Runs = new();

            foreach (var r in inputConfiguration.coreruns)
            {
                Core.Configurations.ASPNetBenchmarks.Run run = new Core.Configurations.ASPNetBenchmarks.Run()
                {
                    environment_variables = r.Value.environment_variables
                };

                run.corerun = r.Value.Path;

                // If just the GCName env var is passed, use that as the "corerun" - the corerun in the context of
                // ASPNET benchmarks is any file that gets uploaded to the servers
                foreach (var envVars in r.Value.environment_variables)
                {
                    if (string.CompareOrdinal(envVars.Key, "DOTNET_GCName") == 0 ||
                        string.CompareOrdinal(envVars.Key, "DOTNET_GCName") == 0)
                    {
                        string directoryOfCorerun = Path.GetDirectoryName(r.Value.Path)!;
                        run.corerun = Path.Combine(directoryOfCorerun, envVars.Value);
                        break;
                    }
                }

                configuration.Runs[r.Key] = run;
            }

            // Add benchmark_file.
            configuration.benchmark_settings = new();
            configuration.benchmark_settings.benchmark_file = destinationASPNetBenchmark;

            // Update Trace Configuration type.
            configuration.TraceConfigurations.Type = inputConfiguration.trace_configuration_type.ToString();
            configuration.Output.Path = outputPath;
            configuration.Name = "ASPNetBenchmarks";

            // Output Path.
            SaveConfiguration(configuration, aspnetBenchmarks, "ASPNetBenchmarks.yaml");
            return aspnetBenchmarks;
        }

        internal static string CreateMicrobenchmarkSuite(InputConfiguration inputConfiguration, string suitePath)
        {
            string microbenchmarkSuitePath = Path.Combine(suitePath, "Microbenchmark");

            Core.Utilities.TryCreateDirectory(microbenchmarkSuitePath);

            string microbenchmarkOutputPath = Path.Combine(inputConfiguration.output_path, "Microbenchmarks");
            Core.Utilities.TryCreateDirectory(microbenchmarkOutputPath);

            string destinationMicrobenchmarksToRun = Path.Combine(microbenchmarkSuitePath, "MicrobenchmarksToRun.txt");
            Core.Utilities.TryCopyFile(sourcePath: Path.Combine(_baseSuitePath, "MicrobenchmarksToRun.txt"),
                                       destinationPath: destinationMicrobenchmarksToRun);

            string destinationMicrobenchmarkInvocationCount = Path.Combine(microbenchmarkSuitePath, "MicrobenchmarkInvocationCounts.psv");
            Core.Utilities.TryCopyFile(sourcePath: Path.Combine(_baseSuitePath, "MicrobenchmarkInvocationCounts.psv"),
                                       destinationPath: destinationMicrobenchmarkInvocationCount);

            // Workstation Runs.
            MicrobenchmarkConfiguration workstation = CreateBaseMicrobenchmarkSuite(inputConfiguration, destinationMicrobenchmarksToRun, destinationMicrobenchmarkInvocationCount);
            foreach (var r in workstation.Runs)
            {
                if (r.Value.environment_variables == null)
                {
                    r.Value.environment_variables = new();
                }

                r.Value.environment_variables["DOTNET_gcServer"] = "0";
            }

            workstation.Name = "Workstation";
            workstation.microbenchmarks_path = inputConfiguration.microbenchmark_path;
            workstation.Output.Path = Path.Combine(microbenchmarkOutputPath, "Workstation");
            SaveConfiguration(workstation, microbenchmarkSuitePath, "Microbenchmarks_Workstation.yaml");

            // Server Runs.
            MicrobenchmarkConfiguration server = CreateBaseMicrobenchmarkSuite(inputConfiguration, destinationMicrobenchmarksToRun, destinationMicrobenchmarkInvocationCount);
            server.Name = "Server";
            foreach (var r in server.Runs)
            {
                if (r.Value.environment_variables == null)
                {
                    r.Value.environment_variables = new();
                }

                r.Value.environment_variables["DOTNET_gcServer"] = "1";
            }
            server.microbenchmarks_path = inputConfiguration.microbenchmark_path;
            server.Output.Path = Path.Combine(microbenchmarkOutputPath, "Server");
            SaveConfiguration(server, microbenchmarkSuitePath, "Microbenchmarks_Server.yaml");

            return microbenchmarkSuitePath;
        }

        internal static MicrobenchmarkConfiguration CreateBaseMicrobenchmarkSuite(InputConfiguration inputConfiguration, string microbenchmarkFilterFile, string microbenchmarkInvocationCountFile)
        {
            MicrobenchmarkConfiguration configuration = MicrobenchmarkConfigurationParser.Parse(_microbenchmarkBase);

            // Add runs.
            configuration.Runs = new();
            foreach (var corerun in inputConfiguration.coreruns)
            {
                configuration.Runs.Add(corerun.Key, new Core.Configurations.Microbenchmarks.Run
                {
                    corerun = corerun.Value.Path,
                    Name = corerun.Key,
                    environment_variables = corerun.Value.environment_variables
                });
            }

            // The first run is always the baseline.
            configuration.Runs.First().Value.is_baseline = true;

            // Microbenchmark Filter Path.
            configuration.MicrobenchmarkConfigurations.Filter = null;
            configuration.MicrobenchmarkConfigurations.FilterPath = microbenchmarkFilterFile;

            // Microbenchmark Invocation Count Path.
            configuration.MicrobenchmarkConfigurations.InvocationCountPath = microbenchmarkInvocationCountFile;

            // Update Trace Configuration type.
            configuration.TraceConfigurations.Type = inputConfiguration.trace_configuration_type.ToLower();

            // Output Path.
            string baseMicrobenchmarkPath = Path.Combine(inputConfiguration.output_path, "Microbenchmarks");
            Core.Utilities.TryCreateDirectory(baseMicrobenchmarkPath);

            configuration.Output.Path = baseMicrobenchmarkPath;
            return configuration;
        }

        internal static string CreateGCPerfSimSuite(InputConfiguration inputConfiguration, string suitePath)
        {
            string gcPerfSimSuitePath = Path.Combine(suitePath, "GCPerfSim");

            Core.Utilities.TryCreateDirectory(gcPerfSimSuitePath);

            string gcPerfSimOutputPath = Path.Combine(inputConfiguration.output_path, "GCPerfSim");
            Core.Utilities.TryCreateDirectory(gcPerfSimOutputPath);
            SaveConfiguration(GetBaseConfiguration(inputConfiguration, Path.Combine(gcPerfSimOutputPath, "LowVolatilityRun")), gcPerfSimSuitePath, "LowVolatilityRun.yaml");

            // Base Configuration = Workstation.
            /*
            These old configurations are commented out because of high volatility in results.
            SaveConfiguration(GetBaseConfiguration(inputConfiguration, Path.Combine(gcPerfSimOutputPath, "Normal_Workstation")), gcPerfSimSuitePath, "Normal_Workstation.yaml");
            SaveConfiguration(CreateNormalServerCase(inputConfiguration, Path.Combine(gcPerfSimOutputPath, "Normal_Server")), gcPerfSimSuitePath, "Normal_Server.yaml");
            SaveConfiguration(CreateLargePagesWithWorkstation(inputConfiguration, Path.Combine(gcPerfSimOutputPath, "LargePages_Workstation")), gcPerfSimSuitePath, "LargePages_Workstation.yaml");
            SaveConfiguration(CreateLargePagesWithServer(inputConfiguration, Path.Combine(gcPerfSimOutputPath, "LargePages_Server")), gcPerfSimSuitePath, "LargePages_Server.yaml");
            SaveConfiguration(CreateHighMemoryCase(inputConfiguration, Path.Combine(gcPerfSimOutputPath, "HighMemory")), gcPerfSimSuitePath, "HighMemory.yaml");
            SaveConfiguration(CreateLowMemoryContainerCase(inputConfiguration, Path.Combine(gcPerfSimOutputPath, "LowMemoryContainer")), gcPerfSimSuitePath, "LowMemoryContainer.yaml");
            */

            return gcPerfSimSuitePath;
        }

        internal static void SaveConfiguration(ConfigurationBase configuration, string outputPath, string fileName)
        {
            var serializedResult = _serializer.Serialize(configuration);
            File.WriteAllText(Path.Combine(outputPath, fileName), serializedResult);
        }

        internal static GCPerfSimConfiguration GetBaseConfiguration(InputConfiguration inputConfiguration, string name)
        {
            GCPerfSimConfiguration baseConfiguration = GCPerfSimConfigurationParser.Parse(_gcPerfSimBaseLowVolatility, isIncompleteConfiguration: true);
            baseConfiguration.Output.Path = Path.Combine(inputConfiguration.output_path, name);
            baseConfiguration.TraceConfigurations.Type = inputConfiguration.trace_configuration_type.ToLower();
            baseConfiguration.gcperfsim_configurations.gcperfsim_path = inputConfiguration.gcperfsim_path;
            baseConfiguration.coreruns = inputConfiguration.coreruns;
            baseConfiguration.linux_coreruns = inputConfiguration.linux_coreruns;

            baseConfiguration.Name = Path.GetFileNameWithoutExtension(name);

            if (inputConfiguration.environment_variables != null)
            {
                baseConfiguration.Environment.environment_variables = inputConfiguration.environment_variables;
            }
            int logicalProcessors = GetAppropriateLogicalProcessors();
            baseConfiguration.Environment.environment_variables["DOTNET_GCHeapCount"] = logicalProcessors.ToString("X");
            baseConfiguration.gcperfsim_configurations.Parameters["tc"] = (2 * logicalProcessors).ToString();

            return baseConfiguration;
        }

        internal static GCPerfSimConfiguration CreateNormalServerCase(InputConfiguration inputConfiguration, string name)
        {
            GCPerfSimConfiguration normalServerCase = GetBaseConfiguration(inputConfiguration, name);
            int logicalProcessors = GetAppropriateLogicalProcessors();

            // Adjust the common tc.
            // Adjust this with the specified GCHeapCount.
            normalServerCase.gcperfsim_configurations.Parameters["tc"] = (2 * logicalProcessors).ToString();
            normalServerCase.gcperfsim_configurations.Parameters["tagb"] = (30 * logicalProcessors).ToString();

            // Set the environment variables appropriately.
            normalServerCase.Environment.environment_variables["DOTNET_gcServer"] = "1";
            normalServerCase.Environment.environment_variables["DOTNET_GCHeapCount"] = logicalProcessors.ToString("x");
            normalServerCase.Name = Path.GetFileNameWithoutExtension(name);

            return normalServerCase;
        }

        internal static GCPerfSimConfiguration CreateHighMemoryCase(InputConfiguration inputConfiguration, string name)
        {
            GCPerfSimConfiguration highMemoryConfiguration = CreateNormalServerCase(inputConfiguration, name);

            highMemoryConfiguration.Runs.Clear();

            // Server Run.
            Core.Configurations.GCPerfSim.Run serverRun = new();
            serverRun.override_parameters = new();
            serverRun.override_parameters["tlgb"] = "3";
            serverRun.override_parameters["sohsi"] = "50";
            highMemoryConfiguration.Runs.Add("server", serverRun);

            // Workstation Run.
            Core.Configurations.GCPerfSim.Run workstationRun = new();
            workstationRun.override_parameters = new();
            workstationRun.override_parameters["tlgb"] = "3";
            workstationRun.override_parameters["sohsi"] = "50";
            workstationRun.environment_variables = new();
            workstationRun.environment_variables["DOTNET_gcServer"] = "0";
            highMemoryConfiguration.Runs.Add("workstation", workstationRun);

            highMemoryConfiguration.Environment.environment_variables["DOTNET_gcServer"] = "1";
            int logicalProcessors = GetAppropriateLogicalProcessors();
            highMemoryConfiguration.Environment.environment_variables["DOTNET_GCHeapCount"] = logicalProcessors.ToString("x");

            // Add the appropriate environment variables.
            highMemoryConfiguration.Environment.environment_variables["DOTNET_GCHeapHardLimit"] = "0x100000000";
            highMemoryConfiguration.Environment.environment_variables["DOTNET_GCTotalPhysicalMemory"] = "0x100000000";
            highMemoryConfiguration.Name = name;
            return highMemoryConfiguration;
        }

        internal static GCPerfSimConfiguration CreateLowMemoryContainerCase(InputConfiguration inputConfiguration, string name)
        {
            GCPerfSimConfiguration lowMemoryConfigurationCase = CreateNormalServerCase(inputConfiguration, name);

            lowMemoryConfigurationCase.Runs.Clear();

            // Server Run.
            Core.Configurations.GCPerfSim.Run serverRun = new();
            serverRun.override_parameters = new();
            serverRun.override_parameters["tc"] = "16";
            serverRun.override_parameters["tagb"] = "350";
            serverRun.override_parameters["tlgb"] = "0.45";
            lowMemoryConfigurationCase.Runs.Add("server", serverRun);

            // Workstation Run.
            Core.Configurations.GCPerfSim.Run workstationRun = new();
            workstationRun.override_parameters = new();
            workstationRun.override_parameters["tc"] = "2";
            workstationRun.override_parameters["tagb"] = "100";
            workstationRun.override_parameters["tlgb"] = "0.5";
            workstationRun.environment_variables = new();
            workstationRun.environment_variables["DOTNET_gcServer"] = "0";
            lowMemoryConfigurationCase.Runs.Add("workstation", workstationRun);

            lowMemoryConfigurationCase.Environment.environment_variables["DOTNET_gcServer"] = "1";
            lowMemoryConfigurationCase.Environment.environment_variables["DOTNET_GCHeapCount"] = "4";

            // Add the appropriate environment variables.
            lowMemoryConfigurationCase.Environment.environment_variables["DOTNET_GCHeapHardLimit"] = "0x23C34600";
            lowMemoryConfigurationCase.Environment.environment_variables["DOTNET_GCTotalPhysicalMemory"] = "0x23C34600";
            lowMemoryConfigurationCase.Name = name;
            return lowMemoryConfigurationCase;
        }

        internal static GCPerfSimConfiguration CreateLargePagesWithServer(InputConfiguration inputConfiguration, string name)
        {
            GCPerfSimConfiguration largePagesServer = CreateNormalServerCase(inputConfiguration, name);
            largePagesServer.Environment.environment_variables["DOTNET_GCLargePages"] = "1";
            // This is a particularly memory intensive test that needs to be revisited. (~40 GB needed)
            largePagesServer.Environment.environment_variables["DOTNET_GCHeapHardLimit"] = "0x960000000";
            largePagesServer.Name = name;
            return largePagesServer;
        }

        internal static GCPerfSimConfiguration CreateLargePagesWithWorkstation(InputConfiguration inputConfiguration, string name)
        {
            GCPerfSimConfiguration largePagesWorkstation = GetBaseConfiguration(inputConfiguration, name);
            largePagesWorkstation.Environment.environment_variables["DOTNET_GCLargePages"] = "1";
            // This is a particularly memory intensive test that needs to be revisited. (~40 GB needed)
            largePagesWorkstation.Environment.environment_variables["DOTNET_GCHeapHardLimit"] = "0x960000000";
            largePagesWorkstation.Name = name;
            return largePagesWorkstation;
        }

        internal static int GetAppropriateLogicalProcessors()
        {
            int logicalProcessors = System.Environment.ProcessorCount;
            switch (logicalProcessors)
            {
                case int lp when lp > 4: return logicalProcessors - 2;
                case int lp when lp > 2 && lp <= 4: return logicalProcessors - 1;
                default:
                    return logicalProcessors;
            }
        }
    }
}
