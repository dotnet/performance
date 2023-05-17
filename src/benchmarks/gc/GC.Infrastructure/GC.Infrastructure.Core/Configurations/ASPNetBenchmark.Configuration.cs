namespace GC.Infrastructure.Core.Configurations.ASPNetBenchmarks
{
    public sealed class ASPNetBenchmarksConfiguration : ConfigurationBase
    {
        public Dictionary<string, Run> Runs { get; set; }
        public Environment Environment { get; set; }
        public BenchmarkSettings benchmark_settings { get; set; }
        public Output Output { get; set; }
    }

    public sealed class Run : RunBase
    {
        public string? corerun { get; set; }
    }

    public class Environment
    {
        public Dictionary<string, string> environment_variables { get; set; } = new();
        public uint default_max_seconds { get; set; } = 300;
    }

    public class BenchmarkSettings
    {
        public string benchmark_file { get; set; }
    }

    public class Output : OutputBase { }
    public static class ASPNetBenchmarksConfigurationParser
    {
        public static ASPNetBenchmarksConfiguration Parse(string path)
        {
            // Preconditions.
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                throw new ArgumentNullException($"ASPNetBenchmarksConfigurationParser: {nameof(path)} is null/empty or doesn't exist. You must specify a valid path.");
            }

            string serializedConfiguration = File.ReadAllText(path);
            ASPNetBenchmarksConfiguration configuration = Common.Deserializer.Deserialize<ASPNetBenchmarksConfiguration>(serializedConfiguration);

            // Checks if mandatory arguments are specified in the configuration.
            if (configuration == null)
            {
                throw new ArgumentNullException($"ASPNetBnechmarksConfigurationParser: {nameof(configuration)} is null. Check the syntax of the configuration.");
            }

            return configuration;
        }
    }
}
