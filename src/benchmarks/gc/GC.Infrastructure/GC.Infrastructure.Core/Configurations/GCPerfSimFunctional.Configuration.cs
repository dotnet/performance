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
        public Dictionary<string, string>? environment_variables { get; set; }
        public string trace_configuration_type { get; set; } = "gc";
    }

    public static class GCPerfSimFunctionalConfigurationParser
    {
        public static GCPerfSimFunctionalConfiguration Parse(string path, bool isIncompleteConfiguration = false)
        {
            string serializedConfiguration = File.ReadAllText(path);
            GCPerfSimFunctionalConfiguration configuration = Common.Deserializer.Deserialize<GCPerfSimFunctionalConfiguration>(serializedConfiguration);

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

            return configuration;
        }
    }
}
