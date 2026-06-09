using FluentAssertions;
using GC.Analysis.API;
using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Presentation.GCPerfSim;

namespace GC.Infrastructure.Core.UnitTests.GCPerfSim
{
    // Integration test that exercises the real GCPerfSim analysis pipeline end-to-end against
    // recorded GC traces: it loads the traces, computes the same ResultItem / GCTraceMetrics that
    // the `gcperfsim` and `gcperfsim-compare` commands produce, runs the comparison, and verifies
    // that the regression / improvement categorization is direction-aware (Speed_MBPerMSec is
    // higher-is-better) while the displayed delta keeps its real sign.
    [TestClass]
    public sealed class ComparisonCategorization
    {
        // The recorded traces capture a managed process named "Benchmarks" with real GC data.
        private const string ProcessName = "Benchmarks";

        private static string? FindTracesDirectory()
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir != null)
            {
                foreach (string relative in new[]
                {
                    Path.Combine("Notebooks", "Examples", "Traces"),
                    Path.Combine("src", "benchmarks", "gc", "GC.Infrastructure", "Notebooks", "Examples", "Traces"),
                })
                {
                    string candidate = Path.Combine(dir.FullName, relative);
                    if (File.Exists(Path.Combine(candidate, "CPU_Baseline.etl.zip")))
                    {
                        return candidate;
                    }
                }

                dir = dir.Parent;
            }

            return null;
        }

        [TestMethod]
        public void GCPerfSimRun_Comparison_CategorizesSpeedMetricByDirection()
        {
            string? tracesDirectory = FindTracesDirectory();
            if (tracesDirectory == null)
            {
                Assert.Inconclusive("Could not locate the example GC traces required for this test.");
                return;
            }

            string baselinePath = Path.Combine(tracesDirectory, "CPU_Baseline.etl.zip");
            string comparandPath = Path.Combine(tracesDirectory, "CPU_Comparand.etl.zip");

            using Analyzer baselineAnalyzer = AnalyzerManager.GetAnalyzer(baselinePath);
            using Analyzer comparandAnalyzer = AnalyzerManager.GetAnalyzer(comparandPath);

            GCProcessData? baselineData = baselineAnalyzer.GetProcessGCData(ProcessName).FirstOrDefault();
            GCProcessData? comparandData = comparandAnalyzer.GetProcessGCData(ProcessName).FirstOrDefault();

            if (baselineData == null || comparandData == null)
            {
                Assert.Inconclusive($"The traces did not contain GC data for the '{ProcessName}' process.");
                return;
            }

            // Ensure the GCPerfSim run actually produced GC data to analyze.
            baselineData.GCs.Count.Should().BeGreaterThan(0, "the baseline trace should contain GCs");
            comparandData.GCs.Count.Should().BeGreaterThan(0, "the comparand trace should contain GCs");

            // Build the same ResultItem the gcperfsim / gcperfsim-compare commands build.
            ResultItem baselineItem = new(baselineData, "Run", "baseline");
            ResultItem comparandItem = new(comparandData, "Run", "comparand");

            ResultItemComparison comparison = new(baselineItem, comparandItem);

            // --- Speed_MBPerMSec is higher-is-better ---
            ComparisonResult speed = comparison.GetComparison("Speed_MBPerMSec");

            // The run produced real, finite values.
            double.IsNaN(speed.BaselineMetric).Should().BeFalse("the run should compute a baseline Speed_MBPerMSec");
            double.IsNaN(speed.ComparandMetric).Should().BeFalse("the run should compute a comparand Speed_MBPerMSec");
            speed.BaselineMetric.Should().BeGreaterThan(0);
            speed.ComparandMetric.Should().BeGreaterThan(0);

            // Displayed delta uses the real, unflipped data.
            speed.Delta.Should().Be(speed.ComparandMetric - speed.BaselineMetric);

            // Categorization is direction-aware: for a higher-is-better metric the regression-oriented
            // delta is the negation of the displayed percentage delta.
            speed.PercentageDelta.Should().NotBe(double.NaN);
            speed.RegressionPercentageDelta.Should().BeApproximately(-speed.PercentageDelta, 1e-9);

            // --- A lower-is-better metric keeps the same sign for categorization ---
            ComparisonResult pause = comparison.GetComparison("PauseDurationMSec_Sum");
            pause.PercentageDelta.Should().NotBe(double.NaN);
            pause.RegressionPercentageDelta.Should().BeApproximately(pause.PercentageDelta, 1e-9);

            // --- End-to-end categorization through the analyze pipeline ---
            GCTraceMetrics[] baselineMetrics = { new(baselineData, "Run", "baseline") };
            GCTraceMetrics[] comparandMetrics = { new(comparandData, "Run", "comparand") };

            GCTraceMetricComparisonResult speedComparison =
                GCTraceMetricComparison.CompareGCTraceMetric(baselineMetrics, comparandMetrics, "Speed_MBPerMSec");
            GCTraceMetricComparisonResults results = new("Run", new[] { speedComparison });

            // A faster comparand (higher Speed_MBPerMSec) must be categorized as an improvement, never a regression.
            bool isFaster = speedComparison.AveragedComparandMetric > speedComparison.AveragedBaselineMetric;
            IEnumerable<GCTraceMetricComparisonResult> regressionBuckets =
                results.LargeRegressions.Concat(results.Regressions).Concat(results.StaleRegressions);
            IEnumerable<GCTraceMetricComparisonResult> improvementBuckets =
                results.LargeImprovements.Concat(results.Improvements).Concat(results.StaleImprovements);

            if (isFaster)
            {
                regressionBuckets.Should().NotContain(speedComparison, "a faster Speed_MBPerMSec is an improvement, not a regression");
                improvementBuckets.Should().Contain(speedComparison);
            }
            else if (speedComparison.AveragedComparandMetric < speedComparison.AveragedBaselineMetric)
            {
                improvementBuckets.Should().NotContain(speedComparison, "a slower Speed_MBPerMSec is a regression, not an improvement");
                regressionBuckets.Should().Contain(speedComparison);
            }
        }
    }
}
