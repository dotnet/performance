using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.CommandBuilders;
using GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
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

            ASPNetBenchmarksConfiguration configuration = ASPNetBenchmarksConfigurationParser.Parse(settings.ConfigurationPath);

            // Before running the ASP.NET benchmarks, execute a few checks:
            // Check 1. If the ASP.NET Machines are pingable. 
            // Check 2. The host machines reboot between 12:00 AM - 12:08 AM PST. Check if we are running during that time, and if so, delay running the infrastructure.

            // Check 1.
            const string machineName = "https://asp-citrine-win";
            bool success = false;
            Ping ping = new Ping();
            try
            {
                PingReply reply = ping.Send(machineName);

                if (reply.Status == IPStatus.Success)
                {
                    success = true;
                }
            }

            catch (PingException exp)
            {
                // DO NOTHING but catch. We log this later.
            }

            if (!success)
            {
                AnsiConsole.MarkupLine($"[red bold]Cannot ping the ASP.NET Machines. Ensure you are connected to corpnet or check if the machines are down before proceeding to run with the ASP.NET Benchmarks [/]");
                return -1;
            }

            // Check 2.
            // We don't care about the output from the following method here since this is at a point before the tests have even started to run.
            bool _ = TrySleepUntilHostsHaveRestarted();

            RunASPNetBenchmarks(configuration);
            AnsiConsole.MarkupLine($"[bold green] Report generated at: {configuration.Output!.Path} [/]");
            return 0;
        }

        private static bool TrySleepUntilHostsHaveRestarted()
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

                // If we are between 12:00 AM and 12:09 AM PST, sleep for the seconds left.
                AnsiConsole.MarkupLine($"[yellow bold] ({DateTime.Now}) ASP.NET Benchmarks Sleeping for {secondsLeft} seconds since the host machines are rebooting. [/]");
                Thread.Sleep((secondsLeft) * 1000);
                return true;
            }

            return false;
        }


        public static AspNetBenchmarkResults RunASPNetBenchmarks(ASPNetBenchmarksConfiguration configuration)
        {
            List<(string run, string benchmark, string reason)> retryMessages = new();
            Dictionary<string, ProcessExecutionDetails> executionDetails = new();
            Core.Utilities.TryCreateDirectory(configuration.Output!.Path);

            // Parse the CSV file for the information.
            string[] lines = File.ReadAllLines(configuration.benchmark_settings!.benchmark_file!);
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

            // Overwrite the dictionary with the most up to date results of what will be run. 
            benchmarkNameToCommand = benchmarkToNameCommandAsKvpList.ToDictionary(pair =>  pair.Key, pair => pair.Value);

            // For each benchmark, iterate over all specified runs.
            foreach (var benchmarkToCommand in benchmarkToNameCommandAsKvpList)
            {
                ExecuteBenchmarkForRuns(configuration, benchmarkToCommand, executionDetails, retryMessages);
            }

            Dictionary<string, List<MetricResult>> results = AspNetBenchmarksAnalyzeCommand.ExecuteAnalysis(configuration, benchmarkNameToCommand, executionDetails, retryMessages);
            return new AspNetBenchmarkResults(executionDetails, results);
        }

        internal static void ExecuteBenchmarkForRuns(ASPNetBenchmarksConfiguration configuration, 
                                                   KeyValuePair<string, string> benchmarkToCommand, 
                                                   Dictionary<string, ProcessExecutionDetails> executionDetails, 
                                                   List<(string, string, string)> retryMessages)
        {
            foreach (var run in configuration.Runs!)
            {
                const string NON_RESPONSIVE = @"for 'application' is invalid or not responsive: ""No such host is known";
                const string TIME_OUT = "[Time Out]";

                // If the machines fall asleep, re-run all the runs for the specific benchmark.
                if (TrySleepUntilHostsHaveRestarted())
                {
                    ExecuteBenchmarkForRuns(configuration, benchmarkToCommand, executionDetails, retryMessages);
                    return; // Return here to prevent infinite looping.
                }
                
                ProcessExecutionDetails result = ExecuteBenchmarkForRun(configuration, run, benchmarkToCommand);
                string key = GetKey(benchmarkToCommand.Key, run.Key);

                bool timeout = result.StandardError.Contains(TIME_OUT);
                bool nonResponsive = result.StandardOut.Contains(NON_RESPONSIVE) || result.StandardError.Contains(NON_RESPONSIVE);
                bool timeoutOrNonResponsive = timeout || nonResponsive;

                // Wait 2 minutes and then retry if the run timed out or the host was non-responsive (post corp-net connection and check).
                if (result.HasFailed && timeoutOrNonResponsive)
                {
                    string retryReason = timeout ? "the run timed out" : "the server was non-responsive";
                    string retryDetails = $"{run.Key} for {benchmarkToCommand.Key} failed as {retryReason}. Sleeping for 2 minutes and retrying";
                    AnsiConsole.MarkupLine($"[red bold] {Markup.Escape(retryDetails)} [/]");
                    retryMessages.Add((run.Key, benchmarkToCommand.Key, retryReason));
                    Thread.Sleep(60 * 2 * 1000);
                    result = ExecuteBenchmarkForRun(configuration, run, benchmarkToCommand);
                    executionDetails[key] = result;
                }

                else
                {
                    executionDetails[key] = result;
                }
            }
        }

        internal static ProcessExecutionDetails ExecuteBenchmarkForRun(ASPNetBenchmarksConfiguration configuration, KeyValuePair<string, Run> run, KeyValuePair<string, string> benchmarkToCommand)
        {
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
            // 3. Test fails because of a test errors.

            // Launch new crank process.
            int exitCode = -1;
            string logfileOutput = Path.Combine(outputPath, $"{GetKey(benchmarkToCommand.Key, run.Key)}.log");
            StringBuilder output = new();
            StringBuilder errors = new();

            using (Process crankProcess = new())
            {
                crankProcess.StartInfo.UseShellExecute = false;
                crankProcess.StartInfo.FileName = commandLine.Item1;
                crankProcess.StartInfo.Arguments = commandLine.Item2;
                crankProcess.StartInfo.RedirectStandardError = true;
                crankProcess.StartInfo.RedirectStandardOutput = true;
                crankProcess.StartInfo.CreateNoWindow = true;

                AnsiConsole.MarkupLine($"[green bold] ({DateTime.Now}) Running ASP.NET Benchmark for Configuration {configuration.Name} {run.Key} {benchmarkToCommand.Key} [/]");

                // Add the command line to the top of the output file.
                output.AppendLine($"Command: {commandLine.Item1} {commandLine.Item2}");

                crankProcess.OutputDataReceived += (s, d) =>
                {
                    output.AppendLine(d?.Data);
                };
                crankProcess.ErrorDataReceived += (s, d) =>
                {
                    errors.AppendLine(d?.Data);
                };

                crankProcess.Start();
                crankProcess.BeginOutputReadLine();
                crankProcess.BeginErrorReadLine();

                bool exited = crankProcess.WaitForExit((int)configuration.Environment!.default_max_seconds * 1000);

                // If the process still hasn't exited, it has timed out from the crank side of things and we'll need to rerun this benchmark.
                if (!crankProcess.HasExited)
                {
                    AnsiConsole.MarkupLine($"[red bold] ASP.NET Benchmark timed out for: {configuration.Name} {run.Key} {benchmarkToCommand.Key} - skipping the results but writing stdout and stderror to {logfileOutput} [/]");
                    errors.AppendLine($"Run: {configuration.Name} {run.Key} {benchmarkToCommand.Key} timed out.");
                    File.WriteAllText(logfileOutput, "Output: \n" + output.ToString() + "\n Errors: \n" + errors.ToString());
                    return new ProcessExecutionDetails(key: GetKey(benchmarkToCommand.Key, run.Key),
                                                       commandlineArgs: commandLine.Item1 + " " + commandLine.Item2,
                                                       environmentVariables: new(),
                                                       standardError: "[Time Out]: " + errors.ToString(),
                                                       standardOut: output.ToString(),
                                                       exitCode: exitCode);
                }

                exitCode = crankProcess.ExitCode;
            }

            string outputJson = Path.Combine(configuration.Output.Path, run.Key, $"{benchmarkToCommand.Key}_{run.Key}.json");
            string outputDetails = output.ToString();

            if (File.Exists(outputJson))
            {
                string[] outputLines = File.ReadAllLines(outputJson);

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
                // For the case where the output file doesn't exist implies that was an issue connecting to the asp.net machines or errors number 1.
                // This case also applies for incorrect crank arguments or errors number 2.
                // Move the standard out to the standard errors as the process failed.
                errors.AppendLine(outputDetails);
            }

            if (exitCode != 0)
            {
                string[] outputLines = outputDetails.Split("\n");
                foreach (var o in outputLines)
                {
                    // Crank provides the standard errors from the test itself by this mechanism.
                    // Error #3: Issues with test run.
                    if (o.StartsWith("[STDERR]"))
                    {
                        errors.AppendLine(o.Replace("[STDERR]", ""));
                    }

                    // Highlight case where: Configuration 'https://github.com/aspnet/Benchmarks/blob/main/scenarios/aspnet.profiles.yml?raw=true' could not be loaded.
                    else if (o.Contains("Configuration '") && o.Contains("could not be loaded"))
                    {
                        errors.AppendLine(o);
                    }
                }

                AnsiConsole.Markup($"[red bold] Failed with the following errors:\n {Markup.Escape(errors.ToString())}. Check the log file for more information: {logfileOutput} \n[/]");
            }

            // Check to see if we got back all the files regardless of the exit code.
            CheckForMissingOutputs(configuration, run.Key, benchmarkToCommand.Key);

            File.WriteAllText(logfileOutput, "Output: \n" + outputDetails + "\n Errors: \n" + errors.ToString());
            return new ProcessExecutionDetails(key: GetKey(benchmarkToCommand.Key, run.Key),
                                               commandlineArgs: commandLine.Item1 + " " + commandLine.Item2,
                                               environmentVariables: new(),
                                               standardError: errors.ToString(),
                                               standardOut: output.ToString(),
                                               exitCode: exitCode);
        }

        internal static void CheckForMissingOutputs(ASPNetBenchmarksConfiguration configuration, string runName, string benchmarkName)
        {
            HashSet<string> missingOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string basePath = Path.Combine(configuration.Output!.Path, runName);

            // Files to expect always from crank:
            // 1. Build Output: BenchmarkName_RunName.build.log
            string buildOutput = Path.Combine(basePath, $"{benchmarkName}_{runName}.build.log");
            if (!File.Exists(buildOutput)) 
            {
                missingOutputs.Add("Build Output");
            }

            // 2. Application Output: BenchmarkName_RunName.output.log
            string applicationOutput = Path.Combine(basePath, $"{benchmarkName}_{runName}.output.log");
            if (!File.Exists(applicationOutput)) 
            {
                missingOutputs.Add("Application Output");
            }

            // 3. Json Output: BenchmarkName_RunName.json
            string outputJson = Path.Combine(basePath, $"{benchmarkName}_{runName}.json");
            if (!File.Exists(outputJson)) 
            {
                missingOutputs.Add("Output Json");
            }

            // Optionally Requested Files:
            // 1. Trace: BenchmarkName.<Type>.etl.zip or BenchmarkName.<Type>.nettrace
            if (configuration.TraceConfigurations?.Type != "none")
            {
                string etlFileName      = Path.Combine(basePath, $"{benchmarkName}.{configuration.TraceConfigurations!.Type}.etl.zip");
                string nettraceFileName = Path.Combine(basePath, $"{benchmarkName}.{configuration.TraceConfigurations!.Type}.nettrace");
                if (!File.Exists(etlFileName) && !File.Exists(nettraceFileName))
                {
                    missingOutputs.Add("Traces");
                }
            }

            // 2. GCLog: BenchmarkName_GCLog/<LogName>.log
            if (configuration.Environment!.environment_variables!.ContainsKey("DOTNET_GCLog")                       || 
                configuration.Environment!.environment_variables!.ContainsKey("COMPlus_GCLog")                      || 
                configuration.Runs!.Any(r => r.Value.environment_variables?.ContainsKey("DOTNET_GCLog") ?? false)   || 
                configuration.Runs!.Any(r => r.Value.environment_variables?.ContainsKey("COMPlus_GCLog") ?? false) )
            {
                string basePathForGCLog = Path.Combine(configuration.Output.Path, runName, $"{benchmarkName}_GCLog");

                // If the directory is entirely missing.
                if (!Directory.Exists(basePathForGCLog))
                {
                    missingOutputs.Add("GCLog");
                }

                else // If the directory exists but somehow we didn't get back the gclog file.
                {
                    IEnumerable<string> gcLogFiles = Directory.EnumerateFiles(basePathForGCLog, "*.log");
                    if (gcLogFiles.Count() == 0)
                    {
                        missingOutputs.Add("GCLog");
                    }
                }
            }

            if (missingOutputs.Any())
            {
                AnsiConsole.Markup($"[yellow bold] Missing the following files from the run: \n\t-{string.Join("\n\t-", missingOutputs)} \n[/]");
            }
        }
    }
}
