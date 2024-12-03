using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GC.Infrastructure.Core.Configurations.Microbenchmarks
{
    public sealed class MicrobenchmarkConfiguration : ConfigurationBase
    {
        public string microbenchmarks_path { get; set; }
        public Dictionary<string, Run> Runs { get; set; }
        public MicrobenchmarkConfigurations MicrobenchmarkConfigurations { get; set; }
        public Environment Environment { get; set; }
        public Output Output { get; set; }
        public string? Path { get; set; }
    }

    public sealed class Run : RunBase
    {
        public string? DotnetInstaller { get; set; }
        public string? Name { get; set; }
        public string? corerun { get; set; }
        public bool is_baseline { get; set; }
    }

    public class Environment
    {
        public uint default_max_seconds { get; set; } = 300;
        public uint iteration { get; set; } = 1;
    }

    public sealed class MicrobenchmarkConfigurations
    {
        public string? Filter { get; set; }
        public string? FilterPath { get; set; }
        public string? InvocationCountPath { get; set; }
        public string DotnetInstaller { get; set; }
        public string? bdn_arguments { get; set; } = null;
    }

    public sealed class Output : OutputBase
    {
        public List<string> cpu_columns { get; set; }
        public List<string> additional_report_metrics { get; set; }
        public List<string>? run_comparisons { get; set; }
    }
    public static class MicrobenchmarkConfigurationParser
    {
        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();

        public static MicrobenchmarkConfiguration Parse(string path)
        {
            // Preconditions.
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                throw new ArgumentNullException($"{nameof(path)} is null/empty or doesn't exist. You must specify a valid path.");
            }

            string serializedConfiguration = File.ReadAllText(path);
            MicrobenchmarkConfiguration configuration = _deserializer.Deserialize<MicrobenchmarkConfiguration>(serializedConfiguration);

            // Checks if mandatory arguments are specified in the configuration.
            if (configuration == null)
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: {nameof(configuration)} is null. Check the syntax of the configuration.");
            }

            // Microbenchmark Configurations.
            if (configuration.MicrobenchmarkConfigurations == null)
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: {nameof(configuration.MicrobenchmarkConfigurations)} is null. This is a default node in the yaml that should be specified.");
            }

            if (string.IsNullOrEmpty(configuration.MicrobenchmarkConfigurations.DotnetInstaller))
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: {nameof(configuration.MicrobenchmarkConfigurations.DotnetInstaller)} is null. A framework version must be specified e.g. 'net7.0'");
            }

            // Trace Configurations must have a type specified.
            if (configuration.TraceConfigurations != null && string.IsNullOrEmpty(configuration.TraceConfigurations.Type))
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: {nameof(configuration.TraceConfigurations.Type)} is null or empty. This value should be specified if the a 'trace_configurations' node is added");
            }

            // Check if COMPlus_ environment variables.
            if (configuration.Runs != null)
            {
                foreach (var run in configuration.Runs!)
                {
                    ConfigurationChecker.VerifyEnvironmentVariables(run.Value.environment_variables, $"{nameof(MicrobenchmarkConfigurationParser)} for Run: {run.Key}");
                }
            }

            return configuration;
        }
    }
}
