using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.CommandBuilders;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GC.Infrastructure.Commands.ASPNetBenchmarks
{
    public sealed class AspNetBenchmarkResults
    {
        public AspNetBenchmarkResults(Dictionary<string, ProcessExecutionDetails> executionDetails, Dictionary<string, List<MetricResult>> results)
        {
            ExecutionDetails = executionDetails;
            Results = results;
        }

        public Dictionary<string, ProcessExecutionDetails> ExecutionDetails { get; }
        public Dictionary<string, List<MetricResult>> Results { get; }
    }

    public sealed class AspNetBenchmarksCommand : Command<AspNetBenchmarksCommand.AspNetBenchmarkSettings>
    {
        public static string GetKey(string configuration, string run) => $"{configuration}.{run}";

        public sealed class AspNetBenchmarkSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public string? ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] AspNetBenchmarkSettings settings)
        {
            AnsiConsole.Write(new Rule("ASPNet Benchmarks Orchestrator"));
            AnsiConsole.WriteLine();

            ConfigurationChecker.VerifyFile(settings.ConfigurationPath, nameof(AspNetBenchmarksCommand));
            ASPNetBenchmarksConfiguration configuration = ASPNetBenchmarksConfigurationParser.Parse(settings.ConfigurationPath);
            RunASPNetBenchmarks(configuration);
            AnsiConsole.MarkupLine($"[bold green] Report generated at: {configuration.Output.Path} [/]");
            return 0;
        }

        public static AspNetBenchmarkResults RunASPNetBenchmarks(ASPNetBenchmarksConfiguration configuration)
        {
            Dictionary<string, ProcessExecutionDetails> executionDetails = new();
            Core.Utilities.TryCreateDirectory(configuration.Output.Path);

            // Parse the CSV file for the information.
            string[] lines = File.ReadAllLines(configuration.benchmark_settings.benchmark_file);
            Dictionary<string, string> configurationToCommand = new(StringComparer.OrdinalIgnoreCase);

            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                if (lineIdx == 0)
                {
                    continue;
                }

                string[] line = lines[lineIdx].Split(',', StringSplitOptions.TrimEntries);
                Debug.Assert(line.Length == 2);
                configurationToCommand[line[0]] = line[1];
            }

            foreach (var c in configurationToCommand)
            {
                foreach (var run in configuration.Runs)
                {
                    OS os = !c.Key.Contains("Win") ? OS.Linux : OS.Windows; 
                    // Build Commandline.
                    (string, string) commandLine = ASPNetBenchmarksCommandBuilder.Build(configuration, run, c, os);

                    string outputPath = Path.Combine(configuration.Output.Path, run.Key);
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    // Launch new process.
                    StringBuilder output = new();
                    StringBuilder error = new();

                    using (Process crankProcess = new())
                    {
                        crankProcess.StartInfo.UseShellExecute = false;
                        crankProcess.StartInfo.FileName = commandLine.Item1;
                        crankProcess.StartInfo.Arguments = commandLine.Item2;
                        crankProcess.StartInfo.RedirectStandardError = true;
                        crankProcess.StartInfo.RedirectStandardOutput = true;
                        crankProcess.StartInfo.CreateNoWindow = true;

                        AnsiConsole.MarkupLine($"[green bold] ({DateTime.Now}) Running ASPNetBenchmark for Configuration {configuration.Name} {run.Key} {c.Key} [/]");

                        crankProcess.OutputDataReceived += (s, d) =>
                        {
                            output.AppendLine(d.Data);
                        };
                        crankProcess.ErrorDataReceived += (s, d) =>
                        {
                            error.Append(d.Data);
                        };

                        crankProcess.Start();
                        crankProcess.BeginOutputReadLine();
                        crankProcess.BeginErrorReadLine();

                        bool exited = crankProcess.WaitForExit((int)configuration.Environment.default_max_seconds * 1000);
                    }

                    int exitCode = -1;

                    string outputFile = Path.Combine(configuration.Output.Path, run.Key, $"{c.Key}_{run.Key}.json");
                    if (File.Exists(outputFile))
                    {
                        string[] outputLines =  File.ReadAllLines(outputFile);

                        // In a quick and dirty way check the returnCode from the file.
                        foreach (var o in outputLines)
                        {
                            if (o.Contains("returnCode"))
                            {
                                string[] result = o.Split(":", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                string code = result[1].Replace(",", "");
                                exitCode = int.Parse(code);
                                break;
                            }
                        }
                    }

                    string outputDetails = output.ToString();
                    File.WriteAllText(Path.Combine(outputPath, $"{GetKey(c.Key, run.Key)}.log"), "Output: \n" + outputDetails + "\n Errors: \n" + error.ToString());

                    if (exitCode != 0)
                    {
                        StringBuilder errorLines = new();

                        errorLines.AppendLine(error.ToString());
                        string[] outputLines = outputDetails.Split("\n");
                        foreach (var o in outputLines)
                        {
                            // Crank provides the standard error from the test itself by this mechanism.
                            if (o.StartsWith("[STDERR]"))
                            {
                                errorLines.AppendLine(o);
                            }
                        }

                        AnsiConsole.Markup($"[red bold] Failed with the following errors:\n {Markup.Escape(errorLines.ToString())} [/]");
                    }

                    executionDetails[GetKey(c.Key, run.Key)] = new ProcessExecutionDetails(key: GetKey(c.Key, run.Key),
                                                                                          commandlineArgs: commandLine.Item1 + " " + commandLine.Item2,
                                                                                          environmentVariables: new(),
                                                                                          standardError: error.ToString(),
                                                                                          standardOut: output.ToString(),
                                                                                          exitCode: exitCode);
                }
            }

            Dictionary<string, List<MetricResult>> results = AspNetBenchmarksAnalyzeCommand.ExecuteAnalysis(configuration, configurationToCommand, executionDetails);
            return new AspNetBenchmarkResults(executionDetails, results);
        }
    }
}
