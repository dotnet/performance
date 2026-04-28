using GC.Analysis.API;

namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    // Per Microbenchmark result.
    public sealed class MicrobenchmarkComparisonResult
    {
        public MicrobenchmarkComparisonResult() { }

        public MicrobenchmarkComparisonResult(IEnumerable<MicrobenchmarkResult> baselines, IEnumerable<MicrobenchmarkResult> comparands)
        {
            Baselines = baselines;
            Comparands = comparands;
            var baselineGCTraceMetricsCollection = GoodLinq.Select(Baselines, baseline => baseline.GCTraceMetrics);
            var comparandGCTraceMetricsCollection = GoodLinq.Select(Comparands, comparand => comparand.GCTraceMetrics);

            string[] metricNames = new string[]
            {
                "PctTimePausedInGC",
                "ExecutionTimeMSec",
                "PauseDurationMSec_MeanWhereIsEphemeral",
                "PauseDurationMSec_MeanWhereIsBackground",
                "PauseDurationMSec_MeanWhereIsBlockingGen2"
            };

            ComparisonResults = new();
            foreach (var metricName in metricNames)
            {
                ComparisonResults.Add(
                    GCTraceMetricComparison.CompareGCTraceMetric(baselineGCTraceMetricsCollection, comparandGCTraceMetricsCollection, metricName));
            }

            BaselineRunName = Baselines?.FirstOrDefault()?.Parent?.Name;
            ComparandRunName = Comparands?.FirstOrDefault()?.Parent?.Name;
            MicrobenchmarkName = Baselines?.FirstOrDefault()?.MicrobenchmarkName;

            OriginalBaselineMeanValueCollection =
                GoodLinq.Select(Baselines, baseline => baseline.Statistics?.Mean ?? double.NaN).ToArray();
            OriginalComparandMeanValueCollection =
                GoodLinq.Select(Comparands, comparand => comparand.Statistics?.Mean ?? double.NaN).ToArray();
        }

        public IEnumerable<MicrobenchmarkResult> Baselines { get; set; }
        public IEnumerable<MicrobenchmarkResult> Comparands { get; set; }
        public List<GCTraceMetricComparisonResult> ComparisonResults { get; set; }
        public string BaselineRunName { get; }
        public string ComparandRunName { get; }
        public string MicrobenchmarkName { get; }
        public double[] OriginalBaselineMeanValueCollection { get; }
        public double[] OriginalComparandMeanValueCollection { get; }

        public double[] OutliersFreeBaselineMeanValueCollection => 
            GC.Analysis.API.Statistics.RemoveOutliers(OriginalBaselineMeanValueCollection).ToArray();
        public double[] OutliersFreeComparandMeanValueCollection =>
            GC.Analysis.API.Statistics.RemoveOutliers(OriginalComparandMeanValueCollection).ToArray();

        public double MeanDiff => OutliersFreeComparandMeanValueCollection.Average() - OutliersFreeBaselineMeanValueCollection.Average();
        public double MeanDiffPerc => (MeanDiff / (Baselines.FirstOrDefault()?.Statistics?.Mean ?? double.NaN)) * 100;

        public double? GetDiffPercentFromOtherMetrics(string metricName)
        {
            List<double> baselineOtherMetricCollection = new();
            List<double> comparandOtherMetricCollection = new();

            foreach (var baseline in Baselines)
            {
                if (baseline.OtherMetrics.TryGetValue(metricName, out var baselineMetric))
                {
                    if (baselineMetric.HasValue)
                    {
                        baselineOtherMetricCollection.Add(baselineMetric.Value);
                    }
                }
            }

            foreach (var comparand in Comparands)
            {
                if (comparand.OtherMetrics.TryGetValue(metricName, out var comparandMetric))
                {
                    if (comparandMetric.HasValue)
                    {
                        comparandOtherMetricCollection.Add(comparandMetric.Value);
                    }
                }
            }

            if (baselineOtherMetricCollection.Count() * comparandOtherMetricCollection.Count() == 0)
            {
                return null;
            }

            var outliersFreeBaselineOtherMetricCollection = GC.Analysis.API.Statistics.RemoveOutliers(baselineOtherMetricCollection);
            var outliersFreeComparandOtherMetricCollection = GC.Analysis.API.Statistics.RemoveOutliers(comparandOtherMetricCollection);

            var averagedOutliersFreeBaselineOtherMetric = outliersFreeBaselineOtherMetricCollection.Average();
            var averagedOutliersFreeComparandOtherMetric = outliersFreeComparandOtherMetricCollection.Average();
            return (averagedOutliersFreeBaselineOtherMetric - averagedOutliersFreeComparandOtherMetric) / averagedOutliersFreeBaselineOtherMetric;
        }
    }
}
