using GC.Analysis.API;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    // Per Microbenchmark result.
    public sealed class MicrobenchmarkComparisonResult
    {
        public static readonly string[] RequiredMetrics = new string[]
        {
            "PctTimePausedInGC",
            "ExecutionTimeMSec",
            "PauseDurationMSec_MeanWhereIsEphemeral",
            "PauseDurationMSec_MeanWhereIsBackground",
            "PauseDurationMSec_MeanWhereIsBlockingGen2"
        };

        public static readonly IReadOnlyDictionary<string, Func<Statistics, double?>> CustomStatisticsCalculationMap = new Dictionary<string, Func<Statistics, double?>>(StringComparer.OrdinalIgnoreCase)
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

        public static readonly Dictionary<string, Func<GCProcessData, double>> CustomAggregateCalculationMap = new Dictionary<string, Func<GCProcessData, double>>(StringComparer.OrdinalIgnoreCase)
        {
            {  "gc count", (gc) => gc.Stats.Count },
            {  "non induced gc count", (gc) => gc.Stats.Count - gc.GCs.Count(g => g.Reason == GCReason.Induced)},
            {  "induced gc count", (gc) => gc.GCs.Count(g => g.Reason == GCReason.Induced)},
            {  "total allocated (mb)", (gc) => gc.Stats.TotalAllocatedMB },
            {  "max size peak (mb)", (gc) => gc.Stats.MaxSizePeakMB },
            {  "total pause time (msec)", (gc) => gc.Stats.TotalPauseTimeMSec },
            {  "gc pause time %", (gc) => gc.Stats.GetGCPauseTimePercentage() },
            {  "avg. heap size (mb)", (gc) => GoodLinq.Average(gc.GCs, g => g.HeapSizeBeforeMB) },
            {  "avg. heap size after (mb)", (gc) => GoodLinq.Average(gc.GCs, g => g.HeapSizeAfterMB) },
        };

        public MicrobenchmarkComparisonResult() { }

        public MicrobenchmarkComparisonResult(IEnumerable<MicrobenchmarkResult> baselines, IEnumerable<MicrobenchmarkResult> comparands, bool includeTraces = true)
        {
            ComparisonResults = new();
            if (includeTraces)
            {
                var baselineGCTraceMetricsCollection = GoodLinq.Select(baselines, baseline => baseline.GCTraceMetrics);
                var comparandGCTraceMetricsCollection = GoodLinq.Select(comparands, comparand => comparand.GCTraceMetrics);

                foreach (var metricName in RequiredMetrics)
                {
                    ComparisonResults.Add(
                        GCTraceMetricComparison.CompareGCTraceMetric(baselineGCTraceMetricsCollection, comparandGCTraceMetricsCollection, metricName));
                }

                foreach (var kvp in CustomAggregateCalculationMap)
                {
                    string customGCData = kvp.Key;
                    Func<GCProcessData, double> calculation = kvp.Value;
                    OriginalBaselineCustomGCData[customGCData] = Array.Empty<double>();
                    OriginalComparandCustomGCData[customGCData] = Array.Empty<double>();

                    OriginalBaselineCustomGCData[customGCData] =
                        GoodLinq.Select(baselines, baseline => calculation(baseline.GCData)).ToArray();
                    OriginalComparandCustomGCData[customGCData] =
                        GoodLinq.Select(comparands, comparand => calculation(comparand.GCData)).ToArray();
                }
            }

            BaselineRunName = baselines?.FirstOrDefault()?.Parent?.Name;
            ComparandRunName = comparands?.FirstOrDefault()?.Parent?.Name;
            MicrobenchmarkName = baselines?.FirstOrDefault()?.MicrobenchmarkName;

            OriginalBaselineMeanValueCollection =
                GoodLinq.Select(baselines, baseline => baseline.Statistics?.Mean ?? double.NaN).ToArray();
            OriginalComparandMeanValueCollection =
                GoodLinq.Select(comparands, comparand => comparand.Statistics?.Mean ?? double.NaN).ToArray();

            foreach (var kvp in CustomStatisticsCalculationMap)
            {
                string customStatistics = kvp.Key;
                Func<Statistics, double?> calculation = kvp.Value;
                OriginalBaselineCustomStatistics[customStatistics] = Array.Empty<double>();
                OriginalComparandCustomStatistics[customStatistics] = Array.Empty<double>();

                OriginalBaselineCustomStatistics[customStatistics] = 
                    GoodLinq.Select(baselines, baseline => calculation(baseline.Statistics) ?? double.NaN).ToArray();
                OriginalComparandCustomStatistics[customStatistics] = 
                    GoodLinq.Select(comparands, comparand => calculation(comparand.Statistics) ?? double.NaN).ToArray();
            }
        }

        public List<GCTraceMetricComparisonResult> ComparisonResults { get; set; }
        public string BaselineRunName { get; }
        public string ComparandRunName { get; }
        public string ComparisonName => $"{ComparandRunName} vs {BaselineRunName}";
        public string MicrobenchmarkName { get; }
        public double[] OriginalBaselineMeanValueCollection { get; }
        public double[] OriginalComparandMeanValueCollection { get; }

        public double[] OutliersFreeBaselineMeanValueCollection => 
            GC.Analysis.API.Statistics.RemoveOutliers(OriginalBaselineMeanValueCollection).ToArray();
        public double[] OutliersFreeComparandMeanValueCollection =>
            GC.Analysis.API.Statistics.RemoveOutliers(OriginalComparandMeanValueCollection).ToArray();

        public double AveragedBaselineMeanValue => GoodLinq.Average(OutliersFreeBaselineMeanValueCollection, r => r);
        public double AveragedComparandMeanValue => GoodLinq.Average(OutliersFreeComparandMeanValueCollection, r => r);

        public double MeanDiff => AveragedComparandMeanValue - AveragedBaselineMeanValue;
        public double MeanDiffPerc{
            get
            {
                if (AveragedBaselineMeanValue == 0)
                {
                    if (AveragedComparandMeanValue == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return double.NaN;
                    }
                }
                return (MeanDiff / AveragedBaselineMeanValue) * 100;
            }
        }

        public Dictionary<string, double[]> OriginalBaselineCustomStatistics { get; } = new();
        public Dictionary<string, double[]> OriginalComparandCustomStatistics { get; } = new();
        public Dictionary<string, double[]> OutliersFreeBaselineCustomStatistics =>
            GoodLinq.Select(OriginalBaselineCustomStatistics, kvp =>
                (kvp.Key, GC.Analysis.API.Statistics.RemoveOutliers(kvp.Value).ToArray()))
            .ToDictionary();
        public Dictionary<string, double[]> OutliersFreeComparandCustomStatistics =>
            GoodLinq.Select(OriginalComparandCustomStatistics, kvp =>
                (kvp.Key, GC.Analysis.API.Statistics.RemoveOutliers(kvp.Value).ToArray()))
            .ToDictionary();
        public Dictionary<string, double> AveragedBaselineCustomStatistics =>
            GoodLinq.Select(OutliersFreeBaselineCustomStatistics, kvp =>
                (kvp.Key, GoodLinq.Average(kvp.Value, v => v)))
            .ToDictionary();
        public Dictionary<string, double> AveragedComparandCustomStatistics =>
            GoodLinq.Select(OutliersFreeComparandCustomStatistics, kvp =>
                (kvp.Key, GoodLinq.Average(kvp.Value, v => v)))
            .ToDictionary();

        public Dictionary<string, double> CustomStatisticsDiff =>
            GoodLinq.Select(OutliersFreeBaselineCustomStatistics, kvp =>
                (kvp.Key, AveragedComparandCustomStatistics[kvp.Key] - AveragedBaselineCustomStatistics[kvp.Key]))
            .ToDictionary();

        public Dictionary<string, double> CustomStatisticsDiffPerc =>
            GoodLinq.Select(OutliersFreeBaselineCustomStatistics, kvp =>
            {
                if (AveragedBaselineCustomStatistics[kvp.Key] == 0)
                {
                    if (AveragedComparandCustomStatistics[kvp.Key] == 0)
                    {
                        return (kvp.Key, 0);
                    }
                    else
                    {
                        return (kvp.Key, double.NaN);
                    }
                }
                return (kvp.Key, CustomStatisticsDiff[kvp.Key] / AveragedBaselineCustomStatistics[kvp.Key]);
            })
            .ToDictionary();

        public Dictionary<string, double[]> OriginalBaselineCustomGCData { get; } = new();
        public Dictionary<string, double[]> OriginalComparandCustomGCData { get; } = new();
        public Dictionary<string, double[]> OutliersFreeBaselineCustomGCData =>
            GoodLinq.Select(OriginalBaselineCustomGCData, kvp =>
                (kvp.Key, GC.Analysis.API.Statistics.RemoveOutliers(kvp.Value).ToArray()))
            .ToDictionary();
        public Dictionary<string, double[]> OutliersFreeComparandCustomGCData =>
            GoodLinq.Select(OriginalComparandCustomGCData, kvp =>
                (kvp.Key, GC.Analysis.API.Statistics.RemoveOutliers(kvp.Value).ToArray()))
            .ToDictionary();
        public Dictionary<string, double> AveragedBaselineCustomGCData =>
            GoodLinq.Select(OutliersFreeBaselineCustomGCData, kvp =>
                (kvp.Key, GoodLinq.Average(kvp.Value, v => v)))
            .ToDictionary();
        public Dictionary<string, double> AveragedComparandCustomGCData =>
            GoodLinq.Select(OutliersFreeComparandCustomGCData, kvp =>
                (kvp.Key, GoodLinq.Average(kvp.Value, v => v)))
            .ToDictionary();

        public Dictionary<string, double> CustomGCDataDiff =>
            GoodLinq.Select(OutliersFreeBaselineCustomGCData, kvp =>
                (kvp.Key, AveragedComparandCustomGCData[kvp.Key] - AveragedBaselineCustomGCData[kvp.Key]))
            .ToDictionary();

        public Dictionary<string, double> CustomGCDataDiffPerc =>
            GoodLinq.Select(OutliersFreeBaselineCustomGCData, kvp =>
            {
                if (AveragedBaselineCustomGCData[kvp.Key] == 0)
                {
                    if (AveragedComparandCustomGCData[kvp.Key] == 0)
                    {
                        return (kvp.Key, 0);
                    }
                    else
                    {
                        return (kvp.Key, double.NaN);
                    }
                }
                return (kvp.Key, CustomGCDataDiff[kvp.Key] / AveragedBaselineCustomGCData[kvp.Key]);
            })
            .ToDictionary();
    }
}
