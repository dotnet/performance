using GC.Analysis.API;

namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    // Per Microbenchmark result.
    public sealed class MicrobenchmarkComparisonResult
    {
        public MicrobenchmarkComparisonResult() { }

        public MicrobenchmarkComparisonResult(IEnumerable<MicrobenchmarkResult> baselines, IEnumerable<MicrobenchmarkResult> comparands)
        {
            var baselineGCTraceMetricsCollection = GoodLinq.Select(baselines, baseline => baseline.GCTraceMetrics);
            var comparandGCTraceMetricsCollection = GoodLinq.Select(comparands, comparand => comparand.GCTraceMetrics);

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

            BaselineRunName = baselines?.FirstOrDefault()?.Parent?.Name;
            ComparandRunName = comparands?.FirstOrDefault()?.Parent?.Name;
            MicrobenchmarkName = baselines?.FirstOrDefault()?.MicrobenchmarkName;

            OriginalBaselineMeanValueCollection =
                GoodLinq.Select(baselines, baseline => baseline.Statistics?.Mean ?? double.NaN).ToArray();
            OriginalComparandMeanValueCollection =
                GoodLinq.Select(comparands, comparand => comparand.Statistics?.Mean ?? double.NaN).ToArray();
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

        //public double? GetDiffPercentFromOtherMetrics(string metricName)
        //{
        //    List<double> baselineOtherMetricCollection = new();
        //    List<double> comparandOtherMetricCollection = new();

        //    foreach (var baseline in Baselines)
        //    {
        //        if (baseline.OtherMetrics.TryGetValue(metricName, out var baselineMetric))
        //        {
        //            if (baselineMetric.HasValue)
        //            {
        //                baselineOtherMetricCollection.Add(baselineMetric.Value);
        //            }
        //        }
        //    }

        //    foreach (var comparand in Comparands)
        //    {
        //        if (comparand.OtherMetrics.TryGetValue(metricName, out var comparandMetric))
        //        {
        //            if (comparandMetric.HasValue)
        //            {
        //                comparandOtherMetricCollection.Add(comparandMetric.Value);
        //            }
        //        }
        //    }

        //    if (baselineOtherMetricCollection.Count() * comparandOtherMetricCollection.Count() == 0)
        //    {
        //        return null;
        //    }

        //    var outliersFreeBaselineOtherMetricCollection = GC.Analysis.API.Statistics.RemoveOutliers(baselineOtherMetricCollection);
        //    var outliersFreeComparandOtherMetricCollection = GC.Analysis.API.Statistics.RemoveOutliers(comparandOtherMetricCollection);

        //    var averagedOutliersFreeBaselineOtherMetric = outliersFreeBaselineOtherMetricCollection.Average();
        //    var averagedOutliersFreeComparandOtherMetric = outliersFreeComparandOtherMetricCollection.Average();
        //    return (averagedOutliersFreeBaselineOtherMetric - averagedOutliersFreeComparandOtherMetric) / averagedOutliersFreeBaselineOtherMetric;
        //}
    }
}
