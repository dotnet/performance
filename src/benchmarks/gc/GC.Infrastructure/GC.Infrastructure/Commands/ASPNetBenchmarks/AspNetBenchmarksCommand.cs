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
using System.Text.RegularExpressions;

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
        public static string GetKey(string benchmark, string run) => $"{benchmark}.{run}";
        public static string? ExtractBenchmarkFromKey(string key) => key?.Split(".", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        public sealed class AspNetBenchmarkSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] AspNetBenchmarkSettings settings)
        {
            AnsiConsole.Write(new Rule("ASP.NET Benchmarks Orchestrator"));
            AnsiConsole.WriteLine();

            ConfigurationChecker.VerifyFile(settings.ConfigurationPath, nameof(AspNetBenchmarksCommand));
            ASPNetBenchmarksConfiguration configuration = ASPNetBenchmarksConfigurationParser.Parse(settings.ConfigurationPath);

            // Before running the ASP.NET benchmarks, execute a few checks:
            // Check 1. If you are connected to corpnet. These tests only run if you are connected to corp-net and therefore, preemptively, check this.
            // Check 2. The host machines reboot between 12:00 AM - 12:08 AM PST. Check if we are running during that time, and if so, delay running the infrastructure.

            // Check 1.
            if (!Directory.Exists(@"\\clrmain\tools\"))
            {
                // Not connected to corpnet if that folder is unavailable.
                AnsiConsole.MarkupLine($"[red bold] Not connected to corpnet. Ensure you are connected before proceeding with the ASP.NET Benchmarks [/]");
                return -1;
            }

            // Check 2.
            SleepUntilHostsHaveRestarted();

            RunASPNetBenchmarks(configuration);
            AnsiConsole.MarkupLine($"[bold green] Report generated at: {configuration.Output.Path} [/]");
            return 0;
        }

        private static void SleepUntilHostsHaveRestarted()
        {
            DateTime now = DateTime.UtcNow;

            // Get the Pacific Standard Time zone (considering daylight saving).
            TimeZoneInfo pstZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            DateTime pstNow = TimeZoneInfo.ConvertTimeFromUtc(now, pstZone);

            // Check if the current time is between 12:00 AM and 12:09 AM.
            DateTime start = pstNow.Date; // 12:00 AM today.
            DateTime end = start.AddMinutes(9); // 12:09 AM today.

            if (pstNow >= start && pstNow < end)
            {
                TimeSpan timeUntilEnd = end - pstNow;
                int secondsLeft = (int)timeUntilEnd.TotalSeconds;

                // If we are between 12:00 AM and 12:09 AM PST, sleep for 
                AnsiConsole.MarkupLine($"[yellow bold] ({DateTime.Now}) ASP.NET Benchmarks Sleeping for {secondsLeft} seconds since the host machines are rebooting. [/]");
                Thread.Sleep((secondsLeft) * 1000);
            }
        }

        private static ProcessExecutionDetails ExecuteBenchmarkForRun(ASPNetBenchmarksConfiguration configuration, KeyValuePair<string, Run> run, KeyValuePair<string, string> benchmarkToCommand)
        {
            // At the start of a run, if we are at a point in time where we are between the time where we deterministically know the host machines need to restart,
            // sleep for the remaining time until the machines are back up.
            SleepUntilHostsHaveRestarted();

            OS os = !benchmarkToCommand.Key.Contains("Win") ? OS.Linux : OS.Windows;
            (string, string) commandLine = ASPNetBenchmarksCommandBuilder.Build(configuration, run, benchmarkToCommand, os);

            string outputPath = Path.Combine(configuration.Output!.Path, run.Key);
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
            string logfileOutput = Path.Combine(outputPath, $"{GetKey(benchmarkToCommand.Key, run.Key)}.log");
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

                AnsiConsole.MarkupLine($"[green bold] ({DateTime.Now}) Running ASP.NET Benchmark for Configuration {configuration.Name} {run.Key} {benchmarkToCommand.Key} [/]");

                crankProcess.OutputDataReceived += (s, d) =>
                {
                    output.AppendLine(d?.Data);
                };
                crankProcess.ErrorDataReceived += (s, d) =>
                {
                    error.AppendLine(d?.Data);
                };

                crankProcess.Start();
                crankProcess.BeginOutputReadLine();
                crankProcess.BeginErrorReadLine();

                bool exited = crankProcess.WaitForExit((int)configuration.Environment!.default_max_seconds * 1000);

                // If the process still hasn't exited, it has timed out from the crank side of things and we'll need to rerun this benchmark.
                if (!crankProcess.HasExited)
                {
                    AnsiConsole.MarkupLine($"[red bold] ASP.NET Benchmark timed out for: {configuration.Name} {run.Key} {benchmarkToCommand.Key} - skipping the results but writing stdout and stderror to {logfileOutput} [/]");
                    File.WriteAllText(logfileOutput, "Output: \n" + output.ToString() + "\n Errors: \n" + error.ToString());
                    return new ProcessExecutionDetails(key: GetKey(benchmarkToCommand.Key, run.Key),
                                                       commandlineArgs: commandLine.Item1 + " " + commandLine.Item2,
                                                       environmentVariables: new(),
                                                       standardError: "[Time Out]: " + error.ToString(),
                                                       standardOut: output.ToString(),
                                                       exitCode: exitCode);
                }

                exitCode = crankProcess.ExitCode;
            }

            string outputFile = Path.Combine(configuration.Output.Path, run.Key, $"{benchmarkToCommand.Key}_{run.Key}.json");
            string outputDetails = output.ToString();

            if (File.Exists(outputFile))
            {
                string[] outputLines = File.ReadAllLines(outputFile);

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
            return new ProcessExecutionDetails(key: GetKey(benchmarkToCommand.Key, run.Key),
                                               commandlineArgs: commandLine.Item1 + " " + commandLine.Item2,
                                               environmentVariables: new(),
                                               standardError: error.ToString(),
                                               standardOut: output.ToString(),
                                               exitCode: exitCode);
        }

        public static AspNetBenchmarkResults RunASPNetBenchmarks(ASPNetBenchmarksConfiguration configuration)
        {
            List<(string run, string benchmark, string reason)> retryMessages = new();
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

                string benchmarkName     = line[0];
                string benchmarkCommands = line[1];

                benchmarkNameToCommand[benchmarkName] = benchmarkCommands; 
            }

            List<KeyValuePair<string, string>> benchmarkToNameCommandAsKvpList = new();
            bool noBenchmarkFilters =
                (configuration.benchmark_settings.benchmarkFilters == null || configuration.benchmark_settings.benchmarkFilters.Count == 0);

            // If the user has specified benchmark filters, retrieve them in that order.
            if (!noBenchmarkFilters)
            {
                foreach (var filter in configuration.benchmark_settings.benchmarkFilters!)
                {
                    foreach (var kvp in benchmarkNameToCommand)
                    {
                        // Check if we simply end with a "*", if so, match.
                        if (filter.EndsWith("*") && kvp.Key.StartsWith(filter.Replace("*", "")))
                        {
                            benchmarkToNameCommandAsKvpList.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
                        }

                        // Regular Regex check.
                        else if (Regex.IsMatch(kvp.Key, $"^{filter}$"))
                        {
                            benchmarkToNameCommandAsKvpList.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
                        }
                    }
                }

                if (benchmarkToNameCommandAsKvpList.Count == 0)
                {
                    throw new ArgumentException($"{nameof(AspNetBenchmarksCommand)}: No benchmark filters found. Please ensure you have added the wildcard character to do the regex matching. Benchmark Filter: {configuration.benchmark_settings.benchmarkFilters}");
                }
            }

            // Else, add all the benchmarks.
            else
            {
                foreach (var kvp in benchmarkNameToCommand)
                {
                    benchmarkToNameCommandAsKvpList.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
                }
            }

            // For each benchmark, iterate over all specified runs.
            foreach (var c in benchmarkToNameCommandAsKvpList)
            {
                foreach (var run in configuration.Runs!)
                {
                    const string NON_RESPONSIVE = @"for 'application' is invalid or not responsive: ""No such host is known";
                    const string TIME_OUT = "[Time Out]";
                    ProcessExecutionDetails result = ExecuteBenchmarkForRun(configuration, run, c);
                    string key = GetKey(c.Key, run.Key);

                    bool timeout = result.StandardError.Contains(TIME_OUT);
                    bool nonResponsive = result.StandardOut.Contains(NON_RESPONSIVE) || result.StandardError.Contains(NON_RESPONSIVE);
                    bool timeoutOrNonResponsive = timeout || nonResponsive;

                    // Wait 2 minutes and then retry if the run timed out or the host was non-responsive (post corp-net connection and check).
                    if (result.HasFailed && timeoutOrNonResponsive)
                    {
                        string retryReason = timeout ? "the run timed out" : "the server was non-responsive";
                        string retryDetails = $"{run.Key} for {c.Key} failed as {retryReason}. Sleeping for 2 minutes and retrying";
                        AnsiConsole.MarkupLine($"[red bold] {Markup.Escape(retryDetails)} [/]");
                        retryMessages.Add((run.Key, c.Key, retryReason));
                        Thread.Sleep(60 * 2 * 1000);
                        result = ExecuteBenchmarkForRun(configuration, run, c);
                        executionDetails[key] = result;
                    }

                    else
                    {
                        executionDetails[key] = result;
                    }
                }
            }

            Dictionary<string, List<MetricResult>> results = AspNetBenchmarksAnalyzeCommand.ExecuteAnalysis(configuration, benchmarkNameToCommand, executionDetails, retryMessages);
            return new AspNetBenchmarkResults(executionDetails, results);
        }
    }
}
