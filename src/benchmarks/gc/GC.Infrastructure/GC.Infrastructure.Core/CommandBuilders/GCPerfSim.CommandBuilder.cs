using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.TraceCollection;
using System.Text;

namespace GC.Infrastructure.Core.CommandBuilders
{
    public enum OS
    {
        Windows,
        Linux
    }

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
            if (run.Value.override_parameters != null)
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

            // Add the configuration and the scenario to be run.
            string pathOfAssembly = Directory.GetParent(System.Reflection.Assembly.GetAssembly(typeof(GCPerfSimCommandBuilder)).Location).FullName;
            commandStringBuilder.Append($"--config {Path.Combine(pathOfAssembly, "Commands", "RunCommand", "BaseSuite", "CrankConfiguration.yaml")} --scenario gcperfsim");

            // Environment Variables.
            // Add the environment variables from the configuration.
            Dictionary<string, string> environmentVariables = new();
            foreach (var env in configuration.Environment.environment_variables)
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
                commandStringBuilder.Append($" --application.environmentVariables {env.Key}={env.Value} ");
            }

            // GCPerfSim Configurations.
            Dictionary<string, string> parameters = new();
            foreach (var p in configuration.gcperfsim_configurations!.Parameters)
            {
                parameters[p.Key] = p.Value;
            }

            // Add overrides, if available.
            if (run.Value.override_parameters != null)
            {
                foreach (var p in run.Value.override_parameters)
                {
                    parameters[p.Key] = p.Value;
                }
            }

            foreach (var @params in parameters)
            {
                commandStringBuilder.Append($" --application.variables.{@params.Key} {@params.Value} ");
            }

            // Trace Collection. 
            // If the TraceConfiguration Key is specified in the yaml and 
            if (configuration.TraceConfigurations != null && !string.Equals(configuration.TraceConfigurations.Type, "none", StringComparison.OrdinalIgnoreCase))
            {
                CollectType collectType = TraceCollector.StringToCollectTypeMap[configuration.TraceConfigurations.Type];
                string collectionCommand = os == OS.Windows ? TraceCollector.WindowsCollectTypeMap[collectType] : TraceCollector.LinuxCollectTypeMap[collectType];

                collectionCommand = collectionCommand.Replace(" ", ";").Replace("/", "");

                // Add specific commands.
                if (os == OS.Windows)
                {
                    commandStringBuilder.Append(" --application.collect true ");
                    commandStringBuilder.Append(" --application.collectStartup true ");
                    commandStringBuilder.Append($" --application.collectArguments {collectionCommand} ");
                }

                else
                {
                    if (!string.Equals(configuration.TraceConfigurations.Type, "gc", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException($"{nameof(GCPerfSimCommandBuilder)}: Currently only GCCollectOnly traces are allowed for Linux.");
                    }

                    commandStringBuilder.Append(" --application.dotnetTrace true ");
                    commandStringBuilder.Append(" --application.dotnetTraceProviders gc-collect ");
                }

                commandStringBuilder.Append($" --application.framework net8.0 ");

                // Add name of output.
                string extension = os == OS.Windows ? "etl.zip" : "nettrace";
                commandStringBuilder.Append($" --application.options.traceOutput {Path.Combine(configuration.Output!.Path, run.Key, run.Key + "." + corerunOverride.Key + "." + iterationIdx + "." + collectType + "." + extension)} ");
            }

            if (corerunOverride.Value.environment_variables != null)
            {
                foreach (var env in corerunOverride.Value.environment_variables)
                {
                    commandStringBuilder.Append($" --application.environmentVariables {env.Key}={env.Value} ");
                }
            }

            // If Path is a file, upload single file.
            if (File.Exists(corerunOverride.Value.Path))
            {
                commandStringBuilder.Append($" --application.options.outputFiles {corerunOverride.Value.Path}");
            }
            // If Path is a folder, upload entire folder.
            if (Directory.Exists(corerunOverride.Value.Path))
            {
                commandStringBuilder.Append($" --application.options.outputFiles {Path.Combine(corerunOverride.Value.Path, "*")} ");
            }

            commandStringBuilder.Append($" --profile {serverName} ");
            return (processName, commandStringBuilder.ToString());
        }
    }
}
