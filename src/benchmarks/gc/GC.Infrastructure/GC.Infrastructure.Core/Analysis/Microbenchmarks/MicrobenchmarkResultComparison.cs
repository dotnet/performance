using GC.Analysis.API;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    public static class MicrobenchmarkResultComparison
    {
        private static readonly int _CPUCount = System.Environment.ProcessorCount;
        private static readonly Dictionary<string, string> _benchmarkNameToTraceFilePatternMap = new()
        {
            { "ByteMark.BenchBitOps", "ByteMark.BenchBitOps"},
            { "System.Collections.CtorGivenSize<String>.Array(Size: 512)", "System.Collections.CtorGivenSize_String_.Array_size_512_"},
            { "System.Collections.Tests.Perf_BitArray.BitArrayByteArrayCtor(Size: 512)", "System.Collections.Tests.Perf_BitArray.BitArrayByteArrayCtor_size_512_"},
            { "System.IO.Tests.Perf_File.ReadAllBytes(size: 104857600)", "System.IO.Tests.Perf_File.ReadAllBytes_size_104857600_"},
            { "System.IO.Tests.Perf_File.ReadAllBytesAsync(size: 104857600)", "System.IO.Tests.Perf_File.ReadAllBytesAsync_size_104857600_"},
            { "System.Linq.Tests.Perf_Enumerable.ToArray(input: ICollection)", "System.Linq.Tests.Perf_Enumerable.ToArray_"},
            { "System.Linq.Tests.Perf_Enumerable.ToArray(input: IEnumerable)", "System.Linq.Tests.Perf_Enumerable.ToArray_"},
            { "System.Numerics.Tests.Perf_BigInteger.Add(arguments: 65536,65536 bits)", "System.Numerics.Tests.Perf_BigInteger.Add_arguments_65536_"},
            { "System.Numerics.Tests.Perf_BigInteger.Subtract(arguments: 65536,65536 bits)", "System.Numerics.Tests.Perf_BigInteger.Subtract_arguments_65536_"},
            { "System.Tests.Perf_GC<Byte>.AllocateArray(length: 1000, pinned: False)", "System.Tests.Perf_GC_Byte_.AllocateArray_length_1000,_"},
            { "System.Tests.Perf_GC<Byte>.AllocateArray(length: 1000, pinned: True)", "System.Tests.Perf_GC_Byte_.AllocateArray_length_1000,_"},
            { "System.Tests.Perf_GC<Byte>.AllocateArray(length: 10000, pinned: False)", "System.Tests.Perf_GC_Byte_.AllocateArray_length_10000,_"},
            { "System.Tests.Perf_GC<Byte>.AllocateArray(length: 10000, pinned: True)", "System.Tests.Perf_GC_Byte_.AllocateArray_length_10000,_"},
            { "System.Tests.Perf_GC<Byte>.AllocateUninitializedArray(length: 1000, pinned: False)", "System.Tests.Perf_GC_Byte_.AllocateUninitializedArray_length_1000,_"},
            { "System.Tests.Perf_GC<Byte>.AllocateUninitializedArray(length: 1000, pinned: True)", "System.Tests.Perf_GC_Byte_.AllocateUninitializedArray_length_1000,_"},
            { "System.Tests.Perf_GC<Byte>.AllocateUninitializedArray(length: 10000, pinned: False)", "System.Tests.Perf_GC_Byte_.AllocateUninitializedArray_length_10000,_"},
            { "System.Tests.Perf_GC<Byte>.AllocateUninitializedArray(length: 10000, pinned: True)", "System.Tests.Perf_GC_Byte_.AllocateUninitializedArray_length_10000,_"},
            { "System.Tests.Perf_GC<Byte>.NewOperator_Array(length: 1000)", "System.Tests.Perf_GC_Byte_.NewOperator_Array_length_1000_"},
            { "System.Tests.Perf_GC<Byte>.NewOperator_Array(length: 10000)", "System.Tests.Perf_GC_Byte_.NewOperator_Array_length_10000_"},
            { "System.Tests.Perf_GC<Char>.AllocateArray(length: 1000, pinned: False)", "System.Tests.Perf_GC_Char_.AllocateArray_length_1000,_"},
            { "System.Tests.Perf_GC<Char>.AllocateArray(length: 1000, pinned: True)", "System.Tests.Perf_GC_Char_.AllocateArray_length_1000,_"},
            { "System.Tests.Perf_GC<Char>.AllocateArray(length: 10000, pinned: False)", "System.Tests.Perf_GC_Char_.AllocateArray_length_10000,_"},
            { "System.Tests.Perf_GC<Char>.AllocateArray(length: 10000, pinned: True)", "System.Tests.Perf_GC_Char_.AllocateArray_length_10000,_"},
            { "System.Tests.Perf_GC<Char>.AllocateUninitializedArray(length: 1000, pinned: False)", "System.Tests.Perf_GC_Char_.AllocateUninitializedArray_length_1000,_"},
            { "System.Tests.Perf_GC<Char>.AllocateUninitializedArray(length: 1000, pinned: True)", "System.Tests.Perf_GC_Char_.AllocateUninitializedArray_length_1000,_"},
            { "System.Tests.Perf_GC<Char>.AllocateUninitializedArray(length: 10000, pinned: False)", "System.Tests.Perf_GC_Char_.AllocateUninitializedArray_length_10000,_"},
            { "System.Tests.Perf_GC<Char>.AllocateUninitializedArray(length: 10000, pinned: True)", "System.Tests.Perf_GC_Char_.AllocateUninitializedArray_length_10000,_"},
            { "System.Tests.Perf_GC<Char>.NewOperator_Array(length: 1000)", "System.Tests.Perf_GC_Char_.NewOperator_Array_length_1000_"},
            { "System.Tests.Perf_GC<Char>.NewOperator_Array(length: 10000)", "System.Tests.Perf_GC_Char_.NewOperator_Array_length_10000_"},
        };

        public static ConcurrentBag<Tuple<Run, BdnJsonResult, string>> LoadBdnJsonResults(MicrobenchmarkConfiguration configuration)
        {
            ConcurrentBag<Tuple<Run, BdnJsonResult, string>> bdnJsonResults = new();
            Parallel.ForEach(configuration.Runs, (run) =>
            {
                string outputPathForRun = Path.Combine(configuration.Output.Path, run.Key);
                string[] jsonFiles = Directory.GetFiles(outputPathForRun, "*full.json", SearchOption.AllDirectories);
                Parallel.ForEach(jsonFiles, jsonPath =>
                {
                    run.Value.Name ??= run.Key;
                    BdnJsonResult? results = JsonConvert.DeserializeObject<BdnJsonResult>(File.ReadAllText(jsonPath));
                    if (results != null)
                    {
                        bdnJsonResults.Add(new(run.Value, results, jsonPath));
                    }
                });
            });
            return bdnJsonResults;
        }

        // TODO: We should specify relationship between json files and trace files before running benchmarks instead of relying on file name patterns.
        // This will make the mapping more robust and less prone to errors due to file naming.
        public static Dictionary<string, string> MapJsonToTrace(string outputPath, ConcurrentBag<Tuple<Run, BdnJsonResult, string>> bdnJsonResults)
        {
            Dictionary<string, string> jsonToTrace = new();
            foreach (var groupForRun in bdnJsonResults.GroupBy(t => t.Item1))
            {
                var run = groupForRun.Key;

                // GroupBy(t => t.Item2.Benchmarks.First().FullName) is not a bug:
                // In single *full.json, multiple benchmarks stands for multiple input parameter combinations for the same benchmark
                // If trace collection is enabled, process data for all those parameter combinations will be in the same trace file
                foreach (var g in groupForRun.GroupBy(t => t.Item2.Benchmarks.First().FullName))
                {
                    var benchmarkName = g.Key;
                    var sortedJsonFiles = GoodLinq.Select(g, t => t.Item3)
                        .OrderBy(jsonFile => Path.GetFileName(Path.GetDirectoryName(jsonFile)))
                        .ToArray();

                    if (!_benchmarkNameToTraceFilePatternMap.ContainsKey(benchmarkName))
                    {
                        throw new InvalidOperationException($"Benchmark name {benchmarkName} does not have a corresponding trace file pattern in the map.");
                    }
                    var traceFileNameTemplate = _benchmarkNameToTraceFilePatternMap[benchmarkName];
                    string outputPathForRun = Path.Combine(outputPath, run.Name);
                    var sortedTraceFiles = Directory.GetFiles(outputPathForRun, $"{traceFileNameTemplate}*.etl.zip", SearchOption.TopDirectoryOnly)
                        .OrderBy(traceFile =>
                        {
                            var match = Regex.Match(Path.GetFileName(traceFile), @"_(\d+)\.etl\.zip$");
                            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
                        })
                        .ToArray();

                    if (sortedJsonFiles.Length != sortedTraceFiles.Length)
                    {
                        throw new InvalidOperationException(
                            $"The number of JSON files ({sortedJsonFiles.Length}) does not match the number of trace files ({sortedTraceFiles.Length}) for benchmark: {benchmarkName}");
                    }

                    for (int i = 0; i < sortedJsonFiles.Length; i++)
                    {
                        jsonToTrace[sortedJsonFiles[i]] = sortedTraceFiles[i];
                    }
                }
            }

            return jsonToTrace;
        }

        public static ConcurrentBag<MicrobenchmarkResult> 
            AnalyzeMicrobenchmarkResults(MicrobenchmarkConfiguration configuration,
                                         ConcurrentBag<Tuple<Run, BdnJsonResult, string>> bdnJsonResults,
                                         bool excludeTraces = false)
        {
            ConcurrentBag<MicrobenchmarkResult> microbenchmarkResults = new();

            Dictionary<string, string> jsonToTraceMap = new();
            if ((!excludeTraces) && configuration.TraceConfigurations?.Type != "none")
            {
                jsonToTraceMap = MapJsonToTrace(configuration.Output.Path, bdnJsonResults);
            }
            
            ParallelOptions options = new() 
            {
                MaxDegreeOfParallelism = _CPUCount * 2
            };

            int count = 0;
            object _lock = new();

            Parallel.ForEach(bdnJsonResults, options, t =>
            {
                var run = t.Item1;
                var bdnJsonResult = t.Item2;
                var jsonPath = t.Item3;

                List<Benchmark>? benchmarks = bdnJsonResult?.Benchmarks;

                if (benchmarks == null)
                {
                    return;
                }

                if ((!excludeTraces) && configuration.TraceConfigurations.Type != "none")
                {
                    string outputPathForRun = Path.Combine(configuration.Output.Path, run.Name!);
                    string tracePath = jsonToTraceMap.GetValueOrDefault(jsonPath, "");

                    using (var analyzer = AnalyzerManager.GetAnalyzer(tracePath))
                    {
                        List<GCProcessData> allPertinentProcesses = analyzer.GetProcessGCData("dotnet");
                        List<GCProcessData> corerunProcesses = analyzer.GetProcessGCData("corerun");
                        allPertinentProcesses.AddRange(corerunProcesses);

                        foreach (var benchmark in benchmarks)
                        {
                            Statistics statistics = benchmark.Statistics;
                            var benchmarkFullName = benchmark.FullName;

                            MicrobenchmarkResult? microbenchmarkResult = null;
                            GCProcessData? benchmarkGCData = null;
                            foreach (var process in allPertinentProcesses)
                            {
                                string commandLine = process.CommandLine.Replace("\"", "").Replace("\\", "");
                                string runCleaned = benchmark.FullName.Replace("\"", "").Replace("\\", "");
                                if (commandLine.Contains(runCleaned) && commandLine.Contains("--benchmarkName"))
                                {
                                    benchmarkGCData = process;
                                    break;
                                }
                            }
                            if (benchmarkGCData != null)
                            {
                                int processID = benchmarkGCData.ProcessID;

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
                                microbenchmarkResult = new(benchmarkFullName,
                                                            run,
                                                            benchmark,
                                                            gcData: benchmarkGCData,
                                                            gcTraceMetrics: new GCTraceMetrics(benchmarkGCData, tracePath, benchmark.FullName),
                                                            additionalReportMetrics: configuration.Output.additional_report_metrics,
                                                            cpuColumns: configuration.Output.cpu_columns,
                                                            columns: configuration.Output.Columns);
                                microbenchmarkResults.Add(microbenchmarkResult!);
                            }
                        } 
                    }
                }
                else
                {
                    foreach (var benchmark in benchmarks)
                    {
                        Statistics statistics = benchmark.Statistics;
                        var benchmarkFullName = benchmark.FullName;

                        MicrobenchmarkResult? microbenchmarkResult = null;
                        microbenchmarkResult = new(benchmarkFullName,
                                                run,
                                                benchmark,
                                                additionalReportMetrics: configuration.Output.additional_report_metrics,
                                                cpuColumns: configuration.Output.cpu_columns,
                                                columns: configuration.Output.Columns);
                        microbenchmarkResults.Add(microbenchmarkResult!);
                    }
                }

                lock (_lock)
                {
                    count = count + 1;
                    Console.Write($"\r{count}/{bdnJsonResults.Count} BDN results analyzed.");
                }
            });

            Console.WriteLine();
            return microbenchmarkResults;
        }

        public static List<MicrobenchmarkComparisonResult> CompareMicrobenchmarkResults(MicrobenchmarkConfiguration configuration, IEnumerable<MicrobenchmarkResult> microbenchmarkResults, bool excludeTraces = false)
        {
            bool includeTraces = (!excludeTraces) && (configuration.TraceConfigurations.Type != "none");
            var microbenchmarkResultsGroupedByBenchmarkName = microbenchmarkResults
                .GroupBy(microbenchmarkResult => microbenchmarkResult.MicrobenchmarkName);

            List<MicrobenchmarkComparisonResult> comparisonResults = new();
            object _lock = new();
            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = _CPUCount
            };
            Parallel.ForEach(microbenchmarkResultsGroupedByBenchmarkName, options, microbenchmarkResultsGroup =>
            {
                if (configuration.Output.run_comparisons != null)
                {
                    foreach (var comparison in configuration.Output.run_comparisons)
                    {
                        string[] breakup = comparison.Split(",", StringSplitOptions.TrimEntries);
                        string baselineName = breakup[0];
                        string runName = breakup[1];

                        var baselineMicrobenchmarkResults = GoodLinq.Where(microbenchmarkResultsGroup, r => r.Parent.Name == baselineName);
                        var comparandMicrobenchmarkResults = GoodLinq.Where(microbenchmarkResultsGroup, r => r.Parent.Name == runName);

                        lock (_lock)
                        {
                            comparisonResults.Add(new(baselineMicrobenchmarkResults, comparandMicrobenchmarkResults, includeTraces));
                        }
                    }
                }

                // Default case where the run comparisons aren't specified.
                else
                {
                    var baselineMicrobenchmarkResults = GoodLinq.Where(microbenchmarkResultsGroup, r => r.Parent.is_baseline);
                    var comparandMicrobenchmarkResults = GoodLinq.Where(microbenchmarkResultsGroup, r => !r.Parent.is_baseline);

                    lock (_lock)
                    {
                        comparisonResults.Add(new(baselineMicrobenchmarkResults, comparandMicrobenchmarkResults, includeTraces));
                    }
                }
            });

            return comparisonResults;
        }

        public static List<MicrobenchmarkComparisonResults> GroupComparisonResultsByName(MicrobenchmarkConfiguration configuration, List<MicrobenchmarkComparisonResult> comparisonResultForAllBenchmarks, bool excludeTraces = false)
        {
            List<MicrobenchmarkComparisonResults> allComparisonResults = new();

            comparisonResultForAllBenchmarks
                .GroupBy(r => r.ComparisonName)
                .ToList()
                .ForEach(group =>
                {
                    string baselineName = group.FirstOrDefault()?.BaselineRunName ?? "Baseline";
                    string runName = group.FirstOrDefault()?.ComparandRunName ?? "Comparand";
                    allComparisonResults.Add(new(baselineName, runName, group.ToList()));
                });

            return allComparisonResults;
        }
    }
}