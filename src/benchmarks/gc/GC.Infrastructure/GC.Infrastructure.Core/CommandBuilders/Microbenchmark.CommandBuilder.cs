using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;

namespace GC.Infrastructure.Core.CommandBuilders
{
    public static class MicrobenchmarkCommandBuilder
    {
        public static (string, string) Build(MicrobenchmarkConfiguration configuration, KeyValuePair<string, Run> run, string? benchmark = null, long? invocationCountFromBaseline = null, string? overrideArtifacts = null)
        {
            string frameworkVersion = run.Value.DotnetInstaller ?? configuration.MicrobenchmarkConfigurations.DotnetInstaller;
            string filter = benchmark ?? configuration.MicrobenchmarkConfigurations.Filter;

            // Base command: Add mandatory commands.
            string command = $"run -f {frameworkVersion} --filter \"{filter}\" -c Release --noOverwrite --no-build";

            // [Optional] Add corerun.
            if (!string.IsNullOrEmpty(run.Value.corerun))
            {
                command += $" --corerun {run.Value.corerun}";
            }

            if (invocationCountFromBaseline.HasValue)
            {
                command += $" --invocationCount {invocationCountFromBaseline.Value}";

                int unrollFactor = invocationCountFromBaseline.Value % 16 == 0 ? 16 : 1;
                command += $" --unrollFactor {unrollFactor}";
            }

            // Add bdn arguments and output that must be added to the --bdn-arguments part of the command.
            if (!string.IsNullOrEmpty(configuration.MicrobenchmarkConfigurations.bdn_arguments) || !string.IsNullOrEmpty(configuration.Output.Path) || (run.Value.environment_variables != null && run.Value.environment_variables.Count != 0))
            {
                command += $" {configuration.MicrobenchmarkConfigurations.bdn_arguments}";

                // Add artifacts.
                if (!string.IsNullOrEmpty(configuration.Output.Path))
                {
                    if (string.IsNullOrEmpty(overrideArtifacts))
                    {
                        command += $" --artifacts {Path.Combine(configuration.Output.Path, run.Key)} ";
                    }

                    else
                    {
                        command += $" --artifacts {overrideArtifacts}";
                    }
                }

                // Add environment variables.
                if (run.Value.environment_variables != null && invocationCountFromBaseline != null)
                {
                    command += $" --envVars ";
                    foreach (var kvp in run.Value.environment_variables)
                    {
                        command += $"{kvp.Key}:{kvp.Value} ";
                    }
                }

                command = command.TrimEnd();
            }

            return ("dotnet", command);
        }
    }
}
