namespace GC.Infrastructure.Core.Analysis
{
    public static class GCTraceMetricComparison
    {
        public static GCTraceMetricComparisonResult CompareGCTraceMetric(IEnumerable<GCTraceMetrics> baselines, IEnumerable<GCTraceMetrics> comparands,string nameOfMetric)
            => new GCTraceMetricComparisonResult(baselines, comparands, nameOfMetric);
    }
}
