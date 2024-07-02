using GC.Infrastructure.Core.Presentation.GCPerfSim;

namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    // Per Microbenchmark result.
    public sealed class MicrobenchmarkComparisonResult
    {
        public MicrobenchmarkComparisonResult() { }

        public MicrobenchmarkComparisonResult(MicrobenchmarkResult baseline, MicrobenchmarkResult comparand)
        {
            Baseline = baseline;
            Comparand = comparand;
            var result = new ResultItemComparison(baseline.ResultItem, comparand.ResultItem);
            ComparisonResults = new();
            ComparisonResults.Add(result.GetComparison("PctTimePausedInGC"));
            ComparisonResults.Add(result.GetComparison("ExecutionTimeMSec"));
            ComparisonResults.Add(result.GetComparison("PauseDurationMSec_MeanWhereIsEphemeral"));
            ComparisonResults.Add(result.GetComparison("PauseDurationMSec_MeanWhereIsBackground"));
            ComparisonResults.Add(result.GetComparison("PauseDurationMSec_MeanWhereIsBlockingGen2"));
        }

        public MicrobenchmarkResult Baseline { get; set; }
        public MicrobenchmarkResult Comparand { get; set; }
        public List<ComparisonResult> ComparisonResults { get; set; }

        // TODO: Nullable double check.
        public string BaselineRunName => Baseline?.Parent?.Name;
        public string ComparandRunName => Comparand?.Parent?.Name;
        public string MicrobenchmarkName => Baseline.MicrobenchmarkName;

        public double MeanDiff => (Comparand.Statistics?.Mean.Value - Baseline.Statistics?.Mean.Value) ?? double.NaN;
        public double MeanDiffPerc => (MeanDiff / Baseline.Statistics?.Mean.Value) * 100 ?? double.NaN;

        public double? GetDiffPercentFromOtherMetrics(string metric)
        {
            if (!Baseline.OtherMetrics.TryGetValue(metric, out var baselineMetric))
            {
                return null;
            }

            if (!baselineMetric.HasValue)
            {
                return null;
            }

            if (!Comparand.OtherMetrics.TryGetValue(metric, out var comparandMetric))
            {
                return null;
            }

            if (!comparandMetric.HasValue)
            {
                return null;
            }

            return (comparandMetric.Value - baselineMetric.Value) / baselineMetric.Value;
        }
    }
}
