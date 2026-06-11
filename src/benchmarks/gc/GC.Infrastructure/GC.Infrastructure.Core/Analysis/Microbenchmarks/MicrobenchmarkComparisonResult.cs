using API = GC.Analysis.API;

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

        public MicrobenchmarkComparisonResult(IReadOnlyCollection<MicrobenchmarkResult> baselines, IReadOnlyCollection<MicrobenchmarkResult> comparands, bool includeTraces = true)
        {
            ComparisonResults = new();

            if (includeTraces)
            {
                var baselineGCTraceMetricsCollection = baselines
                    .Where(baseline => baseline != null)
                    .Where(baseline => baseline.GCTraceMetrics != null)
                    .Select(baseline => baseline.GCTraceMetrics)
                    .ToArray();

                var comparandGCTraceMetricsCollection = comparands
                    .Where(comparand => comparand != null)
                    .Where(comparand => comparand.GCTraceMetrics != null)
                    .Select(comparand => comparand.GCTraceMetrics)
                    .ToArray();

                if (baselineGCTraceMetricsCollection.Length > 0 && comparandGCTraceMetricsCollection.Length > 0)
                {
                    foreach (var metricName in RequiredMetrics)
                    {
                        ComparisonResults.Add(
                            GCTraceMetricComparison.CompareGCTraceMetric(
                                baselineGCTraceMetricsCollection!, comparandGCTraceMetricsCollection!, metricName));
                    }
                       
                }
            }

            var firstBaseline = baselines?.FirstOrDefault();
            var firstComparand = comparands?.FirstOrDefault();

            BaselineRunName = firstBaseline?.Parent?.Name ?? string.Empty;
            ComparandRunName = firstComparand?.Parent?.Name ?? string.Empty;
            MicrobenchmarkName = firstBaseline?.MicrobenchmarkName ?? string.Empty;

            Baselines = baselines ?? new List<MicrobenchmarkResult>();
            Comparands = comparands ?? new List<MicrobenchmarkResult>();

            // Mean Value comparisons
            OriginalBaselineMeanValueCollection = Baselines
                .Select(baseline => baseline.Statistics?.Mean ?? double.NaN)
                .ToArray();
            OriginalAveragedBaselineMeanValue = API.GoodLinq.Average(OriginalBaselineMeanValueCollection, r => r);
            OutliersFreeBaselineMeanValueCollection = API.Statistics.RemoveOutliers(OriginalBaselineMeanValueCollection).ToArray();
            AveragedBaselineMeanValue = API.GoodLinq.Average(OutliersFreeBaselineMeanValueCollection, r => r);

            OriginalComparandMeanValueCollection = Comparands
                .Select(comparand => comparand.Statistics?.Mean ?? double.NaN)
                .ToArray();
            OriginalAveragedComparandMeanValue = API.GoodLinq.Average(OriginalComparandMeanValueCollection, r => r);
            OutliersFreeComparandMeanValueCollection = API.Statistics.RemoveOutliers(OriginalComparandMeanValueCollection).ToArray();
            AveragedComparandMeanValue = API.GoodLinq.Average(OutliersFreeComparandMeanValueCollection, r => r);

            OriginalMeanDiff = OriginalAveragedComparandMeanValue - OriginalAveragedBaselineMeanValue;
            MeanDiff = AveragedComparandMeanValue - AveragedBaselineMeanValue;

            if (OriginalAveragedBaselineMeanValue == 0)
            {
                if (OriginalAveragedComparandMeanValue == 0)
                {
                    OriginalMeanDiffPerc = 0;
                }
                else
                {
                    OriginalMeanDiffPerc = double.NaN;
                }
            }
            else
            {
                OriginalMeanDiffPerc = (OriginalMeanDiff / OriginalAveragedBaselineMeanValue) * 100;
            }

            if (AveragedBaselineMeanValue == 0)
            {
                if (AveragedComparandMeanValue == 0)
                {
                    MeanDiffPerc = 0;
                }
                else
                {
                    MeanDiffPerc = double.NaN;
                }
            }
            else
            {
                MeanDiffPerc = (MeanDiff / AveragedBaselineMeanValue) * 100;
            }

            // Other metrics comparisons
            OriginalBaselineOtherMetrics = new();
            foreach (var baseline in Baselines)
            {
                var otherMetrics = baseline.OtherMetrics;
                foreach (var kvp in otherMetrics)
                {
                    OriginalBaselineOtherMetrics[kvp.Key] = OriginalBaselineOtherMetrics.GetValueOrDefault(kvp.Key, new());
                    OriginalBaselineOtherMetrics[kvp.Key].Add(kvp.Value);
                }
            }
            OriginalAveragedBaselineOtherMetrics = OriginalBaselineOtherMetrics
                .Select(kvp => (kvp.Key, API.GoodLinq.Average(kvp.Value, v => v)))
                .ToDictionary(x => x.Item1, x => x.Item2);
            OutliersFreeBaselineOtherMetrics = OriginalBaselineOtherMetrics
                .Select(kvp => (kvp.Key, API.Statistics.RemoveOutliers(kvp.Value).ToArray()))
                .ToDictionary(x => x.Item1, x => x.Item2);
            AveragedBaselineOtherMetrics = OutliersFreeBaselineOtherMetrics
                .Select(kvp => (kvp.Key, API.GoodLinq.Average(kvp.Value, v => v)))
                .ToDictionary(x => x.Item1, x => x.Item2);

            OriginalComparandOtherMetrics = new();
            foreach (var comparand in Comparands)
            {
                var otherMetrics = comparand.OtherMetrics;
                foreach (var kvp in otherMetrics)
                {
                    OriginalComparandOtherMetrics[kvp.Key] = OriginalComparandOtherMetrics.GetValueOrDefault(kvp.Key, new());
                    OriginalComparandOtherMetrics[kvp.Key].Add(kvp.Value);
                }
            }
            OriginalAveragedComparandOtherMetrics = OriginalComparandOtherMetrics
                .Select(kvp => (kvp.Key, API.GoodLinq.Average(kvp.Value, v => v)))
                .ToDictionary(x => x.Item1, x => x.Item2);
            OutliersFreeComparandOtherMetrics = OriginalComparandOtherMetrics
                .Select(kvp => (kvp.Key, API.Statistics.RemoveOutliers(kvp.Value).ToArray()))
                .ToDictionary(x => x.Item1, x => x.Item2);
            AveragedComparandOtherMetrics = OutliersFreeComparandOtherMetrics
                .Select(kvp => (kvp.Key, API.GoodLinq.Average(kvp.Value, v => v)))
                .ToDictionary(x => x.Item1, x => x.Item2);

            OriginalOtherMetricsDiff = OriginalAveragedBaselineOtherMetrics
                .Select(kvp =>
                {
                    if (OriginalAveragedComparandOtherMetrics.ContainsKey(kvp.Key))
                    {
                        return (kvp.Key, OriginalAveragedComparandOtherMetrics[kvp.Key] - OriginalAveragedBaselineOtherMetrics[kvp.Key]);
                    }
                    return (kvp.Key, double.NaN);
                })
                .ToDictionary(x => x.Item1, x => x.Item2);
            OtherMetricsDiff = AveragedBaselineOtherMetrics
                .Select(kvp =>
                {
                    if (AveragedComparandOtherMetrics.ContainsKey(kvp.Key))
                    {
                        return (kvp.Key, AveragedComparandOtherMetrics[kvp.Key] - AveragedBaselineOtherMetrics[kvp.Key]);
                    }
                    return (kvp.Key, double.NaN);
                })
            .ToDictionary(x => x.Item1, x => x.Item2);

            OriginalOtherMetricsDiffPerc = OriginalAveragedBaselineOtherMetrics
                .Select(kvp =>
                {
                    if (!OriginalAveragedComparandOtherMetrics.ContainsKey(kvp.Key))
                    {
                        return (kvp.Key, double.NaN);
                    }
                    if (OriginalAveragedBaselineOtherMetrics[kvp.Key] == 0)
                    {
                        if (OriginalAveragedComparandOtherMetrics[kvp.Key] == 0)
                        {
                            return (kvp.Key, 0);
                        }
                        else
                        {
                            return (kvp.Key, double.NaN);
                        }
                    }

                    if (!OriginalOtherMetricsDiff.ContainsKey(kvp.Key))
                    {
                        return (kvp.Key, double.NaN);
                    }

                    if (double.IsNaN(OriginalOtherMetricsDiff[kvp.Key]))
                    {
                        return (kvp.Key, double.NaN);
                    }

                    return (kvp.Key, ((OriginalAveragedComparandOtherMetrics[kvp.Key] - OriginalAveragedBaselineOtherMetrics[kvp.Key]) / OriginalAveragedBaselineOtherMetrics[kvp.Key]) * 100);
                    
                })
                .ToDictionary(x => x.Item1, x => x.Item2);
            OtherMetricsDiffPerc = AveragedBaselineOtherMetrics
                .Select(kvp =>
                {
                    if (!AveragedComparandOtherMetrics.ContainsKey(kvp.Key))
                    {
                        return (kvp.Key, double.NaN);
                    }
                    if (AveragedBaselineOtherMetrics[kvp.Key] == 0)
                    {
                        if (AveragedComparandOtherMetrics[kvp.Key] == 0)
                        {
                            return (kvp.Key, 0);
                        }
                        else
                        {
                            return (kvp.Key, double.NaN);
                        }
                    }

                    if (!OtherMetricsDiff.ContainsKey(kvp.Key))
                    {
                        return (kvp.Key, double.NaN);
                    }

                    if (double.IsNaN(OtherMetricsDiff[kvp.Key]))
                    {
                        return (kvp.Key, double.NaN);
                    }

                    return (kvp.Key, 100 * OtherMetricsDiff[kvp.Key] / AveragedBaselineOtherMetrics[kvp.Key]);
                })
                .ToDictionary(x => x.Item1, x => x.Item2);
        }

        public List<GCTraceMetricComparisonResult> ComparisonResults { get; set; }
        public string BaselineRunName { get; }
        public string ComparandRunName { get; }
        public string ComparisonName => $"{ComparandRunName} vs {BaselineRunName}";
        public string MicrobenchmarkName { get; }
        public IEnumerable<MicrobenchmarkResult> Baselines { get; }
        public IEnumerable<MicrobenchmarkResult> Comparands { get; }
        // MeanValue comparisons
        public double[] OriginalBaselineMeanValueCollection { get; }
        public double[] OriginalComparandMeanValueCollection { get; }
        public double[] OutliersFreeBaselineMeanValueCollection { get; }
        public double[] OutliersFreeComparandMeanValueCollection { get; }
        public double OriginalAveragedBaselineMeanValue { get; }
        public double OriginalAveragedComparandMeanValue { get; }
        public double AveragedBaselineMeanValue { get; }
        public double AveragedComparandMeanValue { get; }
        public double OriginalMeanDiff { get; }
        public double MeanDiff { get; }
        public double OriginalMeanDiffPerc { get; }
        public double MeanDiffPerc { get; }
        // OtherMetrics comparisons
        public Dictionary<string, List<double>> OriginalBaselineOtherMetrics { get; }
        public Dictionary<string, List<double>> OriginalComparandOtherMetrics { get; }
        public Dictionary<string, double[]> OutliersFreeBaselineOtherMetrics { get; }
        public Dictionary<string, double[]> OutliersFreeComparandOtherMetrics { get; }
        public Dictionary<string, double> OriginalAveragedBaselineOtherMetrics { get; }
        public Dictionary<string, double> OriginalAveragedComparandOtherMetrics { get; }
        public Dictionary<string, double> AveragedBaselineOtherMetrics { get; }
        public Dictionary<string, double> AveragedComparandOtherMetrics { get; }
        public Dictionary<string, double> OriginalOtherMetricsDiff { get; }
        public Dictionary<string, double> OriginalOtherMetricsDiffPerc { get; }
        public Dictionary<string, double> OtherMetricsDiff { get; }
        public Dictionary<string, double> OtherMetricsDiffPerc { get; }
    }
}
