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

            Dictionary<string, List<MetricResult>> results = ExecuteAnalysis(configuration, configurationToCommand, new());
            return 0;
        }

        public static Dictionary<string, List<MetricResult>> ExecuteAnalysis(ASPNetBenchmarksConfiguration configuration, Dictionary<string, string> configurationToCommand, Dictionary<string, ProcessExecutionDetails> executionDetails)
        {
            // Benchmark to Run to Path. 
            Dictionary<string, List<string>> benchmarkToRunToPaths = new();

            // For each Run, grab the paths of each of the benchmarks.
            string outputPath = configuration.Output.Path;
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

            // Launch new process.
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
                    crankCompareProcess.WaitForExit((int)configuration.Environment.default_max_seconds * 1000);

                    if (crankCompareProcess.ExitCode == 0)
                    {
                        if (!metricResults.TryGetValue(benchmark.Key, out var metrics ))
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
                sw.WriteLine("# Summary");

                var topLevelSummarySet = new HashSet<string>(new List<string> { "Working Set (MB)", "Private Memory (MB)", "Requests/sec", "Mean Latency (MSec)", "Latency 50th (MSec)", "Latency 75th (MSec)", "Latency 90th (MSec)", "Latency 99th (MSec)" });
                sw.WriteLine($"|  | {string.Join("|", topLevelSummarySet)}");
                sw.WriteLine($"|--- | {string.Join( "", Enumerable.Repeat("---|", topLevelSummarySet.Count ))}");

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

                sw.AddIncompleteTestsSection(executionDetails);

                sw.WriteLine("# Results");

                foreach (var benchmark in benchmarkToComparisons)
                {
                    sw.WriteLine($"- [{benchmark.Key}](##{benchmark.Key})");
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

            string baseline  = firstLineSplit[1];
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
