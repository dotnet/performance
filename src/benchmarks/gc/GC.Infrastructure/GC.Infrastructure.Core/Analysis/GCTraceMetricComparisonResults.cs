using GC.Infrastructure.Core.Configurations;

namespace GC.Infrastructure.Core.Analysis
{
    [Serializable]
    public sealed class GCTraceMetricComparisonResults
    {
        public GCTraceMetricComparisonResults(string runName,
                                              IEnumerable<GCTraceMetricComparisonResult> comparisonResults)
        {
            RunName = runName;
            ComparisonResults = comparisonResults;
        }

        public string RunName { get; }
        public IEnumerable<GCTraceMetricComparisonResult> ComparisonResults { get; }
        public IEnumerable<GCTraceMetricComparisonResult> Ordered => ComparisonResults
            .Where(c => !double.IsNaN(c.PercentageDelta))
            .OrderByDescending(c => c.PercentageDelta);
        public IEnumerable<GCTraceMetricComparisonResult> LargeRegressions => Ordered
            .Where(c => c.PercentageDelta >= 20);
        public IEnumerable<GCTraceMetricComparisonResult> LargeImprovements => Ordered
            .Where(c => c.PercentageDelta <= -20)
            .OrderBy(g => g.PercentageDelta);
        public IEnumerable<GCTraceMetricComparisonResult> Regressions => Ordered
            .Where(c => c.PercentageDelta >= 5 && c.PercentageDelta < 20);
        public IEnumerable<GCTraceMetricComparisonResult> Improvements => Ordered
            .Where(c => c.PercentageDelta <= -5 && c.PercentageDelta > -20)
            .OrderBy(g => g.PercentageDelta);
        public IEnumerable<GCTraceMetricComparisonResult> StaleRegressions => Ordered
            .Where(c => c.PercentageDelta > 0 && c.PercentageDelta < 5);
        public IEnumerable<GCTraceMetricComparisonResult> StaleImprovements => Ordered
            .Where(c => c.PercentageDelta <= 0 && c.PercentageDelta > -5)
            .OrderBy(g => g.PercentageDelta);
    }
}
