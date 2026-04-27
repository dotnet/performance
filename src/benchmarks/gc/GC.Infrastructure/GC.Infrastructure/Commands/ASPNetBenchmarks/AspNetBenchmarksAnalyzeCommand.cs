using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using GC.Infrastructure.Core.Presentation;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GC.Infrastructure.Commands.ASPNetBenchmarks
{
    public sealed class AspNetBenchmarksAnalyzeCommand : Command<AspNetBenchmarksAnalyzeCommand.AspNetBenchmarkAnalyzeSettings>
    {
        public sealed class AspNetBenchmarkAnalyzeSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public string? ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] AspNetBenchmarkAnalyzeSettings settings)
        {
            AnsiConsole.Write(new Rule("ASP.NET Benchmarks Analyzer"));
            AnsiConsole.WriteLine();

            ConfigurationChecker.VerifyFile(settings.ConfigurationPath, nameof(AspNetBenchmarksCommand));
            ASPNetBenchmarksConfiguration configuration = ASPNetBenchmarksConfigurationParser.Parse(settings.ConfigurationPath);
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

            Dictionary<string, List<MetricResult>> result = ExecuteAnalysis(configuration, configurationToCommand, new(), new());
            if (result.Count == 0)
            {
                AnsiConsole.MarkupLine($"[bold green] No report generated since there were no results to compare. [/]");
            }

            else
            {
                AnsiConsole.MarkupLine($"[bold green] Report generated at: {Path.Combine(configuration.Output.Path, "Results.md")} [/]");
            }

            return 0;
        }

        public static Dictionary<string, List<MetricResult>> ExecuteAnalysis(ASPNetBenchmarksConfiguration configuration, Dictionary<string, string> configurationToCommand, Dictionary<string, ProcessExecutionDetails> executionDetails, List<(string run, string benchmark, string reason)> retryDetails)
        {
            // Benchmark to Run to Path. 
            Dictionary<string, List<string>> benchmarkToRunToPaths = new();

            bool singleRun = configuration.Runs!.Count == 1;
            // Don't generate a report in case of a single report.
            if (singleRun)
            {
                return new();
            }

            // For each Run, grab the paths of each of the benchmarks.
            string outputPath = configuration.Output!.Path;
            foreach (var c in configuration.Runs)
            {
                string runName = c.Key;
                foreach (var benchmark in configurationToCommand)
                {
                    string benchmarkName = benchmark.Key;
                    if (!benchmarkToRunToPaths.TryGetValue(benchmarkName, out var d))
                    {
                        benchmarkToRunToPaths[benchmarkName] = d = new();
                    }

                    d.Add(Path.Combine(outputPath, runName, $"{benchmarkName}_{runName}.json"));
                }
            }

            Dictionary<string, string> benchmarkToComparisons = new();
            Dictionary<string, List<MetricResult>> metricResults = new();

            foreach (var benchmark in benchmarkToRunToPaths)
            {
                List<string> paths = benchmark.Value;
                using (Process crankCompareProcess = new())
                {
                    crankCompareProcess.StartInfo.UseShellExecute = false;
                    crankCompareProcess.StartInfo.FileName = "crank";
                    crankCompareProcess.StartInfo.Arguments = $"compare {string.Join(" ", paths)}";
                    crankCompareProcess.StartInfo.RedirectStandardOutput = true;
                    crankCompareProcess.StartInfo.RedirectStandardError = true;
                    crankCompareProcess.StartInfo.CreateNoWindow = true;

                    // Grab the output and save it.
                    crankCompareProcess.Start();

                    string output = crankCompareProcess.StandardOutput.ReadToEnd();
                    crankCompareProcess.WaitForExit((int)configuration.Environment!.default_max_seconds * 1000);

                    if (crankCompareProcess.ExitCode == 0)
                    {
                        if (!metricResults.TryGetValue(benchmark.Key, out var metrics))
                        {
                            metrics = metricResults[benchmark.Key] = new();
                        }

                        metrics.AddRange(GetMetricResults(output, benchmark.Key));
                    }

                    benchmarkToComparisons[benchmark.Key] = output;
                }
            }

            using (StreamWriter sw = new StreamWriter(Path.Combine(configuration.Output.Path, "Results.md")))
            {
                // Ignore the summary section in case there is only one run.
                sw.WriteLine("# Summary");

                var topLevelSummarySet = new HashSet<string>(new List<string> { "Working Set (MB)", "Private Memory (MB)", "Requests/sec", "Mean Latency (MSec)", "Latency 50th (MSec)", "Latency 75th (MSec)", "Latency 90th (MSec)", "Latency 99th (MSec)" });
                sw.WriteLine($"|  | {string.Join("|", topLevelSummarySet)}");
                sw.WriteLine($"|--- | {string.Join("", Enumerable.Repeat("---|", topLevelSummarySet.Count))}");

                foreach (var r in metricResults)
                {
                    double workingSet = r.Value.FirstOrDefault(m => m.MetricName.Contains("Working Set (MB)") && m.MetricName.Contains("application"))?.DeltaPercent ?? double.NaN;
                    workingSet = Math.Round(workingSet, 2);
                    double privateMemory = r.Value.FirstOrDefault(m => m.MetricName.Contains("Private Memory (MB)") && m.MetricName.Contains("application"))?.DeltaPercent ?? double.NaN;
                    privateMemory = Math.Round(privateMemory, 2);

                    double rps = r.Value.FirstOrDefault(m => m.MetricName == "load_Requests/sec")?.DeltaPercent ?? double.NaN;
                    rps = Math.Round(rps, 2);

                    double meanLatency = r.Value.FirstOrDefault(m => m.MetricName.Contains("load_Mean latency"))?.DeltaPercent ?? double.NaN;
                    meanLatency = Math.Round(meanLatency, 2);

                    double latency50 = r.Value.FirstOrDefault(m => m.MetricName == "load_Latency 50th (ms)")?.DeltaPercent ?? double.NaN;
                    latency50 = Math.Round(latency50, 2);

                    double latency75 = r.Value.FirstOrDefault(m => m.MetricName == "load_Latency 75th (ms)")?.DeltaPercent ?? double.NaN;
                    latency75 = Math.Round(latency75, 2);

                    double latency90 = r.Value.FirstOrDefault(m => m.MetricName == "load_Latency 90th (ms)")?.DeltaPercent ?? double.NaN;
                    latency90 = Math.Round(latency90, 2);

                    double latency99 = r.Value.FirstOrDefault(m => m.MetricName == "load_Latency 99th (ms)")?.DeltaPercent ?? double.NaN;
                    latency99 = Math.Round(latency99, 2);

                    sw.WriteLine($"{r.Key} | {workingSet}% | {privateMemory}% | {rps}% | {meanLatency}% | {latency50}% | {latency75}% | {latency90}% | {latency99}% |");
                }

                sw.WriteLine("# Retry Notes");
                foreach (var retryDetail in retryDetails)
                {
                    sw.WriteLine($" - {retryDetail.run} for {retryDetail.benchmark} failed as {retryDetail.reason}.");
                }

                // Best way to deep-copy a configuration is to serialize and then deserialize.
                string configurationSerialized = Common.Serializer.Serialize(configuration);

                // We want to be able to rerun the failed tests in an easy manner. For this, we take any failed runs and create a 
                // new configuration based on the run configuration and then add these failed runs to filter on.
                // The process of creating a deep copy of the old configuration involves serializing and deserializing the current configuration.
                // We then iterate over all the failed runs, add them as items in the benchmark filters and persist the new configuration in the output path.
                List<KeyValuePair<string, ProcessExecutionDetails>> failedRuns = executionDetails.Where(exec => exec.Value.HasFailed).ToList();

                // This path is only valid if we have failed runs.
                if (failedRuns.Count > 0)
                {
                    try
                    {
                        configuration = Common.Deserializer.Deserialize<ASPNetBenchmarksConfiguration>(configurationSerialized);
                        Debug.Assert(configuration.benchmark_settings!.benchmarkFilters != null);

                        // Iterate over the failed runs.
                        foreach (var failureKvp in failedRuns)
                        {
                            // Extract the benchmark.
                            string? failedBenchmark = AspNetBenchmarksCommand.ExtractBenchmarkFromKey(failureKvp.Key);
                            if (string.IsNullOrEmpty(failedBenchmark))
                            {
                                continue;
                            }

                            configuration.benchmark_settings.benchmarkFilters.Add(failedBenchmark);
                        }

                        // Persist the new configuration in the output path.
                        string reserializedConfiguration = Common.Serializer.Serialize(configuration);
                        string failureOutputPath = Path.Combine(outputPath, $"{configuration.Name}_Failed.yaml");
                        File.WriteAllText(failureOutputPath, reserializedConfiguration);

                        sw.WriteLine($"\n Note: A new configuration yaml file with the failures are added: {failureOutputPath}. Simply reinvoke the 'aspnetbenchmark' command with the new configuration.");
                        AnsiConsole.MarkupLine($"[green bold] A new configuration yaml file with the failures are added: {Markup.Escape(failureOutputPath)}. Simply reinvoke the 'aspnetbenchmark' command with the new configuration. [/]");
                    }

                    catch (Exception ex)
                    {
                        // Don't throw an exception since the analysis must go on. 
                        AnsiConsole.MarkupLine($"[red bold] {nameof(AspNetBenchmarksAnalyzeCommand)}: Unable to persist a new configuration with the failed runs. Reason: {Markup.Escape(ex.Message)} Call Stack: {Markup.Escape(ex.StackTrace)} [/]");
                    }
                }
                sw.WriteLine();

                sw.AddIncompleteTestsSection(executionDetails);

                sw.WriteLine("# Results");

                foreach (var benchmark in benchmarkToComparisons)
                {
                    sw.WriteLine($"- [{benchmark.Key}](#{benchmark.Key.ToLower().Replace(" ", "-")})");
                }

                sw.WriteLine();

                sw.WriteLine("## Repro Steps");
                foreach (var kvp in executionDetails)
                {
                    sw.WriteLine($"### {kvp.Key}: ");
                    sw.WriteLine($"```{kvp.Value.CommandlineArgs}```");
                }

                sw.WriteLine("## Individual Results");
                foreach (var benchmark in benchmarkToComparisons)
                {
                    sw.WriteLine($"### {benchmark.Key}\n");
                    sw.Write(benchmark.Value);
                }
            }

            return metricResults;
        }

        internal static List<MetricResult> GetMetricResults(string output, string configuration)
        {
            List<MetricResult> results = new();

            try
            {
                // Split the two tables by "\r\n\r"
                string[] splitTables = output.Split("\r\n\r", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                string applicationResults = splitTables.FirstOrDefault(a => a.Contains("application"));
                results.AddRange(GetMetricResultsFromTable(applicationResults, "application", configuration));
                string loadResults = splitTables.FirstOrDefault(a => a.Contains("load"));
                results.AddRange(GetMetricResultsFromTable(loadResults, "load", configuration));
            }

            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red bold] {nameof(AspNetBenchmarksAnalyzeCommand)}: Exception since parsing the results from crank failed, please check any errors with the results from crank. Exception details: {Markup.Escape(ex.Message)} {Markup.Escape(ex.StackTrace ?? string.Empty)} [/]");
            }

            return results;
        }

        internal static List<MetricResult> GetMetricResultsFromTable(string table, string tableName, string configuration)
        {
            if (string.IsNullOrEmpty(table))
            {
                return new();
            }

            string[] resultsLineSplit = table.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            List<MetricResult> results = new();
            string[] firstLineSplit = resultsLineSplit[0].Split("|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            string baseline = firstLineSplit[1];
            string comparand = firstLineSplit[2];

            for (int runnerIdx = 0; runnerIdx < resultsLineSplit.Length; runnerIdx++)
            {
                if (runnerIdx < 2)
                {
                    continue;
                }

                string[] dissectedLine = resultsLineSplit[runnerIdx].Split("|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                string metricName = dissectedLine[0];
                bool baselineParsed = double.TryParse(dissectedLine[1], out var baselineMetric);

                // If the baseline cannot be parsed, we should ignore this metric.
                if (!baselineParsed)
                {
                    continue;
                }

                double comparandMetric = double.Parse(dissectedLine[2]);

                MetricResult result = new(key: configuration,
                                          metricName: $"{tableName}_{metricName}".TrimEnd(),
                                          baselineName: baseline,
                                          baselineValue: baselineMetric,
                                          comparandName: comparand,
                                          comparandValue: comparandMetric);
                results.Add(result);
            }

            return results;
        }
    }
}
