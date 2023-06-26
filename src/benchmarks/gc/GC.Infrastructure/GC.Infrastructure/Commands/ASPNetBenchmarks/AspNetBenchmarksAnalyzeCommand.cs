using GC.Infrastructure.Core.Analysis;
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
            [CommandOption("-o|--output")]
            public string? OutputPath { get; init; } = "";

            [Description("Path to Baseline Json.")]
            [CommandOption("-b|--baseline")]
            public string? BaselineJson { get; init; }

            [Description("Path to Comparand Json.")]
            [CommandOption("-p|--comparand")]
            public string? ComparandJson { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] AspNetBenchmarkAnalyzeSettings settings)
        {
            string output = null;
            using (Process crankCompareProcess = new())
            {
                crankCompareProcess.StartInfo.UseShellExecute = false;
                crankCompareProcess.StartInfo.FileName = "crank";
                crankCompareProcess.StartInfo.Arguments = $"compare {settings.BaselineJson} {settings.ComparandJson}";
                crankCompareProcess.StartInfo.RedirectStandardOutput = true;
                crankCompareProcess.StartInfo.RedirectStandardError = true;
                crankCompareProcess.StartInfo.CreateNoWindow = true;

                // Grab the output and save it.
                crankCompareProcess.Start();

                output = crankCompareProcess.StandardOutput.ReadToEnd();
                List<MetricResult> results = GetMetricResults(output, settings.OutputPath);

                crankCompareProcess.WaitForExit();
            }

            File.WriteAllText(Path.Combine(settings.OutputPath, "Results.md"), output);
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

                var topLevelASP = new HashSet<string>(new List<string> { "Working Set (MB)", "Private Memory (MB)", "Requests/sec" });
                sw.WriteLine($"|  | {string.Join("|", topLevelASP)}");
                sw.WriteLine($"|--- | {string.Join( "", Enumerable.Repeat("---|", topLevelASP.Count ))}");

                foreach (var r in metricResults)
                {
                    double workingSet    = r.Value.FirstOrDefault(m => m.MetricName == "application_Working Set (MB)")?.DeltaPercent ?? double.NaN;
                    double privateMemory = r.Value.FirstOrDefault(m => m.MetricName == "application_Private Memory (MB)")?.DeltaPercent ?? double.NaN;
                    double rps           = r.Value.FirstOrDefault(m => m.MetricName == "load_Requests/sec")?.DeltaPercent ?? double.NaN;

                    sw.WriteLine($"{r.Key} | {workingSet}% | {privateMemory}% | {rps}%");
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
                string applicationResults = splitTables[0];
                results.AddRange(GetMetricResultsFromTable(applicationResults, "application", configuration));
                string loadResults = splitTables[1];
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
