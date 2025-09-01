using YamlDotNet.Serialization;

namespace GC.Infrastructure.Core.Configurations
{
    public sealed class RFCreateSuitesConfiguration
    {
        /// <summary>
        /// Gets or sets the output path
        /// </summary>
        public string OutputFolder { get; set; }

        /// <summary>
        /// Gets or sets the Core_Root path
        /// </summary>
        public string Core_Root { get; set; }

        /// <summary>
        /// Gets or sets the Reliability Framework DLL path
        /// </summary>
        public string ReliabilityFrameworkDll { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the test is running in stress mode.
        /// </summary>
        public bool EnableStressMode { get; set; }

        /// <summary>
        /// Gets or sets the GCPerfSim DLL path.
        /// </summary>
        public string GCPerfSimDll { get; set; } 
        
        /// <summary>
        /// Gets or sets the path to the test folder.
        /// </summary>
        public string TestFolder { get; set; }
    }

    public static class RFCreateSuitesConfigurationParser
    {
        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

        public static RFCreateSuitesConfiguration Parse(string path)
        {
            // Preconditions.
            ConfigurationChecker.VerifyFile(path, nameof(RFCreateSuitesConfigurationParser));

            string serializedConfiguration = File.ReadAllText(path);
            RFCreateSuitesConfiguration? configuration = null;

            // This try catch is here because the exception from the YamlDotNet isn't helpful and must be imbued with more details.
            try
            {
                configuration = _deserializer.Deserialize<RFCreateSuitesConfiguration>(serializedConfiguration);
            }

            catch (Exception ex)
            {
                throw new ArgumentException($"{nameof(RFCreateSuitesConfiguration)}: Unable to parse the yaml file because of an error in the syntax. Exception: {ex.Message} \n Call Stack: {ex.StackTrace}");
            }

            if (string.IsNullOrEmpty(configuration.OutputFolder))
            {
                throw new ArgumentException($"{nameof(RFCreateSuitesConfiguration)}: Please specify output folder");
            }

            if (string.IsNullOrEmpty(configuration.ReliabilityFrameworkDll) )
            {
                throw new ArgumentException($"{nameof(RFCreateSuitesConfiguration)}: Please specify ReliabilityFrameworkDll");
            }

            if (!Path.Exists(configuration.Core_Root))
            {
                throw new ArgumentException($"{nameof(RFCreateSuitesConfiguration)}: Given CoreRoot path is not valid");
            }

            if (string.IsNullOrEmpty(configuration.GCPerfSimDll))
            {
                throw new ArgumentException($"{nameof(RFCreateSuitesConfiguration)}: Please specify GCPerfSimDll");
            }

            if (!Path.Exists(configuration.TestFolder))
            {
                throw new ArgumentException($"{nameof(RFCreateSuitesConfiguration)}: Given TestFolder path is not valid");
            }

            return configuration;
        }
    }
}