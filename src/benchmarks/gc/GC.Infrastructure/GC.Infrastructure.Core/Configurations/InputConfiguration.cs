namespace GC.Infrastructure.Core.Configurations
{
    public sealed class InputConfiguration
    {
        public string output_path                   { get; set; }
        public string gcperfsim_path                { get; set; }
        public string microbenchmark_path           { get; set; }
        public Dictionary<string, CoreRunInfo> coreruns { get; set; }
        public Dictionary<string, CoreRunInfo>? linux_coreruns { get; set; }
        public Dictionary<string, string>? environment_variables { get; set; }
        public string trace_configuration_type { get; set; } = "gc";
        // TODO: Add this feature.
        // public Dictionary<string, string>? clrgcs   { get; set; }
        // public string? debug_parameters             { get; set; }

        public Dictionary<string, string>? symbol_path { get; set; }
        public Dictionary<string, string>? source_path { get; set; }
    }

    public static class InputConfigurationParser
    {
        public static InputConfiguration Parse(string path)
        {
            string serializedConfiguration = File.ReadAllText(path);
            InputConfiguration configuration = Common.Deserializer.Deserialize<InputConfiguration>(serializedConfiguration);

            // Preconditions.
            if (configuration.coreruns == null)
            {
                throw new ArgumentException($"{nameof(InputConfigurationParser)}: Provide a set of coreruns use for the analysis.");                
            }
            foreach (CoreRunInfo corerun in configuration.coreruns.Values)
            {
                if (string.IsNullOrEmpty(corerun.Path) || !File.Exists(corerun.Path))
                {
                    throw new ArgumentException($"{nameof(InputConfigurationParser)}: CoreRun.exe must be provided or exist.");                
                }
            }

            if (string.IsNullOrEmpty(configuration.output_path))
            {
                throw new ArgumentException($"{nameof(InputConfigurationParser)}: Provide an output path.");
            }

            if (string.IsNullOrEmpty(configuration.gcperfsim_path) || !File.Exists(configuration.gcperfsim_path))
            {
                throw new ArgumentException($"{nameof(InputConfigurationParser)}: A path to the gcperfsim dll must be provided or exist.");
            }

            string microbenchmarkExecutablePath = Path.Combine(configuration.microbenchmark_path, "../../../artifacts/bin/MicroBenchmarks/Release/net7.0/MicroBenchmarks.exe");

            if (string.IsNullOrEmpty(configuration.microbenchmark_path) || !Directory.Exists(configuration.microbenchmark_path) || !File.Exists(microbenchmarkExecutablePath))
            {
                throw new ArgumentException($"{nameof(InputConfigurationParser)}: A path to the microbenchmarks must be provided or exist.");
            }

            return configuration;
        }
    }
}
