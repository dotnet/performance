namespace GC.Infrastructure.Core.Analysis
{
    /// <summary>
    /// Encapsulates whether a higher or lower value is better for a given metric so that
    /// regression / improvement classification stays correct regardless of metric direction.
    /// By default a metric is treated as "lower is better" (e.g. pause times, heap sizes, GC counts).
    /// </summary>
    public static class MetricDirection
    {
        // Metrics for which a higher value is an improvement rather than a regression.
        private static readonly HashSet<string> s_higherIsBetterMetrics = new(StringComparer.OrdinalIgnoreCase)
        {
            "Speed_MBPerMSec",
        };

        public static bool IsHigherBetter(string metricName) => s_higherIsBetterMetrics.Contains(metricName);

        /// <summary>
        /// Returns a regression-oriented percentage delta where a positive value always means a
        /// regression and a negative value always means an improvement, irrespective of whether the
        /// metric is higher-is-better or lower-is-better. The original <paramref name="percentageDelta"/>
        /// is still used for display; only classification uses this value.
        /// </summary>
        public static double GetRegressionDelta(string metricName, double percentageDelta)
            => IsHigherBetter(metricName) ? -percentageDelta : percentageDelta;
    }
}
