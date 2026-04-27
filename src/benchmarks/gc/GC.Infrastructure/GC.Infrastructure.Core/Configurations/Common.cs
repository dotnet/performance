using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GC.Infrastructure.Core.Configurations
{
    public static class Common
    {
        private static readonly Lazy<IDeserializer> _deserializer =
            new Lazy<IDeserializer>(new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build());
        private static readonly Lazy<ISerializer> _serializer =
            new Lazy<ISerializer>(new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build());
        public static IDeserializer Deserializer => _deserializer.Value;
        public static ISerializer Serializer => _serializer.Value;
    }

    public class CoreRunInfo
    {
        public string Path { get; set; }
        public Dictionary<string, string> environment_variables { get; set; }
    }

    public static class ConfigurationChecker
    {
        public static void VerifyFile(string configurationPath, string prefix)
        {
            // Parse configuration + Precondition checks.
            if (string.IsNullOrEmpty(configurationPath) || !File.Exists(configurationPath))
            {
                throw new ArgumentNullException($"{prefix}: The provided path to yaml file {nameof(configurationPath)} doesn't exist or is empty - please ensure you are passing in a valid .yaml file.");
            }

            if (Path.GetExtension(configurationPath) != ".yaml")
            {
                throw new ArgumentNullException($"{prefix}: A yaml file wasn't provided as the configuration.");
            }
        }

        public static void VerifyEnvironmentVariables(Dictionary<string, string>? environmentVariables, string prefix)
        {
            // If there are no environment variables set, ignore.
            if (environmentVariables == null)
            {
                return;
            }

            else
            {
                foreach (var env in environmentVariables)
                {
                    if (env.Key.ToLower().StartsWith("complus_"))
                    {
                        throw new ArgumentException($"{prefix}: COMPlus Environment variables are disallowed. Please replace it with it's DOTNET equivalent.");
                    }
                }
            }
        }
    }
}
