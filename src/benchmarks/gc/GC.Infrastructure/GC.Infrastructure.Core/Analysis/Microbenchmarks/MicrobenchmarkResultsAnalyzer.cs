using GC.Analysis.API;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    public static class MicrobenchmarkResultsAnalyzer
    {
        public static IReadOnlyDictionary<Run, ConcurrentDictionary<string, MicrobenchmarkResult>> Analyze(MicrobenchmarkConfiguration configuration, bool excludeTraces = false)
        {
            ConcurrentDictionary<Run, ConcurrentDictionary<string, MicrobenchmarkResult>> runsToResults = new();

            Parallel.ForEach(configuration.Runs, (run) =>
            {
                string outputPathForRun = Path.Combine(configuration.Output.Path, run.Key);
                run.Value.Name ??= run.Key;

                // Find the json path.
                string[] jsonFiles = Directory.GetFiles(outputPathForRun, "*full.json", SearchOption.AllDirectories);

                // Retrieve benchmarks from all the JSON files.
                Parallel.ForEach(jsonFiles, (jsonFile) =>
                {
                    MicrobenchmarkResults results = JsonConvert.DeserializeObject<MicrobenchmarkResults>(File.ReadAllText(jsonFile));
                    foreach (var benchmark in results?.Benchmarks)
                    {
                        string title = benchmark.FullName;
                        Statistics statistics = benchmark.Statistics;

                        if (!runsToResults.TryGetValue(run.Value, out var perBenchmarkData))
                        {
                            runsToResults[run.Value] = perBenchmarkData = new ConcurrentDictionary<string, MicrobenchmarkResult>();
                        }

                        runsToResults[run.Value][title] = new MicrobenchmarkResult
                        {
                            Statistics = statistics,
                            Parent = run.Value,
                            MicrobenchmarkName = title,
                        };
                    }
                });

                if (!excludeTraces)
                {
                    Dictionary<string, Analyzer> analyzers = AnalyzerManager.GetAllAnalyzers(outputPathForRun);

                    foreach (var analyzer in analyzers)
                    {
                        List<GCProcessData> allPertinentProcesses = analyzer.Value.GetProcessGCData("dotnet");
                        List<GCProcessData> corerunProcesses = analyzer.Value.GetProcessGCData("corerun");
                        allPertinentProcesses.AddRange(corerunProcesses);
                        foreach (var benchmark in runsToResults[run.Value])
                        {
                            GCProcessData? benchmarkGCData = null;
                            foreach (var process in allPertinentProcesses)
                            {
                                string commandLine = process.CommandLine.Replace("\"", "").Replace("\\", "");
                                string runCleaned = benchmark.Key.Replace("\"", "").Replace("\\", "");
                                if (commandLine.Contains(runCleaned) && commandLine.Contains("--benchmarkName"))
                                {
                                    benchmarkGCData = process;
                                    break;
                                }
                            }

                            if (benchmarkGCData != null)
                            {
                                int processID = benchmarkGCData.ProcessID;
                                benchmark.Value.GCData = benchmarkGCData;
                                benchmark.Value.ResultItem = new Presentation.GCPerfSim.ResultItem(benchmarkGCData, analyzer.Key, benchmark.Key);
                                /*
                                TODO: THIS NEEDS TO BE ADDED BACK.
                                if (configuration.Output.cpu_columns != null && configuration.Output.cpu_columns.Count > 0)
                                {
                                    // TODO: Add parameterize.
                                    benchmark.Value.GCData.Parent.AddCPUAnalysis(yamlPath: @"C:\Users\musharm\source\repos\GC.Analysis.API\GC.Analysis.API\CPUAnalysis\DefaultMethods.yaml",
                                        symbolLogFile: Path.Combine(configuration.Output.Path, run.Key, Guid.NewGuid() + ".txt"),
                                        symbolPath: Path.Combine(configuration.Output.Path, run.Key));
                                    var d1 = benchmark.Value.GCData.Parent.CPUAnalyzer.GetCPUDataForProcessName("dotnet");
                                    d1.AddRange(benchmark.Value.GCData.Parent.CPUAnalyzer.GetCPUDataForProcessName("corerun"));
                                    benchmark.Value.CPUData = d1.FirstOrDefault(p => p.ProcessID == processID);
                                }
                                */
                            }
                        }
                    };
                }
            });

            return runsToResults;
        }

        public static IReadOnlyList<MicrobenchmarkComparisonResults> GetComparisons(MicrobenchmarkConfiguration configuration, bool excludeTraces = false)
        {
            IReadOnlyDictionary<Run, ConcurrentDictionary<string, MicrobenchmarkResult>> runResults = Analyze(configuration, excludeTraces);
            List<MicrobenchmarkComparisonResults> comparisonResults = new();

            if (configuration.Output.run_comparisons != null)
            {
                foreach (var comparison in configuration.Output.run_comparisons)
                {
                    string[] breakup = comparison.Split(",", StringSplitOptions.TrimEntries);
                    string baselineName = breakup[0];
                    string runName = breakup[1];

                    Run run = runResults.Keys.FirstOrDefault(k => string.CompareOrdinal(k.Name, runName) == 0);
                    Run baselineRun = runResults.Keys.FirstOrDefault(k => string.CompareOrdinal(k.Name, baselineName) == 0);

                    List<MicrobenchmarkComparisonResult> microbenchmarkResults = new();

                    // Go through all the microbenchmarks for the current run and find the corresponding runs in the baseline.
                    foreach (var r in runResults[run])
                    {
                        string microbenchmarkName = r.Key;
                        if (runResults[baselineRun].TryGetValue(microbenchmarkName, out var m))
                        {
                            MicrobenchmarkComparisonResult microbenchmarkResult = new(m, r.Value);
                            microbenchmarkResults.Add(microbenchmarkResult);
                        }

                        else
                        {
                            // TODO: Log the fact that we haven't found a corresponding result in the baseline.
                            Console.WriteLine($"Microbenchmark: {microbenchmarkName} isn't found on the baseline: {baselineName} for run: {runName}");
                        }
                    }

                    // At this point of time, the lack thereof of either of the runs should be a non-issue.
                    comparisonResults.Add(new MicrobenchmarkComparisonResults(baselineName, runName, microbenchmarkResults));
                }
            }

            // Default case where the run comparisons aren't specified.
            else
            {
                string baselineName = configuration.Runs.FirstOrDefault(r => r.Value.is_baseline).Key;
                KeyValuePair<Run, ConcurrentDictionary<string, MicrobenchmarkResult>> baselineResult = baselineName != null ? runResults.First(r => r.Key.Name == baselineName) : runResults.First();

                // For each run, we want to grab it and it's baseline and then do a per microbenchmark association.
                foreach (var runResult in runResults)
                {
                    Run run = runResult.Key;
                    string runName = run.Name;

                    if (string.CompareOrdinal(runName, baselineName) == 0)
                    {
                        continue;
                    }

                    List<MicrobenchmarkComparisonResult> microbenchmarkResults = new();

                    // Go through all the microbenchmarks for the current run and find the corresponding runs in the baseline.
                    foreach (var r in runResult.Value)
                    {
                        string microbenchmarkName = r.Key;
                        if (baselineResult.Value.TryGetValue(microbenchmarkName, out var m))
                        {
                            MicrobenchmarkComparisonResult microbenchmarkResult = new(m, r.Value);
                            microbenchmarkResults.Add(microbenchmarkResult);
                        }

                        else
                        {
                            Console.WriteLine($"Microbenchmark: {microbenchmarkName} isn't found on the baseline: {baselineName} for run: {runName}");
                            // TODO: Log the fact that we haven't found a corresponding result in the baseline.
                        }
                    }

                    comparisonResults.Add(new MicrobenchmarkComparisonResults(baselineName, runName, microbenchmarkResults));
                }
            }

            return comparisonResults;
        }
    }
}
