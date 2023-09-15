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
            AnsiConsole.Write(new Rule("ASP.NET Benchmarks Orchestrator"));
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
            Dictionary<string, string> benchmarkNameToCommand = new(StringComparer.OrdinalIgnoreCase);

            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                if (lineIdx == 0)
                {
                    continue;
                }

                string[] line = lines[lineIdx].Split(',', StringSplitOptions.TrimEntries);
                Debug.Assert(line.Length == 2);
                benchmarkNameToCommand[line[0]] = line[1];
            }

            // For each benchmark, iterate over all specified runs.
            foreach (var c in benchmarkNameToCommand)
            {
                foreach (var run in configuration.Runs)
                {
                    OS os = !c.Key.Contains("Win") ? OS.Linux : OS.Windows; 
                    (string, string) commandLine = ASPNetBenchmarksCommandBuilder.Build(configuration, run, c, os);

                    string outputPath = Path.Combine(configuration.Output.Path, run.Key);
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    // There are 3 main ASP.NET errors:
                    // 1. The server is unavailable - this could be because you aren't connected to CorpNet or the machine is down.
                    // 2. The crank commands are incorrect.
                    // 3. Test fails because of a test error.

                    // Launch new crank process.
                    int exitCode = -1;
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

                        AnsiConsole.MarkupLine($"[green bold] ({DateTime.Now}) Running ASP.NET Benchmark for Configuration {configuration.Name} {run.Key} {c.Key} [/]");

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

                        // If the process still hasn't exited, it has timed out from the crank side of things and we'll need to rerun this benchmark.
                        if (!crankProcess.HasExited)
                        {
                            AnsiConsole.MarkupLine($"[red bold] ASP.NET Benchmark timed out for: {configuration.Name} {run.Key} {c.Key} [/]");
                            continue;
                        }

                        exitCode = crankProcess.ExitCode;
                    }

                    string outputFile = Path.Combine(configuration.Output.Path, run.Key, $"{c.Key}_{run.Key}.json");
                    string outputDetails = output.ToString();

                    if (File.Exists(outputFile))
                    {
                        string[] outputLines =  File.ReadAllLines(outputFile);

                        // In a quick and dirty way, check the returnCode from the file that'll tell us if the test failed.
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

                    else
                    {
                        // For the case where the output file doesn't exist implies that was an issue connecting to the asp.net machines or error number 1.
                        // This case also applies for incorrect crank arguments or error number 2.
                        // Move the standard out to the standard error as the process failed.
                        error.AppendLine(outputDetails);
                    }

                    string logfileOutput = Path.Combine(outputPath, $"{GetKey(c.Key, run.Key)}.log");
                    if (exitCode != 0)
                    {
                        string[] outputLines = outputDetails.Split("\n");
                        foreach (var o in outputLines)
                        {
                            // Crank provides the standard error from the test itself by this mechanism.
                            // Error #3: Issues with test run.
                            if (o.StartsWith("[STDERR]"))
                            {
                                error.AppendLine(o.Replace("[STDERR]", ""));
                            }
                        }

                        AnsiConsole.Markup($"[red bold] Failed with the following errors:\n {Markup.Escape(error.ToString())} Check the log file for more information: {logfileOutput} \n[/]");
                    }

                    File.WriteAllText(logfileOutput, "Output: \n" + outputDetails + "\n Errors: \n" + error.ToString());
                    executionDetails[GetKey(c.Key, run.Key)] = new ProcessExecutionDetails(key: GetKey(c.Key, run.Key),
                                                                                          commandlineArgs: commandLine.Item1 + " " + commandLine.Item2,
                                                                                          environmentVariables: new(),
                                                                                          standardError: error.ToString(),
                                                                                          standardOut: output.ToString(),
                                                                                          exitCode: exitCode);
                }
            }

            Dictionary<string, List<MetricResult>> results = AspNetBenchmarksAnalyzeCommand.ExecuteAnalysis(configuration, benchmarkNameToCommand, executionDetails);
            return new AspNetBenchmarkResults(executionDetails, results);
        }
    }
}
