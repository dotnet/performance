using GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using GC.Infrastructure.Core.TraceCollection;
using System.Linq;
using System.Text;

namespace GC.Infrastructure.Core.CommandBuilders
{
    public static class ASPNetBenchmarksCommandBuilder
    {
        public static (string, string) Build(ASPNetBenchmarksConfiguration configuration, KeyValuePair<string, Run> run, KeyValuePair<string, string> benchmarkNameToCommand, OS os)
        {
            string processName = "crank";
            StringBuilder commandStringBuilder = new();
            commandStringBuilder.Append(benchmarkNameToCommand.Value);

            List<KeyValuePair<string, string>> keyValueArgsList = new();

            // Environment Variables.
            // Add the environment variables from the configuration.
            var environmentVariables = ServerRunCommandBuilder.OverrideDictionary(
                configuration.Environment!.environment_variables!,
                run.Value!.environment_variables!);

            keyValueArgsList.AddRange(
                ServerRunCommandBuilder.GenerateKeyValuePairListForEnvironmentVariables(environmentVariables));

            // Check if the log file is specified, also add the fact that we want to retrieve the log file back.
            // This log file should be named in concordance with the name of the run and the benchmark.
            string? fileNameOfLog = environmentVariables!.GetValueOrDefault("DOTNET_GCLogFile", null);
            if (!String.IsNullOrEmpty(fileNameOfLog))
            {
                string gcLogDownloadPath = Path.Combine(configuration.Output!.Path, run.Key, $"{benchmarkNameToCommand.Key}_GCLog");
                keyValueArgsList.AddRange(
                    ServerRunCommandBuilder.GenerateKeyValuePairListForGCLog(fileNameOfLog, gcLogDownloadPath));
            }

            // Trace Collection. 
            // If the TraceConfiguration Key is specified in the yaml and 
            if (configuration.TraceConfigurations != null && !string.Equals(configuration.TraceConfigurations.Type, "none", StringComparison.OrdinalIgnoreCase))
            {
                CollectType collectType = TraceCollector.StringToCollectTypeMap[configuration.TraceConfigurations.Type];
                string traceFileSuffix = os == OS.Windows? ".etl.zip": ".nettrace";
                string tracePath = Path.Combine(configuration.Output.Path, run.Key, (benchmarkNameToCommand.Key + "." + collectType)) + traceFileSuffix;

                keyValueArgsList.AddRange(
                    ServerRunCommandBuilder.GenerateKeyValuePairListForTrace(configuration.TraceConfigurations.Type, tracePath, os));
            }

            // Override the framework version if it's specified at the level of the run.
            string frameworkVersion = string.IsNullOrEmpty(run.Value.framework_version) ? 
                configuration.Environment.framework_version : run.Value.framework_version;
            keyValueArgsList.AddRange(
                    ServerRunCommandBuilder.GenerateKeyValuePairListForFramework(frameworkVersion));

            // If the corerun specified is a directory, upload the entire directory.
            // Else, we upload just the file.
            keyValueArgsList.AddRange(
                    ServerRunCommandBuilder.GenerateKeyValuePairListForUploadFiles(run.Value.corerun));

            // Get the logs.
            string logDownloadPathWithoutExtension = Path.Combine(
                configuration.Output.Path, run.Key, $"{benchmarkNameToCommand.Key}_{run.Key}");
            keyValueArgsList.AddRange(
                    ServerRunCommandBuilder.GenerateKeyValuePairListForGettingLogs(logDownloadPathWithoutExtension));

            // Add the extra metrics by including the configuration.
            string configPath = Path.Combine("Commands", "RunCommand", "BaseSuite", "PercentileBasedMetricsConfiguration.yml");
            keyValueArgsList.AddRange(
                    ServerRunCommandBuilder.GenerateKeyValuePairListForConfig(configPath));

            // Apply overrides.
            if (!string.IsNullOrEmpty(configuration.benchmark_settings.override_arguments))
            {
                List<KeyValuePair<string, string>> overrideCommands = ServerRunCommandBuilder.GetCrankArgsAsList(
                    configuration.benchmark_settings.override_arguments);

                keyValueArgsList = ServerRunCommandBuilder.OverrideKeyValuePairList(
                    keyValueArgsList, overrideCommands);
            }

            // Add key-Value arguments to commandStringBuilder
            commandStringBuilder.Append(ServerRunCommandBuilder.ConvertKeyValueArgsListToString(keyValueArgsList));

            // Add any additional arguments specified.
            if (!string.IsNullOrEmpty(configuration.benchmark_settings.additional_arguments))
            {
                commandStringBuilder.Append($" {configuration.benchmark_settings.additional_arguments} ");
            }

            string commandString = commandStringBuilder.ToString();
            return (processName, commandString);
        }
    }
}
