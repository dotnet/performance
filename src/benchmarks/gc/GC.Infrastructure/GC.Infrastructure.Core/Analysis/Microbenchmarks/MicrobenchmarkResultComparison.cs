using GC.Analysis.API;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    public static class MicrobenchmarkResultComparison
    {
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

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, List<string>>> _benchmarkFullNameToJsonForRun = new();

        public static ConcurrentDictionary<string, List<string>> MapBenchmarkFullNameToJsonForRun(string outputPathForRun)
        {
            return _benchmarkFullNameToJsonForRun.GetOrAdd(outputPathForRun, path =>
            {
                ConcurrentDictionary<string, List<string>> benchmarkFullNameJsonMap = new();

                string[] jsonFiles = Directory.GetFiles(outputPathForRun, "*full.json", SearchOption.AllDirectories);

                Parallel.ForEach(jsonFiles, (jsonFile) => {
                    BdnJsonResult results = JsonConvert.DeserializeObject<BdnJsonResult>(File.ReadAllText(jsonFile));
                    string fullName = results.Benchmarks.FirstOrDefault()?.FullName;
                    benchmarkFullNameJsonMap[fullName] = benchmarkFullNameJsonMap.GetValueOrDefault(fullName, new());
                    benchmarkFullNameJsonMap[fullName].Add(jsonFile);
                });

                return benchmarkFullNameJsonMap;
            });
        }

        public static ConcurrentDictionary<string, string> MapJsonToTraceForSingleBenchmarkRun(string outputPathForRun, string benchmarkFullName)
        {
            ConcurrentDictionary<string, string> jsonTraceMap = new();

            var benchmarkFullNameJsonMap = MapBenchmarkFullNameToJsonForRun(outputPathForRun);

            string[] jsonFiles = benchmarkFullNameJsonMap.GetValueOrDefault(benchmarkFullName, new()).ToArray();

            Parallel.ForEach(jsonFiles, (jsonFile) => {
                BdnJsonResult results = JsonConvert.DeserializeObject<BdnJsonResult>(File.ReadAllText(jsonFile));
                string fullName = results.Benchmarks.FirstOrDefault()?.FullName;
                if (fullName != benchmarkFullName)
                {
                    return;
                }
                // placeholder
                jsonTraceMap[jsonFile] = "";
            });

            string[] sortedJsonFiles = jsonTraceMap.Keys
                .OrderBy(jsonFile => Path.GetFileName(Path.GetDirectoryName(jsonFile)))
                .ToArray();

            string traceFileNameTemplate = _benchmarkNameToTraceFilePatternMap[benchmarkFullName];

            string[] sortedTraceFiles = Enumerable.Where(Directory.GetFiles(outputPathForRun, "*.etlx", SearchOption.TopDirectoryOnly), traceFile =>
                    Path.GetFileName(traceFile).ToLower().Contains(traceFileNameTemplate.ToLower()))
                .OrderBy(traceFile => traceFile)
                .ToArray();

            if (sortedJsonFiles.Length != sortedTraceFiles.Length)
            {
                throw new InvalidOperationException(
                    $"The number of JSON files ({sortedJsonFiles.Length}) does not match the number of trace files ({sortedTraceFiles.Length}) for benchmark: {benchmarkFullName}");
            }

            for (int idx = 0; idx < sortedJsonFiles.Length; idx++)
            {
                jsonTraceMap[sortedJsonFiles[idx]] = sortedTraceFiles[idx];
            }

            return jsonTraceMap;
        }

        public static IReadOnlyDictionary<Run,List<MicrobenchmarkResult>> AnalyzeMicrobenchmarkResultsForSingleBenchmark(MicrobenchmarkConfiguration configuration, string benchmarkFullName, bool excludeTraces = false)
        {
            ConcurrentDictionary<Run, List<MicrobenchmarkResult>> runsToResults = new();

            Parallel.ForEach(configuration.Runs, (run) =>
            {
                string outputPathForRun = Path.Combine(configuration.Output.Path, run.Key);
                run.Value.Name ??= run.Key;

                var jsonTraceMap = MapJsonToTraceForSingleBenchmarkRun(outputPathForRun, benchmarkFullName);

                runsToResults[run.Value] = runsToResults.GetValueOrDefault(run.Value, new());

                Parallel.ForEach(jsonTraceMap, jsonTracePair => {
                    string jsonPath = jsonTracePair.Key;
                    string tracePath = jsonTracePair.Value;

                    BdnJsonResult results = JsonConvert.DeserializeObject<BdnJsonResult>(File.ReadAllText(jsonPath));

                    foreach (var benchmark in results?.Benchmarks)
                    {
                        Statistics statistics = benchmark.Statistics;

                        MicrobenchmarkResult microbenchmarkResult = new()
                        {
                            Statistics = statistics,
                            Parent = run.Value,
                            MicrobenchmarkName = benchmarkFullName,
                        };

                        if (!excludeTraces)
                        {
                            Analyzer analyzer = AnalyzerManager.GetAnalyzer(tracePath);

                            List<GCProcessData> allPertinentProcesses = analyzer.GetProcessGCData("dotnet");
                            List<GCProcessData> corerunProcesses = analyzer.GetProcessGCData("corerun");
                            allPertinentProcesses.AddRange(corerunProcesses);

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
                                microbenchmarkResult.GCData = benchmarkGCData;
                                microbenchmarkResult.GCTraceMetrics = new GCTraceMetrics(benchmarkGCData, tracePath, benchmark.FullName);
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
                        runsToResults[run.Value].Add(microbenchmarkResult);
                    }
                });
                
            });

            return runsToResults;
        }

        public static List<MicrobenchmarkComparisonResult> CompareMicrobenchmarkResultForBenchmark(MicrobenchmarkConfiguration configuration, string benchmarkFullName, bool excludeTraces = false)
        {
            IReadOnlyDictionary<Run, List<MicrobenchmarkResult>> runResults = AnalyzeMicrobenchmarkResultsForSingleBenchmark(configuration, benchmarkFullName, excludeTraces);
            List<MicrobenchmarkComparisonResult> comparisonResults = new();

            if (configuration.Output.run_comparisons != null)
            {
                foreach (var comparison in configuration.Output.run_comparisons)
                {
                    string[] breakup = comparison.Split(",", StringSplitOptions.TrimEntries);
                    string baselineName = breakup[0];
                    string runName = breakup[1];

                    var baselineRuns = GoodLinq.Where(runResults.Keys, r => r.Name == baselineName);
                    var comparandRuns = GoodLinq.Where(runResults.Keys, r => r.Name == runName);

                    var baselineMicrobenchmarkResults = GoodLinq.Select(baselineRuns, b => runResults[b]).SelectMany(r => r);
                    var comparandMicrobenchmarkResults = GoodLinq.Select(comparandRuns, c => runResults[c]).SelectMany(r => r);

                    comparisonResults.Add(new(baselineMicrobenchmarkResults, comparandMicrobenchmarkResults));
                }
            }

            // Default case where the run comparisons aren't specified.
            else
            {
                var baselineRuns = GoodLinq.Where(runResults.Keys, r => r.is_baseline);
                var comparandRuns = GoodLinq.Where(runResults.Keys, r => !r.is_baseline);

                var baselineMicrobenchmarkResults = GoodLinq.Select(baselineRuns, b => runResults[b]).SelectMany(r => r);
                var comparandMicrobenchmarkResults = GoodLinq.Select(comparandRuns, c => runResults[c]).SelectMany(r => r);

                comparisonResults.Add(new(baselineMicrobenchmarkResults, comparandMicrobenchmarkResults));
            }

            return comparisonResults;
        }
    }
}
