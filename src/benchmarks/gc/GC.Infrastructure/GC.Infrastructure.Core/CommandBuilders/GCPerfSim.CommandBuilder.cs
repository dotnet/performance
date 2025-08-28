using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.TraceCollection;
using System.Text;

namespace GC.Infrastructure.Core.CommandBuilders
{
    public static class GCPerfSimCommandBuilder
    {
        // Example output:
        // corerun GCPerfSim.dll -tc 256 -tagb 1000 -tlgb 0.0 -lohar 0 -sohsi 0 -lohsi 0 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType reference -testKind time
        public static (string, string) BuildForLocal(GCPerfSimConfiguration configuration, KeyValuePair<string, Run> run, CoreRunInfo corerunOverride)
        {
            string commandArgs = string.Empty;
            string processName = corerunOverride.Path;

            Dictionary<string, string> parameters = new();
            foreach (var p in configuration.gcperfsim_configurations.Parameters)
            {
                parameters[p.Key] = p.Value;
            }

            // Add overrides, if available.
            if (run.Value?.override_parameters != null)
            {
                foreach (var p in run.Value.override_parameters)
                {
                    parameters[p.Key] = p.Value;
                }
            }

            // Construct the command line with the appropriate parameters.
            StringBuilder sb = new();
            foreach (var p in parameters)
            {
                sb.Append($" -{p.Key} {p.Value}");
            }

            commandArgs = $"{configuration.gcperfsim_configurations.gcperfsim_path} {sb}";

            return (processName, commandArgs);
        }

        public static (string, string) BuildForServer(GCPerfSimConfiguration configuration, KeyValuePair<string, Run> run, int iterationIdx, KeyValuePair<string, CoreRunInfo> corerunOverride, string serverName, OS os)
        {
            string processName = "crank";
            StringBuilder commandStringBuilder = new();

            List<KeyValuePair<string, string>> keyValueArgsList = new();

            // Add the configuration and the scenario to be run.
            string pathOfAssembly = Directory.GetParent(System.Reflection.Assembly.GetAssembly(typeof(GCPerfSimCommandBuilder)).Location).FullName;
            string configPath = Path.Combine(pathOfAssembly, "Commands", "RunCommand", "BaseSuite", "CrankConfiguration.yaml");

            keyValueArgsList.AddRange(
                ServerRunCommandBuilder.GenerateKeyValuePairListForConfig(configPath));
            keyValueArgsList.AddRange(
                ServerRunCommandBuilder.GenerateKeyValuePairListForScenario("gcperfsim"));

            // Environment Variables.
            // Add the environment variables from the configuration.
            Dictionary<string, string> environmentVariables = ServerRunCommandBuilder.OverrideDictionary(
                configuration.Environment.environment_variables, corerunOverride.Value.environment_variables);
            environmentVariables = ServerRunCommandBuilder.OverrideDictionary(
                environmentVariables, run.Value.environment_variables!);
            keyValueArgsList.AddRange(
                ServerRunCommandBuilder.GenerateKeyValuePairListForEnvironmentVariables(environmentVariables));

            // GCPerfSim Configurations.
            Dictionary<string, string> parameters = ServerRunCommandBuilder.OverrideDictionary(
                configuration.gcperfsim_configurations!.Parameters, run.Value.override_parameters);

            keyValueArgsList.AddRange(
                ServerRunCommandBuilder.GenerateKeyValuePairListForParameters(parameters));

            // Trace Collection. 
            // If the TraceConfiguration Key is specified in the yaml and 
            if (configuration.TraceConfigurations != null && !string.Equals(configuration.TraceConfigurations.Type, "none", StringComparison.OrdinalIgnoreCase))
            {
                CollectType collectType = TraceCollector.StringToCollectTypeMap[configuration.TraceConfigurations.Type];
                string extension = os == OS.Windows ? ".etl.zip" : ".nettrace";
                string tracePath = Path.Combine(configuration.Output!.Path, run.Key, run.Key + "." + corerunOverride.Key + "." + iterationIdx + "." + collectType + extension);

                keyValueArgsList.AddRange(
                    ServerRunCommandBuilder.GenerateKeyValuePairListForTrace(configuration.TraceConfigurations.Type, tracePath, os));
                keyValueArgsList.AddRange(
                    ServerRunCommandBuilder.GenerateKeyValuePairListForFramework("net8.0"));
            }

            // Upload corerun or Core_Root
            keyValueArgsList.AddRange(
                ServerRunCommandBuilder.GenerateKeyValuePairListForUploadFiles(corerunOverride.Value.Path));

            // Set profile
            keyValueArgsList.AddRange(
                    ServerRunCommandBuilder.GenerateKeyValuePairListForProfile(serverName));

            // Add key-Value arguments to commandStringBuilder
            commandStringBuilder.Append(ServerRunCommandBuilder.ConvertKeyValueArgsListToString(keyValueArgsList));
            return (processName, commandStringBuilder.ToString());
        }
    }
}
