namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    [Serializable]
    // All Microbenchmarks.
    public sealed class MicrobenchmarkComparisonResults
    {
        public MicrobenchmarkComparisonResults(string baselineName, string runName, IEnumerable<MicrobenchmarkComparisonResult> comparisons)
        {
            Comparisons = comparisons;
            BaselineName = baselineName;
            RunName = runName;
        }

        public string BaselineName { get; }
        public string RunName { get; }
        public string MarkdownIdentifier => $"#{BaselineName.ToLowerInvariant()}-vs-{RunName.ToLowerInvariant()}";

        public IEnumerable<MicrobenchmarkComparisonResult> Comparisons { get; }
        public IEnumerable<MicrobenchmarkComparisonResult> Ordered => Comparisons.Where(o => !double.IsNaN(o.MeanDiff)).OrderByDescending(m => m.MeanDiffPerc);
        public IEnumerable<MicrobenchmarkComparisonResult> LargeRegressions => Ordered.Where(o => o.MeanDiffPerc > 20);
        public IEnumerable<MicrobenchmarkComparisonResult> LargeImprovements => Ordered.Where(o => o.MeanDiffPerc < -20).OrderBy(g => g.MeanDiffPerc);
        public IEnumerable<MicrobenchmarkComparisonResult> Regressions => Ordered.Where(o => o.MeanDiffPerc < 20 && o.MeanDiffPerc > 5);
        public IEnumerable<MicrobenchmarkComparisonResult> Improvements => Ordered.Where(o => o.MeanDiffPerc > -20 && o.MeanDiffPerc < -5).OrderBy(g => g.MeanDiffPerc);
        public IEnumerable<MicrobenchmarkComparisonResult> StaleRegressions => Ordered.Where((o => o.MeanDiffPerc > 0 && o.MeanDiffPerc < 5)).OrderByDescending(g => g.MeanDiffPerc);
        public IEnumerable<MicrobenchmarkComparisonResult> StaleImprovements => Ordered.Where((o => o.MeanDiffPerc < 0 && o.MeanDiffPerc > -5)).OrderBy(g => g.MeanDiffPerc);
    }
}
