using GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using GC.Infrastructure.Core.TraceCollection;
using System.Text;

namespace GC.Infrastructure.Core.CommandBuilders
{
    public static class ASPNetBenchmarksCommandBuilder 
    {
        public static (string, string) Build(ASPNetBenchmarksConfiguration configuration, KeyValuePair<string, Run> run, KeyValuePair<string, string> benchmarkNameToCommand, OS os)
        {
            string processName = "crank";
            StringBuilder commandStringBuilder = new();

            // Load the base configuration.
            commandStringBuilder.Append(benchmarkNameToCommand.Value);

            // Environment Variables.
            // Add the environment variables from the configuration.
            Dictionary<string, string> environmentVariables = new();
            foreach (var env in configuration.Environment!.environment_variables)
            {
                environmentVariables[env.Key] = env.Value;
            }

            // Add overrides, if available.
            if (run.Value.environment_variables != null)
            {
                foreach (var env in run.Value.environment_variables)
                {
                    environmentVariables[env.Key] = env.Value;
                }
            }

            foreach (var env in environmentVariables)
            {
                string variable = env.Value;

                // Check if the log file is specified, also add the fact that we want to retrieve the log file back.
                // This log file should be named in concordance with the name of the run and the benchmark.
                if (string.CompareOrdinal(env.Key, "DOTNET_GCLogFile") == 0)
                {
                    string fileNameOfLog = Path.GetFileName(env.Value);
                    commandStringBuilder.Append( $" --application.options.downloadFiles \"*{fileNameOfLog}.log\" " );
                    commandStringBuilder.Append( $" --application.options.downloadFilesOutput \"{Path.Combine(configuration.Output!.Path, run.Key, $"{benchmarkNameToCommand.Key}_GCLog")}\" " );
                }

                commandStringBuilder.Append($" --application.environmentVariables {env.Key}={variable} ");
            }

            // Trace Collection. 
            // If the TraceConfiguration Key is specified in the yaml and 
            if (configuration.TraceConfigurations != null && !string.Equals(configuration.TraceConfigurations.Type, "none", StringComparison.OrdinalIgnoreCase))
            {
                CollectType collectType  = TraceCollector.StringToCollectTypeMap[configuration.TraceConfigurations.Type];
                string collectionCommand = TraceCollector.WindowsCollectTypeMap[collectType];
                collectionCommand        = collectionCommand.Replace(" ", ";").Replace("/", "");

                string traceFileSuffix = ".etl.zip";
                // Add specific commands.
                if (os == OS.Windows)
                {
                    commandStringBuilder.Append(" --application.collect true ");
                    commandStringBuilder.Append(" --application.collectStartup true ");
                    commandStringBuilder.Append($" --application.collectArguments \"{collectionCommand}\" ");
                }

                else
                {
                    if (configuration.TraceConfigurations.Type != "gc")
                    {
                        throw new ArgumentException($"{nameof(ASPNetBenchmarksCommandBuilder)}: Currently only GCCollectOnly traces are allowed for Linux.");
                    }

                    else
                    {
                        traceFileSuffix = ".nettrace";
                        commandStringBuilder.Append(" --application.dotnetTrace true ");
                        commandStringBuilder.Append(" --application.dotnetTraceProviders gc-collect ");
                    }
                }

                // Add name of output.
                commandStringBuilder.Append($" --application.options.traceOutput {Path.Combine(configuration.Output.Path, run.Key, (benchmarkNameToCommand.Key + "." + collectType)) + traceFileSuffix}");
            }

            string frameworkVersion = configuration.Environment.framework_version;
            // Override the framework version if it's specified at the level of the run.
            if (!string.IsNullOrEmpty(run.Value.framework_version))
            {
                frameworkVersion = run.Value.framework_version;
            }
            commandStringBuilder.Append($" --application.framework {frameworkVersion} ");

            string artifactsToUpload = run.Value.corerun!;

            // If the corerun specified is a directory, upload the entire directory.
            // Else, we upload just the file.
            if (Directory.Exists(run.Value.corerun!))
            {
                artifactsToUpload = Path.Combine(artifactsToUpload, "*.*");
            }
            commandStringBuilder.Append($" --application.options.outputFiles {artifactsToUpload} ");

            // Get the logs.
            commandStringBuilder.Append(" --application.options.downloadOutput true ");
            commandStringBuilder.Append($" --application.options.downloadOutputOutput {Path.Combine(configuration.Output.Path, run.Key, $"{benchmarkNameToCommand.Key}_{run.Key}.output.log")} ");

            commandStringBuilder.Append(" --application.options.downloadBuildLog true ");
            commandStringBuilder.Append($" --application.options.downloadBuildLogOutput {Path.Combine(configuration.Output.Path, run.Key, $"{benchmarkNameToCommand.Key}_{run.Key}.build.log")} ");

            commandStringBuilder.Append($" --json {Path.Combine(configuration.Output.Path, run.Key, $"{benchmarkNameToCommand.Key}_{run.Key}.json")}");

            // Add the extra metrics by including the configuration.
            commandStringBuilder.Append($" --config {Path.Combine("Commands", "RunCommand", "BaseSuite", "PercentileBasedMetricsConfiguration.yml")} ");

            // Add any additional arguments specified.
            if (!string.IsNullOrEmpty(configuration.benchmark_settings.additional_arguments))
            {
                commandStringBuilder.Append($" {configuration.benchmark_settings.additional_arguments} ");
            }

            string commandString = commandStringBuilder.ToString();

            // Apply overrides.
            if (!string.IsNullOrEmpty(configuration.benchmark_settings.override_arguments))
            {
                List<KeyValuePair<string, string>> overrideCommands = GetCrankArgsAsList(configuration.benchmark_settings.override_arguments);
                if (overrideCommands.Count > 0) 
                {
                    // Take the current commands and first replace all the keys that match the override commands.
                    // Subsequently, add the new overrides and then convert the key-value pair list back to a string.
                    List<KeyValuePair<string, string>> currentCommands = GetCrankArgsAsList(commandString);
                    foreach (var item in overrideCommands)
                    {
                        var existing = currentCommands.Where(kv => kv.Key == item.Key).ToList();
                        foreach (var kv in existing)
                        {
                            if (kv.Key != null)
                            {
                                currentCommands.Remove(kv);
                            }
                        }

                        currentCommands.Add(item);
                    }

                    commandString = string.Join(" ", currentCommands.Select(c => $"--{c.Key} {c.Value}"));
                }
            }

            return (processName, commandString);
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
    }
}
