using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using API = GC.Analysis.API;

namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    public sealed class MicrobenchmarkResult
    {
        public static readonly IReadOnlyDictionary<string, Func<Statistics, double>> CustomStatisticsCalculationMap = new Dictionary<string, Func<Statistics, double>>(StringComparer.OrdinalIgnoreCase)
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

        public static readonly Dictionary<string, Func<API.GCProcessData, double>> CustomAggregateCalculationMap = new Dictionary<string, Func<API.GCProcessData, double>>(StringComparer.OrdinalIgnoreCase)
        {
            {  "gc count", (gc) => gc.Stats.Count },
            {  "non induced gc count", (gc) => gc.Stats.Count - gc.GCs.Count(g => g.Reason == GCReason.Induced)},
            {  "induced gc count", (gc) => gc.GCs.Count(g => g.Reason == GCReason.Induced)},
            {  "total allocated (mb)", (gc) => gc.Stats.TotalAllocatedMB },
            {  "max size peak (mb)", (gc) => gc.Stats.MaxSizePeakMB },
            {  "total pause time (msec)", (gc) => gc.Stats.TotalPauseTimeMSec },
            {  "gc pause time %", (gc) => gc.Stats.GetGCPauseTimePercentage() },
            {  "avg. heap size (mb)", (gc) => API.GoodLinq.Average(gc.GCs, g => g.HeapSizeBeforeMB) },
            {  "avg. heap size after (mb)", (gc) => API.GoodLinq.Average(gc.GCs, g => g.HeapSizeAfterMB) },
        };

        public MicrobenchmarkResult(string benchmarkFullName,
                                    Run parent,
                                    Benchmark benchmark,
                                    API.GCProcessData? gcData = null,
                                    GCTraceMetrics? gcTraceMetrics = null,
                                    API.CPUProcessData? cpuData = null,
                                    IEnumerable<string>? additionalReportMetrics = null,
                                    IEnumerable<string>? columns = null,
                                    IEnumerable<string>? cpuColumns = null)
        {
            MicrobenchmarkName = benchmarkFullName;
            Parent = parent;
            Statistics = benchmark.Statistics;
            GCTraceMetrics = gcTraceMetrics;
            CPUData = cpuData;

            if (additionalReportMetrics != null)
            {
                var additionalReportMetricsHashSet = new HashSet<string>(additionalReportMetrics, StringComparer.OrdinalIgnoreCase);
                foreach (var metric in benchmark.Metrics)
                {
                    if (!additionalReportMetricsHashSet.Contains(metric.Descriptor.Id))
                    {
                        continue;
                    }
                    OtherMetrics[metric.Descriptor.Id] = metric.Value;
                }
            }

            if (columns != null)
            {
                foreach (var column in columns)
                {
                    if (CustomStatisticsCalculationMap.Keys.Contains(column))
                    {
                        OtherMetrics[column] = CustomStatisticsCalculationMap[column](benchmark.Statistics);
                    }

                    if (CustomAggregateCalculationMap.Keys.Contains(column) && gcData != null)
                    {
                        OtherMetrics[column] = CustomAggregateCalculationMap[column](gcData);
                    }
                }
            }
        }
        public string MicrobenchmarkName { get; set; }
        public Run Parent { get; set; }
        public Statistics Statistics { get; set; }
        public GCTraceMetrics? GCTraceMetrics { get; set; }
        public Dictionary<string, double> OtherMetrics { get; set; } = new();
        public API.CPUProcessData? CPUData { get; set; }
    }
}