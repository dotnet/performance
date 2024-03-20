namespace GC.Infrastructure.Core.Configurations.ASPNetBenchmarks
{
    public sealed class ASPNetBenchmarksConfiguration : ConfigurationBase
    {
        public Dictionary<string, Run>? Runs { get; set; }
        public Environment Environment { get; set; }
        public BenchmarkSettings? benchmark_settings { get; set; }
        public Output? Output { get; set; }
    }

    public sealed class Run : RunBase
    {
        public string? corerun { get; set; }
        public string? framework_version { get; set; }
    }

    public class Environment
    {
        public Dictionary<string, string> environment_variables { get; set; } = new();
        public uint default_max_seconds { get; set; } = 300;
        public string framework_version { get; set; } = "net8.0";
    }

    public class BenchmarkSettings
    {
        public string? benchmark_file { get; set; }
        public string additional_arguments { get; set; } = "";
        public List<string> benchmarkFilters { get; set; } = new();
    }

    public class Output : OutputBase { }
    public static class ASPNetBenchmarksConfigurationParser
    {
        public static ASPNetBenchmarksConfiguration Parse(string path)
        {
            // Preconditions.
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                throw new ArgumentNullException($"{nameof(ASPNetBenchmarksConfigurationParser)}: {nameof(path)} is null/empty or doesn't exist. You must specify a valid path.");
            }

            string serializedConfiguration = File.ReadAllText(path);

            ASPNetBenchmarksConfiguration? configuration = null;
            try
            {
                configuration = Common.Deserializer.Deserialize<ASPNetBenchmarksConfiguration>(serializedConfiguration);
            }

            catch (Exception ex)
            {
                throw new ArgumentException($"{nameof(ASPNetBenchmarksConfiguration)}: Unable to parse the yaml file because of an error in the syntax. Please use the configurations under: Configuration/GCPerfSim/*.yaml in as example to ensure the file is formatted correctly. Exception: {ex.Message} \n Call Stack: {ex.StackTrace}");
            }

            // Checks if mandatory arguments are specified in the configuration.
            if (configuration == null)
            {
                throw new ArgumentNullException($"{nameof(ASPNetBenchmarksConfigurationParser)}: {nameof(configuration)} is null. Check the syntax of the configuration.");
            }

            return configuration;
        }
    }
}
