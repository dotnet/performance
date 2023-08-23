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

            // Add any additional arguments specified.
            if (!string.IsNullOrEmpty(configuration.benchmark_settings.additional_arguments))
            {
                commandStringBuilder.Append($" {configuration.benchmark_settings.additional_arguments} ");
            }

            // Add the Framework version.
            string frameworkVersion = "net8.0";
            // If the framework version at the top level is explicitly stated, use it.
            if (!string.IsNullOrEmpty(configuration.Environment.framework_version))
            {
                frameworkVersion = configuration.Environment.framework_version;

                // If the framework version is set at the run level, use it.
                if (!string.IsNullOrEmpty(run.Value.framework_version))
                {
                    frameworkVersion = run.Value.framework_version;
                }
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

            // Get the log.
            // TODO: Specify the path.
            commandStringBuilder.Append(" --application.options.downloadOutput true ");
            commandStringBuilder.Append($" --application.options.downloadOutput {Path.Combine(configuration.Output.Path, run.Key, $"{baseConfiguration.Key}_{run.Key}.output")} ");

            commandStringBuilder.Append(" --application.options.downloadBuildLog true ");
            commandStringBuilder.Append($" --application.options.downloadBuildLogOutput {Path.Combine(configuration.Output.Path, run.Key, $"{baseConfiguration.Key}_{run.Key}.buildLog")} ");


            commandStringBuilder.Append($" --json {Path.Combine(configuration.Output.Path, run.Key, $"{baseConfiguration.Key}_{run.Key}.json")}");
            return (processName, commandStringBuilder.ToString());
        }
    }
}
