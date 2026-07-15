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
                .OrderByDescending(c => c.RegressionPercentageDelta);

            LargeRegressions = Ordered
                .Where(c => c.RegressionPercentageDelta >= 20);
            LargeImprovements = Ordered
                .Where(c => c.RegressionPercentageDelta <= -20)
                .OrderBy(g => g.RegressionPercentageDelta);
            Regressions = Ordered
                .Where(c => c.RegressionPercentageDelta >= 5 && c.RegressionPercentageDelta < 20);
            Improvements = Ordered
                .Where(c => c.RegressionPercentageDelta <= -5 && c.RegressionPercentageDelta > -20)
                .OrderBy(g => g.RegressionPercentageDelta);
            StaleRegressions = Ordered
                .Where(c => c.RegressionPercentageDelta > 0 && c.RegressionPercentageDelta < 5);
            StaleImprovements = Ordered
                .Where(c => c.RegressionPercentageDelta <= 0 && c.RegressionPercentageDelta > -5)
                .OrderBy(g => g.RegressionPercentageDelta);
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
