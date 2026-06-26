namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    [Serializable]
    // All Microbenchmarks.
    public sealed class MicrobenchmarkComparisonResults
    {
        public MicrobenchmarkComparisonResults(string baselineName, string runName, IReadOnlyCollection<MicrobenchmarkComparisonResult> comparisons)
        {
            Comparisons = comparisons;
            BaselineName = baselineName;
            RunName = runName;
        }

        public string BaselineName { get; }
        public string RunName { get; }
        public string MarkdownIdentifier => $"#{BaselineName.ToLowerInvariant()}-vs-{RunName.ToLowerInvariant()}";

        public IReadOnlyCollection<MicrobenchmarkComparisonResult> Comparisons { get; }
        public IReadOnlyCollection<MicrobenchmarkComparisonResult> Ordered => Comparisons.Where(o => !double.IsNaN(o.MeanDiff)).OrderByDescending(m => m.MeanDiffPerc).ToList();
        public IReadOnlyCollection<MicrobenchmarkComparisonResult> LargeRegressions => Ordered.Where(o => o.MeanDiffPerc >= 20).ToList();
        public IReadOnlyCollection<MicrobenchmarkComparisonResult> LargeImprovements => Ordered.Where(o => o.MeanDiffPerc <= -20).OrderBy(g => g.MeanDiffPerc).ToList();
        public IReadOnlyCollection<MicrobenchmarkComparisonResult> Regressions => Ordered.Where(o => o.MeanDiffPerc < 20 && o.MeanDiffPerc >= 5).ToList();
        public IReadOnlyCollection<MicrobenchmarkComparisonResult> Improvements => Ordered.Where(o => o.MeanDiffPerc > -20 && o.MeanDiffPerc <= -5).OrderBy(g => g.MeanDiffPerc).ToList();
        public IReadOnlyCollection<MicrobenchmarkComparisonResult> StaleRegressions => Ordered.Where((o => o.MeanDiffPerc > 0 && o.MeanDiffPerc < 5)).ToList();
        public IReadOnlyCollection<MicrobenchmarkComparisonResult> StaleImprovements => Ordered.Where((o => o.MeanDiffPerc <= 0 && o.MeanDiffPerc > -5)).OrderBy(g => g.MeanDiffPerc).ToList();
    }
}
