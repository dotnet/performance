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
            Ordered = ComparisonResults
                .Where(c => !double.IsNaN(c.PercentageDelta))
                .OrderByDescending(c => c.PercentageDelta);

            LargeRegressions = Ordered
                .Where(c => c.PercentageDelta >= 20);
            LargeImprovements = Ordered
                .Where(c => c.PercentageDelta <= -20)
                .OrderBy(g => g.PercentageDelta);
            Regressions = Ordered
                .Where(c => c.PercentageDelta >= 5 && c.PercentageDelta < 20);
            Improvements = Ordered
                .Where(c => c.PercentageDelta <= -5 && c.PercentageDelta > -20)
                .OrderBy(g => g.PercentageDelta);
            StaleRegressions = Ordered
                .Where(c => c.PercentageDelta > 0 && c.PercentageDelta < 5);
            StaleImprovements = Ordered
                .Where(c => c.PercentageDelta <= 0 && c.PercentageDelta > -5)
                .OrderBy(g => g.PercentageDelta);
        }

        public string RunName { get; }
        public IEnumerable<GCTraceMetricComparisonResult> ComparisonResults { get; }
        public IEnumerable<GCTraceMetricComparisonResult> Ordered { get; }
        public IEnumerable<GCTraceMetricComparisonResult> LargeRegressions { get; }
        public IEnumerable<GCTraceMetricComparisonResult> LargeImprovements { get; }
        public IEnumerable<GCTraceMetricComparisonResult> Regressions { get; }
        public IEnumerable<GCTraceMetricComparisonResult> Improvements { get; }
        public IEnumerable<GCTraceMetricComparisonResult> StaleRegressions { get; }
        public IEnumerable<GCTraceMetricComparisonResult> StaleImprovements { get; }
    }
}
