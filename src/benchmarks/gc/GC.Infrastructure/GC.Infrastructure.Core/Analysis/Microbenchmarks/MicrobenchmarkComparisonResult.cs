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

        public MicrobenchmarkComparisonResult(IEnumerable<MicrobenchmarkResult> baselines, IEnumerable<MicrobenchmarkResult> comparands, bool includeTraces = true)
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

            OriginalBaselineOtherMetrics = Baselines
                .Select(baseline => baseline.OtherMetrics)
                .SelectMany(kvp => kvp)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Value).ToArray());

            OriginalComparandOtherMetrics = Comparands
                .Select(comparand => comparand.OtherMetrics)
                .SelectMany(kvp => kvp)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Value).ToArray());
        }

        public List<GCTraceMetricComparisonResult> ComparisonResults { get; set; }
        public string BaselineRunName { get; }
        public string ComparandRunName { get; }
        public string ComparisonName => $"{ComparandRunName} vs {BaselineRunName}";
        public string MicrobenchmarkName { get; }
        public IEnumerable<MicrobenchmarkResult> Baselines { get; }
        public IEnumerable<MicrobenchmarkResult> Comparands { get; }

        public double[] OutliersFreeBaselineMeanValueCollection => 
            API.Statistics.RemoveOutliers(Baselines
                .Select(baseline => baseline.Statistics?.Mean ?? double.NaN))
                .ToArray();
        public double[] OutliersFreeComparandMeanValueCollection =>
            API.Statistics.RemoveOutliers(Comparands
                .Select(comparand => comparand.Statistics?.Mean ?? double.NaN))
                .ToArray();

        public double AveragedBaselineMeanValue => API.GoodLinq.Average(OutliersFreeBaselineMeanValueCollection, r => r);
        public double AveragedComparandMeanValue => API.GoodLinq.Average(OutliersFreeComparandMeanValueCollection, r => r);

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

        public Dictionary<string, double[]> OriginalBaselineOtherMetrics { get; } = new();
        public Dictionary<string, double[]> OriginalComparandOtherMetrics { get; } = new();
        public Dictionary<string, double[]> OutliersFreeBaselineOtherMetrics => OriginalBaselineOtherMetrics
            .Select(kvp => (kvp.Key, API.Statistics.RemoveOutliers(kvp.Value).ToArray()))
            .ToDictionary(x => x.Item1, x => x.Item2);
        public Dictionary<string, double[]> OutliersFreeComparandOtherMetrics => OriginalComparandOtherMetrics
            .Select(kvp => (kvp.Key, API.Statistics.RemoveOutliers(kvp.Value).ToArray()))
            .ToDictionary(x => x.Item1, x => x.Item2);
        public Dictionary<string, double> AveragedBaselineOtherMetrics => OutliersFreeBaselineOtherMetrics
            .Select(kvp => (kvp.Key, API.GoodLinq.Average(kvp.Value, v => v)))
            .ToDictionary(x => x.Item1, x => x.Item2);
        public Dictionary<string, double> AveragedComparandOtherMetrics => OutliersFreeComparandOtherMetrics
            .Select(kvp => (kvp.Key, API.GoodLinq.Average(kvp.Value, v => v)))
            .ToDictionary(x => x.Item1, x => x.Item2);

        public Dictionary<string, double> OtherMetricsDiff => AveragedBaselineOtherMetrics
            .Select(kvp =>
            {
                if (AveragedComparandOtherMetrics.ContainsKey(kvp.Key))
                {
                    return (kvp.Key, AveragedComparandOtherMetrics[kvp.Key] - AveragedBaselineOtherMetrics[kvp.Key]);
                }
                return (kvp.Key, double.NaN);
            })
            .ToDictionary(x => x.Item1, x => x.Item2);

        public Dictionary<string, double> OtherMetricsDiffPerc => AveragedBaselineOtherMetrics
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
}
