using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GC.Infrastructure.Core.Configurations
{
    public sealed class GCPerfSimFunctionalConfiguration
    {
        public string output_path { get; set; }
        public string gcperfsim_path { get; set; }
        public Dictionary<string, CoreRunInfo> coreruns { get; set; }
        public Environment Environment { get; set; } = new();
        public string trace_configuration_type { get; set; } = "gc";
    }
    public class Environment
    {
        public Dictionary<string, string> environment_variables { get; set; } = new();
    }

    public static class GCPerfSimFunctionalConfigurationParser
    {
        public static GCPerfSimFunctionalConfiguration Parse(string path)
        {
            string serializedConfiguration = File.ReadAllText(path);

            GCPerfSimFunctionalConfiguration? configuration = null;

            try
            {
                configuration = Common.Deserializer.Deserialize<GCPerfSimFunctionalConfiguration>(serializedConfiguration);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"{nameof(GCPerfSimFunctionalConfigurationParser)}: Unable to parse the yaml file because of an error in the syntax. Please use the configurations under: Configuration/GCPerfSim/*.yaml in as example to ensure the file is formatted correctly. Exception: {ex.Message} \n Call Stack: {ex.StackTrace}");
            }

            // Preconditions.
            if (configuration.coreruns == null)
            {
                throw new ArgumentException($"{nameof(GCPerfSimFunctionalConfigurationParser)}: Provide a set of coreruns use for the analysis.");
            }

            if (string.IsNullOrEmpty(configuration.output_path))
            {
                throw new ArgumentException($"{nameof(GCPerfSimFunctionalConfigurationParser)}: Provide an output path.");
            }

            if (string.IsNullOrEmpty(configuration.gcperfsim_path) || !File.Exists(configuration.gcperfsim_path))
            {
                throw new ArgumentException($"{nameof(GCPerfSimFunctionalConfigurationParser)}: A path to the gcperfsim dll must be provided or exist.");
            }

            if (configuration.trace_configuration_type == null || string.IsNullOrEmpty(configuration?.trace_configuration_type))
            {
                throw new ArgumentException($"{nameof(GCPerfSimFunctionalConfigurationParser)}: Please provide the trace_configuration type");
            }

            // Check if COMPlus_ environment variables.
            foreach (var run in configuration.coreruns!)
            {
                ConfigurationChecker.VerifyEnvironmentVariables(run.Value.environment_variables, $"{nameof(GCPerfSimFunctionalConfigurationParser)} for Run: {run.Key}");
            }
            ConfigurationChecker.VerifyEnvironmentVariables(configuration.Environment.environment_variables, $"{nameof(GCPerfSimFunctionalConfigurationParser)}");

            return configuration;
        }
    }
}
