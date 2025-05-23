using YamlDotNet.Serialization;

namespace GC.Infrastructure.Core.Configurations.ReliabilityFrameworkTest
{
    public sealed class ReliabilityFrameworkTestAnalyzeConfiguration
    {
        public required string DebuggerPath { get; set; }
        public required List<string> StackFrameKeyWords { get; set; }
        public required string CoreRoot { get; set; }
        public required string WSLInstanceLocation { get; set; }
        public required string DumpFolder { get; set; }
        public required string AnalyzeOutputFolder { get; set; }
    }

    public static class ReliabilityFrameworkTestAnalyzeConfigurationParser
    {
        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

        public static ReliabilityFrameworkTestAnalyzeConfiguration Parse(string path)
        {
            // Preconditions.
            ConfigurationChecker.VerifyFile(path, nameof(ReliabilityFrameworkTestAnalyzeConfigurationParser));

            string serializedConfiguration = File.ReadAllText(path);
            ReliabilityFrameworkTestAnalyzeConfiguration? configuration = null;

            // This try catch is here because the exception from the YamlDotNet isn't helpful and must be imbued with more details.
            try
            {
                configuration = _deserializer.Deserialize<ReliabilityFrameworkTestAnalyzeConfiguration>(serializedConfiguration);
            }

            catch (Exception ex)
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestAnalyzeConfiguration)}: Unable to parse the yaml file because of an error in the syntax. Exception: {ex.Message} \n Call Stack: {ex.StackTrace}");
            }

            if (String.IsNullOrEmpty(configuration.AnalyzeOutputFolder))
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestAnalyzeConfiguration)}: Provide a analyze output folder.");
            }

            if (!Path.Exists(configuration.DumpFolder))
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestAnalyzeConfiguration)}: Dump folder doesn't exist.");
            }

            // Check Core_Root folder
            if (!Path.Exists(configuration.CoreRoot))
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestAnalyzeConfiguration)}: Core_Root doesn't exist.");
            }
            bool hasCoreRun = Directory.GetFiles(configuration.CoreRoot)
                .Any(filePath => Path.GetFileNameWithoutExtension(filePath) == "corerun");
            if (!hasCoreRun)
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestAnalyzeConfiguration)}: Provide a valid Core_Root.");
            }

            return configuration;
        }
    }
}
