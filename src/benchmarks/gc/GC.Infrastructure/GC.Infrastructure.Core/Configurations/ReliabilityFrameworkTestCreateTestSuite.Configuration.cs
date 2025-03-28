using YamlDotNet.Serialization;

namespace GC.Infrastructure.Core.Configurations
{
    public sealed class ReliabilityFrameworkTestCreateTestSuiteConfiguration
    {
        public string OutputFolder { get; set; }
        public string CoreRoot { get; set; }
        public string ReliabilityFrameworkDll { get; set; }
        public bool EnableStressMode { get; set; }
        public string GCPerfSimDll { get; set; }        
        public string TestFolder { get; set; }
    }

    public static class ReliabilityFrameworkTestCreateTestSuiteConfigurationParser
    {
        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

        public static ReliabilityFrameworkTestCreateTestSuiteConfiguration Parse(string path)
        {
            // Preconditions.
            ConfigurationChecker.VerifyFile(path, nameof(ReliabilityFrameworkTestCreateTestSuiteConfigurationParser));

            string serializedConfiguration = File.ReadAllText(path);
            ReliabilityFrameworkTestCreateTestSuiteConfiguration? configuration = null;

            // This try catch is here because the exception from the YamlDotNet isn't helpful and must be imbued with more details.
            try
            {
                configuration = _deserializer.Deserialize<ReliabilityFrameworkTestCreateTestSuiteConfiguration>(serializedConfiguration);
            }

            catch (Exception ex)
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestCreateTestSuiteConfiguration)}: Unable to parse the yaml file because of an error in the syntax. Exception: {ex.Message} \n Call Stack: {ex.StackTrace}");
            }

            if (string.IsNullOrEmpty(configuration.OutputFolder))
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestCreateTestSuiteConfiguration)}: Please specify output folder");
            }

            if (string.IsNullOrEmpty(configuration.ReliabilityFrameworkDll) )
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestCreateTestSuiteConfiguration)}: Please specify ReliabilityFrameworkDll");
            }

            if (!Path.Exists(configuration.CoreRoot))
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestCreateTestSuiteConfiguration)}: Given CoreRoot path is not valid");
            }

            if (string.IsNullOrEmpty(configuration.GCPerfSimDll))
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestCreateTestSuiteConfiguration)}: Please specify GCPerfSimDll");
            }

            if (!Path.Exists(configuration.TestFolder))
            {
                throw new ArgumentException($"{nameof(ReliabilityFrameworkTestCreateTestSuiteConfiguration)}: Given TestFolder path is not valid");
            }

            return configuration;
        }
    }
}