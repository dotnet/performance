using YamlDotNet.Serialization;

namespace GC.Infrastructure.Core.Configurations
{
    public sealed class RFAnalyzeConfiguration
    {
        public required string DebuggerPath { get; set; }
        public required List<string> StackFrameKeyWords { get; set; }
        public required string Core_Root { get; set; }
        public required string WSLInstanceLocation { get; set; }
        public required string DumpFolder { get; set; }
        public required string AnalyzeOutputFolder { get; set; }
    }

    public static class RFAnalyzeConfigurationParser
    {
        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

        public static RFAnalyzeConfiguration Parse(string path)
        {
            // Preconditions.
            ConfigurationChecker.VerifyFile(path, nameof(RFAnalyzeConfigurationParser));

            string serializedConfiguration = File.ReadAllText(path);
            RFAnalyzeConfiguration? configuration = null;

            // This try catch is here because the exception from the YamlDotNet isn't helpful and must be imbued with more details.
            try
            {
                configuration = _deserializer.Deserialize<RFAnalyzeConfiguration>(serializedConfiguration);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"{nameof(RFAnalyzeConfiguration)}: Unable to parse the yaml file because of an error in the syntax. Exception: {ex.Message} \n Call Stack: {ex.StackTrace}");
            }

            if (String.IsNullOrEmpty(configuration.AnalyzeOutputFolder))
            {
                throw new ArgumentException($"{nameof(RFAnalyzeConfiguration)}: Provide a analyze output folder.");
            }

            if (!Path.Exists(configuration.DumpFolder))
            {
                throw new ArgumentException($"{nameof(RFAnalyzeConfiguration)}: Dump folder doesn't exist.");
            }

            // Check Core_Root folder
            if (!Path.Exists(configuration.Core_Root))
            {
                throw new ArgumentException($"{nameof(RFAnalyzeConfiguration)}: Core_Root doesn't exist.");
            }
            bool hasCoreRun = Directory.GetFiles(configuration.Core_Root)
                .Any(filePath => Path.GetFileNameWithoutExtension(filePath) == "corerun");
            if (!hasCoreRun)
            {
                throw new ArgumentException($"{nameof(RFAnalyzeConfiguration)}: Provide a valid Core_Root.");
            }

            return configuration;
        }
    }
}
