using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GC.Infrastructure.Core.Configurations.GCPerfSim
{
    public sealed class GCPerfSimConfiguration : ConfigurationBase
    {
        public Dictionary<string, Run>? Runs { get; set; }
        public GCPerfSimConfigurations? gcperfsim_configurations { get; set; }
        public Environment Environment { get; set; } = new();
        public Dictionary<string, CoreRunInfo>? coreruns { get; set; }
        public Dictionary<string, CoreRunInfo>? linux_coreruns { get; set; }
        public Output? Output { get; set; }
    }

    public sealed class Run : RunBase
    {
        public Dictionary<string, string>? override_parameters { get; set; }
    }

    public sealed class Output : OutputBase { }

    public class GCPerfSimConfigurations
    {
        public Dictionary<string, string> Parameters { get; set; } = new();
        public string? gcperfsim_path { get; set; }
    }

    public class ClrGcRunInfo
    {
        public Dictionary<string, string>? paths { get; set; }
        public string? corerun { get; set; }
    }

    public class Environment
    {
        public Dictionary<string, string> environment_variables { get; set; } = new();
        public uint default_max_seconds { get; set; } = 300;
        public uint Iterations { get; set; } = 1;
    }
    public static class GCPerfSimConfigurationParser
    {
        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();

        public static GCPerfSimConfiguration Parse(string path, bool isIncompleteConfiguration = false)
        {
            // Preconditions.
            ConfigurationChecker.VerifyFile(path, nameof(GCPerfSimConfigurationParser));

            string serializedConfiguration = File.ReadAllText(path);
            GCPerfSimConfiguration? configuration = null;

            // This try catch is here because the exception from the YamlDotNet isn't helpful and must be imbued with more details.
            try
            {
                configuration = _deserializer.Deserialize<GCPerfSimConfiguration>(serializedConfiguration);
            }

            catch (Exception ex)
            {
                throw new ArgumentException($"{nameof(GCPerfSimConfiguration)}: Unable to parse the yaml file because of an error in the syntax. Please use the configurations under: Configuration/GCPerfSim/*.yaml in as example to ensure the file is formatted correctly. Exception: {ex.Message} \n Call Stack: {ex.StackTrace}");
            }

            // Check to make sure we get a valid configuration.
            if (configuration == null)
            {
                throw new ArgumentNullException($"{nameof(GCPerfSimConfigurationParser)}: {nameof(configuration)} is null. Check the syntax of the configuration.");
            }

            // Check to ensure gcperfsim_configurations field exists.
            if (configuration.gcperfsim_configurations == null)
            {
                throw new ArgumentException($"{nameof(GCPerfSimConfigurationParser)}: The configuration is missing the `gcperfsim_configuration` field in the yaml. Please add it following the example: Configuration/GCPerfSim/*.yaml.");
            }

            // The rest of the items aren't filled for the incomplete configuration that's programmatically filled by the infrastructure.
            if (isIncompleteConfiguration)
            {
                return configuration;
            }

            // Check to ensure the GCPerfSim configuration binaries exist.
            if (string.IsNullOrEmpty(configuration.gcperfsim_configurations.gcperfsim_path) || !File.Exists(configuration.gcperfsim_configurations.gcperfsim_path))
            {
                throw new ArgumentException($"{nameof(GCPerfSimConfigurationParser)}: The GCPerfSim binary either doesn't exist or the path provided is incorrect. Please ensure the path is valid; the path provided: {configuration.gcperfsim_configurations.gcperfsim_path}");
            }

            // Check to ensure the runs are valid.
            if (configuration.Runs == null || configuration.Runs?.Count == 0)
            {
                throw new ArgumentNullException($"{nameof(GCPerfSimConfigurationParser)}: {nameof(configuration.Runs)} are null or empty. 1 or more runs should be specified.");
            }

            // Check to ensure there are some coreruns passed in.
            if (configuration.coreruns == null || configuration.coreruns?.Count == 0)
            {
                throw new ArgumentNullException($"{nameof(GCPerfSimConfigurationParser)}: {nameof(configuration.coreruns)} are null or empty. 1 or more builds should be specified.");
            }

            // Check to ensure the builds are valid i.e., have an existent path to corerun.
            foreach (var build in configuration.coreruns!)
            {
                if (string.IsNullOrEmpty(build.Value.Path) ||
                    (!File.Exists(build.Value.Path) && !Directory.Exists(build.Value.Path)))
                {
                    throw new ArgumentException($"{nameof(GCPerfSimConfigurationParser)}: The corerun for {build.Key} either doesn't exist or the path provided is incorrect. Please ensure that path points to a valid corerun");
                }

                // If corerun path is a directory, make sure it contains corerun binary.
                if (Directory.Exists(build.Value.Path) &&
                    !Directory.EnumerateFiles(build.Value.Path)
                        .Any(filePath => Path.GetFileNameWithoutExtension(filePath) == "corerun"))
                {
                    throw new ArgumentException($"{nameof(GCPerfSimConfigurationParser)}: The corerun for {build.Key} is incorrect. Please ensure that path points to a valid core root");
                }
            }

            // Parameters.
            if (configuration.gcperfsim_configurations.Parameters == null || configuration.gcperfsim_configurations.Parameters.Count == 0)
            {
                throw new ArgumentException($"{nameof(GCPerfSimConfigurationParser)}: {nameof(configuration.gcperfsim_configurations.Parameters)} are null or empty. GC PerfSim Parameters must be specified.");
            }

            // Trace Configurations if specified, must have a type specified.
            if (configuration.TraceConfigurations != null && string.IsNullOrEmpty(configuration.TraceConfigurations.Type))
            {
                throw new ArgumentException($"{nameof(GCPerfSimConfigurationParser)}: Please ensure a trace configuration type is specified. If you don't want to collect a trace, simply don't include the trace_configuration type or choose either: gc, verbose, cpu, cpu_managed, threadtime, threadtime_managed.");
            }

            // If the user passes in a null output path, default to the current directory.
            if (string.IsNullOrEmpty(configuration.Output?.Path))
            {
                configuration.Output!.Path = Directory.GetCurrentDirectory();
            }

            // Check if the user passes any COMPlus environment variables.
            ConfigurationChecker.VerifyEnvironmentVariables(configuration.Environment.environment_variables, $"{nameof(GCPerfSimConfigurationParser)}");
            foreach (var corerun in configuration.coreruns)
            {
                ConfigurationChecker.VerifyEnvironmentVariables(corerun.Value.environment_variables, $"{nameof(GCPerfSimConfigurationParser)} for Corerun: {corerun.Key}");
            }

            foreach (var run in configuration.Runs!)
            {
                ConfigurationChecker.VerifyEnvironmentVariables(run.Value.environment_variables, $"{nameof(GCPerfSimConfigurationParser)} for Run: {run.Key}");
            }

            return configuration;
        }
    }
}
