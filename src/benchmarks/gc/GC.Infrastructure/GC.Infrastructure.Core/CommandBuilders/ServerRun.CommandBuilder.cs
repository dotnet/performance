using GC.Infrastructure.Core.TraceCollection;

namespace GC.Infrastructure.Core.CommandBuilders
{
    public enum OS
    {
        Windows,
        Linux
    }

    public static class ServerRunCommandBuilder
    {
        public static List<KeyValuePair<string, string>> GenerateKeyValuePairListForUploadFiles(string? uploadFilesPath)
        {
            List<KeyValuePair<string, string>> commandKVPList = new();
            string artifactsToUpload = Directory.Exists(uploadFilesPath!) ? Path.Combine(uploadFilesPath!, "*.*") : uploadFilesPath!;
            commandKVPList.Add(new("application.options.outputFiles", artifactsToUpload));
            return commandKVPList;
        }

        public static List<KeyValuePair<string, string>> GenerateKeyValuePairListForGettingLogs(string logDownloadPathWithoutExtension)
        {
            List<KeyValuePair<string, string>> commandKVPList = new();
            commandKVPList.Add(new("application.options.downloadOutput", "true"));
            commandKVPList.Add(new("application.options.downloadOutputOutput", $"{logDownloadPathWithoutExtension}.output.log"));
            commandKVPList.Add(new("application.options.downloadBuildLog", "true"));
            commandKVPList.Add(new("application.options.downloadBuildLogOutput", $"{logDownloadPathWithoutExtension}.build.log"));
            commandKVPList.Add(new("json", $"{logDownloadPathWithoutExtension}.json"));
            return commandKVPList;
        }

        public static List<KeyValuePair<string, string>> GenerateKeyValuePairListForFramework(string frameworkVersion)
        {
            List<KeyValuePair<string, string>> commandKVPList = new();
            commandKVPList.Add(new("application.framework", frameworkVersion));
            return commandKVPList;
        }

        public static List<KeyValuePair<string, string>> GenerateKeyValuePairListForEnvironmentVariables(Dictionary<string, string> environmentVariables)
        {
            return environmentVariables
                .Select(kv => new KeyValuePair<string, string>("application.environmentVariables", $"{kv.Key}={kv.Value}"))
                .ToList();
        }

        public static List<KeyValuePair<string, string>> GenerateKeyValuePairListForParameters(Dictionary<string, string> parameters)
        {
            return parameters
                .Select(kv => new KeyValuePair<string, string>($"application.variables.{kv.Key}", kv.Value))
                .ToList();
        }

        public static List<KeyValuePair<string, string>> GenerateKeyValuePairListForConfig(string configPath)
        {
            List<KeyValuePair<string, string>> commandKVPList = new();
            commandKVPList.Add(new("config", configPath));
            return commandKVPList;
        }

        public static List<KeyValuePair<string, string>> GenerateKeyValuePairListForProfile(string profilePath)
        {
            List<KeyValuePair<string, string>> commandKVPList = new();
            commandKVPList.Add(new("profile", profilePath));
            return commandKVPList;
        }

        public static List<KeyValuePair<string, string>> GenerateKeyValuePairListForScenario(string scenario)
        {
            List<KeyValuePair<string, string>> commandKVPList = new();
            commandKVPList.Add(new("scenario", scenario));
            return commandKVPList;
        }

        public static List<KeyValuePair<string, string>> GenerateKeyValuePairListForTrace(string? traceType, string tracePath, OS os)
        {
            List<KeyValuePair<string, string>> commandKVPList = new();
            CollectType collectType = TraceCollector.StringToCollectTypeMap[traceType];
            string collectionCommand = TraceCollector.WindowsCollectTypeMap[collectType];
            collectionCommand = collectionCommand.Replace(" ", ";").Replace("/", "");

            // Add specific commands.
            if (os == OS.Windows)
            {
                commandKVPList.Add(new("application.collect", "true"));
                commandKVPList.Add(new("application.collectStartup", "true"));
                commandKVPList.Add(new("application.collectArguments", $"\"{collectionCommand}\""));
            }
            else
            {
                if (traceType != "gc")
                {
                    throw new ArgumentException($"{nameof(ServerRunCommandBuilder)}: Currently only GCCollectOnly traces are allowed for Linux.");
                }

                else
                {
                    commandKVPList.Add(new("application.dotnetTrace", "true"));
                    commandKVPList.Add(new("application.dotnetTraceProviders", "gc-collect"));
                }
            }

            // Add name of output.
            commandKVPList.Add(new("application.options.traceOutput", tracePath));
            
            return commandKVPList;
        }

        public static List<KeyValuePair<string, string>> GenerateKeyValuePairListForGCLog(string fileNameOfLog, string gcLogDownloadPath)
        {
            List<KeyValuePair<string, string>> commandKVPList = new();
            commandKVPList.Add(new("application.options.downloadFiles", $"\"*{fileNameOfLog}.log\""));
            commandKVPList.Add(new("application.options.downloadFilesOutput", $"\"{gcLogDownloadPath}\""));
            return commandKVPList;
        }

        public static Dictionary<string, string> OverrideDictionary(Dictionary<string, string> baseDict, Dictionary<string, string> overrideDict)
        {
            Dictionary<string, string> dict = new(baseDict);
            overrideDict
                .ToList()
                .ForEach(kv => dict[kv.Key] = kv.Value);
            return dict;
        }

        public static List<KeyValuePair<string, string>> OverrideKeyValuePairList(List<KeyValuePair<string, string>> baseKVPList, List<KeyValuePair<string, string>> overrideKVPList)
        {
            List<KeyValuePair<string, string>> kvpList = new(baseKVPList);

            foreach (var item in overrideKVPList)
            {
                var existing = kvpList.Where(kv => kv.Key == item.Key).ToList();
                foreach (var kv in existing)
                {
                    if (kv.Key != null)
                    {
                        kvpList.Remove(kv);
                    }
                }

                kvpList.Add(item);
            }
            return kvpList;
        }

        internal static List<KeyValuePair<string, string>> GetCrankArgsAsList(string input)
        {
            var keyValuePairs = new List<KeyValuePair<string, string>>();
            var splitStr = input.Split(new[] { "--" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in splitStr)
            {
                var keyValue = item.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (keyValue.Length == 2)
                {
                    keyValuePairs.Add(new KeyValuePair<string, string>(keyValue[0], keyValue[1]));
                }
            }

            return keyValuePairs;
        }

        public static string ConvertKeyValueArgsListToString(List<KeyValuePair<string, string>> keyValueArgsList)
        {
            return string.Join(" ", keyValueArgsList.Select(c => $" --{c.Key} {c.Value} "));
        }
    }
}
