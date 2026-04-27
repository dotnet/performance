using GC.Analysis.API;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using GC.Infrastructure.Core.Presentation.GCPerfSim;
using Newtonsoft.Json;

namespace GC.Infrastructure.Core.Analysis
{
    public sealed class MicrobenchmarkResult
    {
        public Statistics Statistics { get; set; }

        [JsonIgnore]
        public GCProcessData? GCData { get; set; }

        public ResultItem ResultItem { get; set; }

        [JsonIgnore]
        public CPUProcessData? CPUData { get; set; }
        public Run Parent { get; set; }
        public string MicrobenchmarkName { get; set; }
        public Dictionary<string, double?> OtherMetrics { get; set; } = new();

        private static readonly IReadOnlyDictionary<string, Func<Statistics, double?>> _customStatisticsCalculationMap = new Dictionary<string, Func<Statistics, double?>>(StringComparer.OrdinalIgnoreCase)
        {
            { "number of iterations", (Statistics stats) => stats.N },
            { "min", (Statistics stats) => stats.Min },
            { "max", (Statistics stats) => stats.Max },
            { "median", (Statistics stats) => stats.Median },
            { "q1", (Statistics stats) => stats.Q1 },
            { "q3", (Statistics stats) => stats.Q3 },
            { "variance", (Statistics stats) => stats.Variance },
            { "standard deviation", (Statistics stats) => stats.StandardDeviation },
            { "skewness", (Statistics stats) => stats.Skewness },
            { "kurtosis", (Statistics stats) => stats.Kurtosis },
            { "standard error", (Statistics stats) => stats.StandardError },
            { "standard error / mean", (Statistics stats) => stats.StandardError / stats.Mean },
        };

        public static double? LookupStatisticsCalculation(string columnName, MicrobenchmarkResult result)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return null;
            }

            if (!_customStatisticsCalculationMap.TryGetValue(columnName, out var val))
            {
                return null;
            }

            else
            {
                return val.Invoke(result.Statistics);
            }
        }
    }

    public sealed class Benchmark
    {
        public string DisplayInfo { get; set; }
        public string Namespace { get; set; }
        public string Type { get; set; }
        public string Method { get; set; }
        public string MethodTitle { get; set; }
        public string Parameters { get; set; }
        public string FullName { get; set; }
        public Statistics Statistics { get; set; }
        public Memory Memory { get; set; }
        public List<Measurement> Measurements { get; set; }
        public List<Metric> Metrics { get; set; }
    }

    public sealed class ChronometerFrequency
    {
        public int Hertz { get; set; }
    }

    public sealed class ConfidenceInterval
    {
        public int N { get; set; }
        public double? Mean { get; set; }
        public double? StandardError { get; set; }
        public int? Level { get; set; }
        public double? Margin { get; set; }
        public double? Lower { get; set; }
        public double? Upper { get; set; }
    }

    public sealed class Descriptor
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Legend { get; set; }
        public string NumberFormat { get; set; }
        public int UnitType { get; set; }
        public string Unit { get; set; }
        public bool TheGreaterTheBetter { get; set; }
        public int PriorityInCategory { get; set; }
    }

    public sealed class HostEnvironmentInfo
    {
        public string BenchmarkDotNetCaption { get; set; }
        public string BenchmarkDotNetVersion { get; set; }
        public string OsVersion { get; set; }
        public string ProcessorName { get; set; }
        public int PhysicalProcessorCount { get; set; }
        public int PhysicalCoreCount { get; set; }
        public int LogicalCoreCount { get; set; }
        public string RuntimeVersion { get; set; }
        public string Architecture { get; set; }
        public bool HasAttachedDebugger { get; set; }
        public bool HasRyuJit { get; set; }
        public string Configuration { get; set; }
        public string DotNetCliVersion { get; set; }
        public ChronometerFrequency ChronometerFrequency { get; set; }
        public string HardwareTimerKind { get; set; }
    }

    public sealed class Measurement
    {
        public string IterationMode { get; set; }
        public string IterationStage { get; set; }
        public int LaunchIndex { get; set; }
        public int IterationIndex { get; set; }
        public long Operations { get; set; }
        public long Nanoseconds { get; set; }
    }

    public sealed class Memory
    {
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public int TotalOperations { get; set; }
        public long BytesAllocatedPerOperation { get; set; }
    }

    public sealed class Metric
    {
        public double Value { get; set; }
        public Descriptor Descriptor { get; set; }
    }

    public sealed class Percentiles
    {
        public double P0 { get; set; }
        public double P25 { get; set; }
        public double P50 { get; set; }
        public double P67 { get; set; }
        public double P80 { get; set; }
        public double P85 { get; set; }
        public double P90 { get; set; }
        public double P95 { get; set; }
        public double P100 { get; set; }
    }

    public sealed class MicrobenchmarkResults
    {
        public string Title { get; set; }
        public HostEnvironmentInfo HostEnvironmentInfo { get; set; }
        public List<Benchmark> Benchmarks { get; set; }
    }

    public sealed class Statistics
    {
        public List<double> OriginalValues { get; set; }
        public int N { get; set; }
        public double? Min { get; set; }
        public double? LowerFence { get; set; }
        public double? Q1 { get; set; }
        public double? Median { get; set; }
        public double? Mean { get; set; }
        public double? Q3 { get; set; }
        public double? UpperFence { get; set; }
        public double? Max { get; set; }
        public double? InterquartileRange { get; set; }
        public List<double?> LowerOutliers { get; set; }
        public List<object> UpperOutliers { get; set; }
        public List<double?> AllOutliers { get; set; }
        public double? StandardError { get; set; }
        public double? Variance { get; set; }
        public double? StandardDeviation { get; set; }
        public double? Skewness { get; set; }
        public double? Kurtosis { get; set; }
        public ConfidenceInterval? ConfidenceInterval { get; set; }
        public Percentiles Percentiles { get; set; }
    }
}