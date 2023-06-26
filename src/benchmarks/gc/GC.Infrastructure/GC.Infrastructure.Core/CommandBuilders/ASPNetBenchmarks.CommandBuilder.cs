using GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using GC.Infrastructure.Core.TraceCollection;
using System.Text;

namespace GC.Infrastructure.Core.CommandBuilders
{
    public static class ASPNetBenchmarksCommandBuilder 
    {
        public static (string, string) Build(ASPNetBenchmarksConfiguration configuration, KeyValuePair<string, Run> run, KeyValuePair<string, string> baseConfiguration, OS os)
        {
            string processName = "crank";
            StringBuilder commandStringBuilder = new();

            // Load the base configuration.
            commandStringBuilder.Append(baseConfiguration.Value);

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
                commandStringBuilder.Append($" --application.options.traceOutput {Path.Combine(configuration.Output.Path, run.Key, (baseConfiguration.Key + "." + collectType)) + traceFileSuffix}");
            }

            commandStringBuilder.Append($" --application.framework net8.0 ");

            string corerunToSend = run.Value.corerun.EndsWith("\\") ? run.Value.corerun.Remove(run.Value.corerun.Length - 1) : run.Value.corerun;
            commandStringBuilder.Append($" --application.options.outputFiles {Path.Combine(Path.GetDirectoryName(corerunToSend), "*.*" )}");

            // Get the log.
            commandStringBuilder.Append(" --application.options.downloadOutput true ");
            commandStringBuilder.Append(" --application.options.downloadBuildLog true ");

            commandStringBuilder.Append($" --json {Path.Combine(configuration.Output.Path, run.Key, $"{baseConfiguration.Key}_{run.Key}.json")}");
            return (processName, commandStringBuilder.ToString());
        }
    }
}
