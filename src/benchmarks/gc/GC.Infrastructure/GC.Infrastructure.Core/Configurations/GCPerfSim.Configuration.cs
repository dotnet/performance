using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GC.Infrastructure.Core.Configurations.GCPerfSim
{
    public sealed class GCPerfSimConfiguration : ConfigurationBase
    {
        public Dictionary<string, Run> Runs { get; set; }
        public GCPerfSimConfigurations gcperfsim_configurations { get; set; }
        public Environment Environment { get; set; } = new();
        public Dictionary<string, CoreRunInfo> coreruns { get; set; }
        public Dictionary<string, CoreRunInfo>? linux_coreruns { get; set; }
        public Output Output { get; set; }
    }

    public sealed class Run : RunBase 
    { 
        public Dictionary<string, string>? override_parameters { get; set; }
    }

    public sealed class Output : OutputBase {}

    public class GCPerfSimConfigurations
    {
        public Dictionary<string, string> Parameters { get; set; } = new();
        public string gcperfsim_path { get; set; }
    }

    public class ClrGcRunInfo
    {
        public Dictionary<string, string>? paths { get; set; }
        public string corerun { get; set; }
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

        public static GCPerfSimConfiguration Parse(string path)
        {
            // Preconditions.
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                throw new ArgumentNullException($"{nameof(GCPerfSimConfigurationParser)}: {nameof(path)} is null/empty or doesn't exist. You must specify a valid path.");
            }

            string serializedConfiguration = File.ReadAllText(path);
            GCPerfSimConfiguration configuration = _deserializer.Deserialize<GCPerfSimConfiguration>(serializedConfiguration);

            // Checks if mandatory arguments are specified in the configuration.
            if (configuration == null)
            {
                throw new ArgumentNullException($"{nameof(GCPerfSimConfigurationParser)}: {nameof(configuration)} is null. Check the syntax of the configuration.");
            }

            // Runs.
            if (configuration.Runs == null || configuration.Runs?.Count == 0)
            {
                throw new ArgumentNullException($"{nameof(configuration.Runs)} are null or empty. 1 or more runs should be specified.");
            }

            // Parameters.
            if (configuration.gcperfsim_configurations.Parameters == null || configuration.gcperfsim_configurations.Parameters.Count == 0)
            {
                throw new ArgumentNullException($"{nameof(GCPerfSimConfigurationParser)}: {nameof(configuration.gcperfsim_configurations.Parameters)} are null or empty. GC Perf Sim Parameters must be specified."); 
            }

            // Trace Configurations if specified, must have a type specified.
            if (configuration.TraceConfigurations != null && string.IsNullOrEmpty(configuration.TraceConfigurations.Type))
            {
                throw new ArgumentNullException($"{nameof(GCPerfSimConfigurationParser)}: {nameof(configuration.TraceConfigurations.Type)} is null or empty. This value should be specified if the a 'trace_configurations' node is added");
            }

            return configuration;
        }
    }
}
