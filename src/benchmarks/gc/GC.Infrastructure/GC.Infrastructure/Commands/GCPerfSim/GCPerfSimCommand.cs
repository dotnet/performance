using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.CommandBuilders;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Presentation.GCPerfSim;
using GC.Infrastructure.Core.TraceCollection;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GC.Infrastructure.Commands.GCPerfSim
{
    public sealed class GCPerfSimResults
    {
        public GCPerfSimResults(IReadOnlyDictionary<string, ProcessExecutionDetails> executionDetails, IReadOnlyList<ComparisonResult> analysisResults)
        {
            ExecutionDetails = executionDetails;
            AnalysisResults = analysisResults;
        }

        public IReadOnlyDictionary<string, ProcessExecutionDetails> ExecutionDetails { get; }
        public IReadOnlyList<ComparisonResult> AnalysisResults { get; }
    }

    internal sealed class GCPerfSimCommand : Command<GCPerfSimCommand.GCPerfSimSettings>
    {
        public sealed class GCPerfSimSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public string? ConfigurationPath { get; init; }

            [Description("Crank Server to target.")]
            [CommandOption("-s|--server")]
            public string? Server { get; init; }
        }

        internal sealed class RunInfo
        {
            public RunInfo(KeyValuePair<string, Run> runDetails, KeyValuePair<string, CoreRunInfo> corerunDetails)
            {
                RunDetails = runDetails;
                CorerunDetails = corerunDetails;
            }

            public KeyValuePair<string, Run> RunDetails { get; set; }
            public KeyValuePair<string, CoreRunInfo> CorerunDetails { get; set; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] GCPerfSimSettings settings)
        {
            Stopwatch sw = new();
            sw.Start();

            AnsiConsole.Write(new Rule("GCPerfSim Orchestrator"));
            AnsiConsole.WriteLine();

            GCPerfSimConfiguration configuration = GCPerfSimConfigurationParser.Parse(settings.ConfigurationPath);
            GCPerfSimResults _ = RunGCPerfSim(configuration, settings.Server);

            sw.Stop();
            AnsiConsole.WriteLine($"Time to execute Msec: {sw.ElapsedMilliseconds}");
            return 0;
        }

        public static GCPerfSimResults RunGCPerfSim(GCPerfSimConfiguration configuration, string? server)
        {
            Core.Utilities.TryCreateDirectory(configuration.Output.Path);

            List<RunInfo> runInfos = new List<RunInfo>();

            // Add the coreruns runs here.
            foreach (var corerun in configuration.coreruns)
            {
                foreach (var run in configuration.Runs)
                {
                    runInfos.Add(new RunInfo(run, corerun));
                }
            }

            Dictionary<string, ProcessExecutionDetails> executionDetails = new();

            // Server Case.
            if (!string.IsNullOrEmpty(server))
            {
                executionDetails = ExecuteOnTheCrankServers(configuration, server, runInfos);
            }

            // Local Case.
            else
            {
                executionDetails = ExecuteLocally(configuration, runInfos);
            }

            return new GCPerfSimResults(executionDetails, GCPerfSimAnalyzeCommand.ExecuteAnalysis(configuration, executionDetails));
        }

        internal static Dictionary<string, ProcessExecutionDetails> ExecuteLocally(GCPerfSimConfiguration configuration, IReadOnlyList<RunInfo> runInfos)
        {
            Dictionary<string, ProcessExecutionDetails> executionDetails = new();

            string collectType = configuration.TraceConfigurations?.Type ?? "none";

            foreach (var runInfo in runInfos)
            {
                (string, string) processAndParameters = GCPerfSimCommandBuilder.BuildForLocal(configuration, runInfo.RunDetails, runInfo.CorerunDetails.Value);

                // Create the run path directory.
                string outputPath = Path.Combine(configuration.Output!.Path, runInfo.RunDetails.Key);
                Core.Utilities.TryCreateDirectory(outputPath);

                for (int iterationIdx = 0; iterationIdx < configuration.Environment.Iterations; iterationIdx++)
                {
                    using (Process gcperfsimProcess = new())
                    {
                        gcperfsimProcess.StartInfo.FileName = processAndParameters.Item1;
                        gcperfsimProcess.StartInfo.Arguments = processAndParameters.Item2;
                        gcperfsimProcess.StartInfo.UseShellExecute = false;
                        gcperfsimProcess.StartInfo.RedirectStandardError = true;
                        gcperfsimProcess.StartInfo.RedirectStandardOutput = true;
                        gcperfsimProcess.StartInfo.CreateNoWindow = true;

                        AnsiConsole.MarkupLine($"[green bold] ({DateTime.Now}) Running {Path.GetFileNameWithoutExtension(configuration.Name)}: {runInfo.CorerunDetails.Key} for {runInfo.RunDetails.Key} - Iteration: {iterationIdx} [/]");

                        // Environment Variables.
                        Dictionary<string, string> environmentVariables = new();

                        // Add the environment based environment variables.
                        if (configuration.Environment.environment_variables != null)
                        {
                            foreach (var environmentVar in configuration.Environment.environment_variables)
                            {
                                environmentVariables[environmentVar.Key] = environmentVar.Value;
                            }
                        }

                        // Add overrides.
                        if (runInfo.RunDetails.Value.environment_variables != null)
                        {
                            foreach (var environmentVar in runInfo.RunDetails.Value.environment_variables)
                            {
                                environmentVariables[environmentVar.Key] = environmentVar.Value;
                            }
                        }

                        // Add per corerun based environment variables.
                        if (runInfo.CorerunDetails.Value.environment_variables != null)
                        {
                            foreach (var environmentVar in runInfo.CorerunDetails.Value.environment_variables)
                            {
                                environmentVariables[environmentVar.Key] = environmentVar.Value;
                            }
                        }

                        foreach (var environVar in environmentVariables)
                        {
                            gcperfsimProcess.StartInfo.EnvironmentVariables[environVar.Key] = environVar.Value;
                        }

                        // Format: (Name of Run).(corerun / name of corerun).(IterationIdx)
                        string? output = null;
                        string? error = null;

                        string key = $"{runInfo.RunDetails.Key}.{runInfo.CorerunDetails.Key}.{iterationIdx}";
                        string traceName = $"{runInfo.RunDetails.Key}.{runInfo.CorerunDetails.Key}.{iterationIdx}";
                        using (TraceCollector traceCollector = new TraceCollector(traceName, collectType, outputPath))
                        {
                            gcperfsimProcess.Start();
                            output = gcperfsimProcess.StandardOutput.ReadToEnd();
                            error = gcperfsimProcess.StandardError.ReadToEnd();
                            gcperfsimProcess.WaitForExit((int)configuration.Environment.default_max_seconds * 1000);
                            File.WriteAllText(Path.Combine(outputPath, key + ".txt"), "Standard Out: \n" + output + "\n Standard Error: \n" + error);
                        }

                        // If a trace is requested, ensure the file exists. If not, there is was an error and alert the user.
                        if (configuration.TraceConfigurations?.Type != "none")
                        {
                            // Not checking Linux here since the local run only allows for Windows.
                            if (!File.Exists(Path.Combine(outputPath, traceName + ".etl.zip")))
                            {
                                AnsiConsole.MarkupLine($"[yellow bold] ({DateTime.Now}) The trace for the run wasn't successfully captured. Please check the log file for more details: {Markup.Escape(output)} Full run details: {Path.GetFileNameWithoutExtension(configuration.Name)}: {runInfo.CorerunDetails.Key} for {runInfo.RunDetails.Key} [/]");
                            }
                        }

                        int exitCode = gcperfsimProcess.ExitCode;
                        if (!string.IsNullOrEmpty(error))
                        {
                            AnsiConsole.MarkupLine($"[red bold] {Path.GetFileNameWithoutExtension(configuration.Name)}: {runInfo.CorerunDetails.Key} for {runInfo.RunDetails.Key} failed with: \n{Markup.Escape(error)} [/]");
                        }

                        ProcessExecutionDetails details = new(key: key,
                                                              commandlineArgs: $"{processAndParameters.Item1} {processAndParameters.Item2}",
                                                              environmentVariables: environmentVariables,
                                                              standardError: error,
                                                              standardOut: output,
                                                              exitCode: exitCode);
                        executionDetails[key] = details;
                    }
                }
            }

            return executionDetails;
        }

        internal static Dictionary<string, ProcessExecutionDetails> ExecuteOnTheCrankServers(GCPerfSimConfiguration configuration, string serverName, IReadOnlyList<RunInfo> runInfos)
        {
            Dictionary<string, ProcessExecutionDetails> executionDetails = new();

            // For each GCPerfSim run, start collecting the appropriate trace and run.
            foreach (var run in runInfos)
            {
                // Create the run path directory.
                string outputPath = Path.Combine(configuration.Output!.Path, run.RunDetails.Key);
                Core.Utilities.TryCreateDirectory(outputPath);

                for (int iterationIdx = 0; iterationIdx < configuration.Environment.Iterations; iterationIdx++)
                {
                    OS os = serverName.Contains("lin") ? OS.Linux : OS.Windows;
                    (string, string) processAndParameters = GCPerfSimCommandBuilder.BuildForServer(configuration, run.RunDetails, iterationIdx, run.CorerunDetails, serverName, os);

                    string key = $"{run.RunDetails.Key}.{run.CorerunDetails.Key}.{iterationIdx}";
                    StringBuilder output = new();
                    StringBuilder error = new();

                    int exitCode = -1;

                    using (Process crankProcess = new())
                    {
                        crankProcess.StartInfo.FileName = processAndParameters.Item1;
                        crankProcess.StartInfo.Arguments = processAndParameters.Item2;
                        crankProcess.StartInfo.UseShellExecute = false;
                        crankProcess.StartInfo.RedirectStandardOutput = true;
                        crankProcess.StartInfo.RedirectStandardError = true;

                        crankProcess.OutputDataReceived += (s, d) =>
                        {
                            output.AppendLine(d.Data);
                        };
                        crankProcess.ErrorDataReceived += (s, d) =>
                        {
                            error.AppendLine(d.Data);
                        };

                        AnsiConsole.MarkupLine($"[green bold] ({DateTime.Now}) Running {Path.GetFileNameWithoutExtension(configuration.Name)}: {run.CorerunDetails.Key} for {run.RunDetails.Key} - Iteration: {iterationIdx} [/]");
                        crankProcess.Start();
                        crankProcess.BeginOutputReadLine();
                        crankProcess.BeginErrorReadLine();
                        crankProcess.WaitForExit((int)configuration.Environment.default_max_seconds * 1000);
                        Thread.Sleep(1000);

                        if (crankProcess.HasExited)
                        {
                            exitCode = crankProcess.ExitCode;
                        }
                    }

                    if (exitCode != 0)
                    {
                        AnsiConsole.MarkupLine($"[red bold] {Path.GetFileNameWithoutExtension(configuration.Name)}: {run.CorerunDetails.Key} for {run.RunDetails.Key} - Iteration: {iterationIdx} failed with \n{Markup.Escape(error.ToString())} \n {Markup.Escape(output.ToString())} [/]");
                    }

                    ProcessExecutionDetails details = new(key: key,
                                                          commandlineArgs: $"{processAndParameters.Item1} {processAndParameters.Item2}",
                                                          environmentVariables: new(), // The environment variables are embedded in the command line for crank. 
                                                          standardError: error.ToString(),
                                                          standardOut: output.ToString(),
                                                          exitCode: exitCode);
                    executionDetails[key] = details;
                }
            }

            return executionDetails;
        }
    }
}
