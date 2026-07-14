using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using System.Text;

namespace GC.Infrastructure.Core.Presentation.GCPerfSim
{
    public static class Markdown
    {
        public static void GenerateForCompareCommand(GCTraceMetricComparisonResults comparisonResult,
                                                     string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                StringBuilder sb = new();
                sw.WriteLine("# Summary");
                sw.WriteLine();

                sw.WriteLine("| *ExecutionTime (MSec)* - base | comparand | Δ% | *% GC Pause Time* - base | comparand | Δ% |");
                sw.WriteLine("|  ---------------------------  | --------- | ---| ------------------------------- | --------- | ---|");

                var executionTimeMSecMetric = GetGCTraceMetricComparisonResultByMetricName(comparisonResult, "ExecutionTimeMSec");
                var pctTimePausedInGCMetric = GetGCTraceMetricComparisonResultByMetricName(comparisonResult, "PctTimePausedInGC");
                double metric1_Base = executionTimeMSecMetric?.AveragedBaselineMetric ?? double.NaN;
                double metric2_Base = pctTimePausedInGCMetric?.AveragedBaselineMetric ?? double.NaN;

                double metric1_Comparand = executionTimeMSecMetric?.AveragedComparandMetric ?? double.NaN;
                double metric2_Comparand = pctTimePausedInGCMetric?.AveragedComparandMetric ?? double.NaN;

                double metric1_PercentageDelta = executionTimeMSecMetric?.RegressionPercentageDelta ?? double.NaN;
                double metric2_PercentageDelta = pctTimePausedInGCMetric?.RegressionPercentageDelta ?? double.NaN;

                sw.WriteLine($"| {metric1_Base:N2} | {metric1_Comparand:N2} | {metric1_PercentageDelta:N2} | {metric2_Base:N2} |  {metric2_Comparand:N2} | {metric2_PercentageDelta:N2}| ");

                sw.WriteLine();

                // HeapSizeBeforeMB
                sw.WriteLine("| *Mean Heap Size Before (MB)* - base | comparand | Δ% |");
                sw.WriteLine("|  ---------------------------  | --------- | ---|");

                var heapSizeBeforeMBMetric = GetGCTraceMetricComparisonResultByMetricName(comparisonResult, "HeapSizeBeforeMB_Mean");
                metric1_Base = heapSizeBeforeMBMetric?.AveragedBaselineMetric ?? double.NaN;
                metric1_Comparand = heapSizeBeforeMBMetric?.AveragedComparandMetric ?? double.NaN;
                metric1_PercentageDelta = heapSizeBeforeMBMetric?.RegressionPercentageDelta ?? double.NaN;

                sw.WriteLine($"| {metric1_Base:N2} | {metric1_Comparand:N2} | {metric1_PercentageDelta:N2} |");
                sw.WriteLine();

                // Pauses.
                sw.WriteLine("| *Mean Ephemeral Pause (MSec)* - base | comparand | Δ% |");
                sw.WriteLine("| ---------------------------  | --------- | ---|");

                var ephemeralPauseMetric = GetGCTraceMetricComparisonResultByMetricName(comparisonResult, "PauseDurationMSec_MeanWhereIsEphemeral");
                metric1_Base = ephemeralPauseMetric?.AveragedBaselineMetric ?? double.NaN;
                metric1_Comparand = ephemeralPauseMetric?.AveragedComparandMetric ?? double.NaN;
                metric1_PercentageDelta = ephemeralPauseMetric?.RegressionPercentageDelta ?? double.NaN;

                sw.WriteLine($"| {metric1_Base:N2} | {metric1_Comparand:N2} | {metric1_PercentageDelta:N2} |");

                sw.WriteLine();

                sw.WriteLine($"| *Mean BGC Pause (MSec)* - base | comparand | Δ% | *Mean Full Blocking GC Pause (MSec)* - base | comparand | Δ% |");
                sw.WriteLine("| ---------------------------  | --------- | ---| ------------------------------- | --------- | ---|");

                // Go through all the runs, get the baseline and the comparand values.
                var meanBGCMetric = GetGCTraceMetricComparisonResultByMetricName(comparisonResult, "PauseDurationMSec_MeanWhereIsBackground");
                var meanFullBlockingGCMetric = GetGCTraceMetricComparisonResultByMetricName(comparisonResult, "PauseDurationMSec_MeanWhereIsBlockingGen2");
                metric1_Base = meanBGCMetric?.AveragedBaselineMetric ?? double.NaN;
                metric2_Base = meanFullBlockingGCMetric?.AveragedBaselineMetric ?? double.NaN;

                metric1_Comparand = meanBGCMetric?.AveragedComparandMetric ?? double.NaN;
                metric2_Comparand = meanFullBlockingGCMetric?.AveragedComparandMetric ?? double.NaN;

                metric1_PercentageDelta = meanBGCMetric?.RegressionPercentageDelta ?? double.NaN;
                metric2_PercentageDelta = meanFullBlockingGCMetric?.RegressionPercentageDelta ?? double.NaN;

                sw.WriteLine($"| {metric1_Base:N2} | {metric1_Comparand:N2} | {metric1_PercentageDelta:N2} | {metric2_Base:N2} |  {metric2_Comparand:N2} | {metric2_PercentageDelta:N2}| ");
                sb.AppendLine();

                sb.AppendLine("# Individual Results"); 
                sb.AppendLine("#### Large Regressions (>20%)");
                sb.AppendLine();

                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in comparisonResult.LargeRegressions)
                {
                    sb.AppendLine($"| {r.MetricName} | {r.AveragedBaselineMetric:N2} | {r.AveragedComparandMetric:N2} | {r.RegressionPercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Large Improvements (>20%)");
                sb.AppendLine();

                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in comparisonResult.LargeImprovements)
                {
                    sb.AppendLine($"| {r.MetricName} | {r.AveragedBaselineMetric:N2} | {r.AveragedComparandMetric:N2} | {r.RegressionPercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Regressions (5% - 20%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in comparisonResult.Regressions)
                {
                    sb.AppendLine($"| {r.MetricName} | {r.AveragedBaselineMetric:N2} | {r.AveragedComparandMetric:N2} | {r.RegressionPercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Improvements (5 - 20%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in comparisonResult.Improvements)
                {
                    sb.AppendLine($"| {r.MetricName} | {r.AveragedBaselineMetric:N2} | {r.AveragedComparandMetric:N2} | {r.RegressionPercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Stale Regression (< 5%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in comparisonResult.StaleRegressions)
                {
                    sb.AppendLine($"| {r.MetricName} | {r.AveragedBaselineMetric:N2} | {r.AveragedComparandMetric:N2} | {r.RegressionPercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Stale Improvements (< 5%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in comparisonResult.StaleImprovements)
                {
                    sb.AppendLine($"| {r.MetricName} | {r.AveragedBaselineMetric:N2} | {r.AveragedComparandMetric:N2} | {r.RegressionPercentageDelta:N2} | {r.Delta:N2} |");
                }

                sb.AppendLine();
                sw.WriteLine("\n");
                sw.WriteLine(sb.ToString());
            }
        }

        public static void GenerateForAnalyzeCommand(GCPerfSimConfiguration configuration,
                                                     IReadOnlyCollection<GCTraceMetricComparisonResults> comparisonResultsCollection,
                                                     Dictionary<string, ProcessExecutionDetails> executionDetails,
                                                     string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                // Create summary.
                sw.WriteLine("# Summary"); 
                sw.WriteLine();

                sw.WriteLine("| Name | *ExecutionTime (MSec)* - base | comparand | Δ% | *% GC Pause Time* - base | comparand | Δ% |");
                sw.WriteLine("| ---- | ---------------------------  | --------- | ---| ------------------------------- | --------- | ---|");
                foreach (var run in configuration.Runs)
                {
                    var runName = run.Key;
                    var executionTimeComparisonResult = GetGCTraceMetricComparisonResultByRunNameAndMetricName(
                        comparisonResultsCollection, runName, "ExecutionTimeMSec");
                    var execTimeBaseline = executionTimeComparisonResult?.AveragedBaselineMetric ?? double.NaN;
                    var execTimeComparand = executionTimeComparisonResult?.AveragedComparandMetric ?? double.NaN;
                    var execTimeDeltaPercentage = executionTimeComparisonResult?.RegressionPercentageDelta ?? double.NaN;

                    var gcPauseTimeComparisonResult = GetGCTraceMetricComparisonResultByRunNameAndMetricName(
                        comparisonResultsCollection, runName, "PctTimePausedInGC");
                    var gcPauseTimeBaseline = gcPauseTimeComparisonResult?.AveragedBaselineMetric ?? double.NaN;
                    var gcPauseTimeComparand = gcPauseTimeComparisonResult?.AveragedComparandMetric ?? double.NaN;
                    var gcPauseTimeDeltaPercentage = gcPauseTimeComparisonResult?.RegressionPercentageDelta ?? double.NaN;

                    sw.WriteLine($"| {runName} | {execTimeBaseline:N2} | {execTimeComparand:N2} | {(double.IsNaN(execTimeDeltaPercentage) ? "NaN" : $"{execTimeDeltaPercentage:N2} %")} | {gcPauseTimeBaseline:N2} | {gcPauseTimeComparand:N2} | {(double.IsNaN(gcPauseTimeDeltaPercentage) ? "NaN" : $"{gcPauseTimeDeltaPercentage:N2} %")} |");
                }
                sw.WriteLine();

                sw.WriteLine("| Name | *Mean Heap Size Before (MB)* - base | comparand | Δ% |");
                sw.WriteLine("| ---- | ---------------------------  | --------- | ---|");
                foreach (var run in configuration.Runs)
                {
                    var runName = run.Key;
                    var heapSizeBeforeComparisonResult = GetGCTraceMetricComparisonResultByRunNameAndMetricName(
                        comparisonResultsCollection, runName, "HeapSizeBeforeMB_Mean");
                    var heapSizeBeforeBaseline = heapSizeBeforeComparisonResult?.AveragedBaselineMetric ?? double.NaN;
                    var heapSizeBeforeComparand = heapSizeBeforeComparisonResult?.AveragedComparandMetric ?? double.NaN;
                    var heapSizeBeforeDeltaPercentage = heapSizeBeforeComparisonResult?.RegressionPercentageDelta ?? double.NaN;
                    sw.WriteLine($"| {runName} | {heapSizeBeforeBaseline:N2} | {heapSizeBeforeComparand:N2} | {(double.IsNaN(heapSizeBeforeDeltaPercentage) ? "NaN" : $"{heapSizeBeforeDeltaPercentage:N2} %")} |");
                }
                sw.WriteLine();

                sw.WriteLine("| Name | *Mean Ephemeral Pause (MSec)* - base | comparand | Δ% |");
                sw.WriteLine("| ---- | ---------------------------  | --------- | ---|");
                foreach (var run in configuration.Runs)
                {
                    var runName = run.Key;
                    var meanEphemeralPauseComparisonResult = GetGCTraceMetricComparisonResultByRunNameAndMetricName(
                        comparisonResultsCollection, runName, "PauseDurationMSec_MeanWhereIsEphemeral");
                    var meanEphemeralPauseBaseline = meanEphemeralPauseComparisonResult?.AveragedBaselineMetric ?? double.NaN;
                    var meanEphemeralPauseComparand = meanEphemeralPauseComparisonResult?.AveragedComparandMetric ?? double.NaN;
                    var meanEphemeralPauseDeltaPercentage = meanEphemeralPauseComparisonResult?.RegressionPercentageDelta ?? double.NaN;
                    sw.WriteLine($"| {runName} | {meanEphemeralPauseBaseline:N2} | {meanEphemeralPauseComparand:N2} | {(double.IsNaN(meanEphemeralPauseDeltaPercentage) ? "NaN" : $"{meanEphemeralPauseDeltaPercentage:N2} %")} |");
                }
                sw.WriteLine();

                sw.WriteLine($"| Name | *Mean BGC Pause (MSec)* - base | comparand | Δ% | *Mean Full Blocking GC Pause (MSec)* - base | comparand | Δ% |");
                sw.WriteLine("| ---- | ---------------------------  | --------- | ---| ------------------------------- | --------- | ---|");
                foreach (var run in configuration.Runs)
                {
                    var runName = run.Key;
                    var meanBGCPauseComparisonResult = GetGCTraceMetricComparisonResultByRunNameAndMetricName(
                        comparisonResultsCollection, runName, "PauseDurationMSec_MeanWhereIsBackground");
                    var meanBGCPauseBaseline = meanBGCPauseComparisonResult?.AveragedBaselineMetric ?? double.NaN;
                    var meanBGCPauseComparand = meanBGCPauseComparisonResult?.AveragedComparandMetric ?? double.NaN;
                    var meanBGCPauseDeltaPercentage = meanBGCPauseComparisonResult?.RegressionPercentageDelta ?? double.NaN;

                    var meanFullBlockingGCPauseComparisonResult = GetGCTraceMetricComparisonResultByRunNameAndMetricName(
                        comparisonResultsCollection, runName, "PauseDurationMSec_MeanWhereIsBlockingGen2");
                    var meanFullBlockingGCPauseBaseline = meanFullBlockingGCPauseComparisonResult?.AveragedBaselineMetric ?? double.NaN;
                    var meanFullBlockingGCPauseComparand = meanFullBlockingGCPauseComparisonResult?.AveragedComparandMetric ?? double.NaN;
                    var meanFullBlockingGCPauseDeltaPercentage = meanFullBlockingGCPauseComparisonResult?.RegressionPercentageDelta ?? double.NaN;
                    sw.WriteLine($"| {runName} | {meanBGCPauseBaseline:N2} | {meanBGCPauseComparand:N2} | {(double.IsNaN(meanBGCPauseDeltaPercentage) ? "NaN" : $"{meanBGCPauseDeltaPercentage:N2} %")} | {meanFullBlockingGCPauseBaseline:N2} | {meanFullBlockingGCPauseComparand:N2} | {(double.IsNaN(meanFullBlockingGCPauseDeltaPercentage) ? "NaN" : $"{meanFullBlockingGCPauseDeltaPercentage:N2} %")} |");
                }
                sw.WriteLine();

                // Add Incomplete Tests.
                sw.AddIncompleteTestsSection(executionDetails);

                // Add Individual Run Comparisons.
                foreach (var comparisonResults in comparisonResultsCollection)
                {
                    sw.WriteLine($"# Individual Result - {comparisonResults.RunName}");
                    sw.WriteLine();
                    sw.AddReproSection(executionDetails);
                    sw.WriteLine();
                    sw.AddDetailsOfSingleRun(comparisonResults);
                }
            }
        }

        internal static void AddDetailsOfSingleRun(this StreamWriter sw,
                                                   GCTraceMetricComparisonResults comparisonResult)
        {
            // Large Regressions
            sw.WriteLine($"### Large Regressions (>= 20%): {comparisonResult.LargeRegressions.Count()} \n");
            sw.AddTablesForSingleCriteria(comparisonResult.LargeRegressions);
            sw.WriteLine("\n");

            // Large Improvements
            sw.WriteLine($"### Large Improvements (<= -20%): {comparisonResult.LargeImprovements.Count()} \n");
            sw.AddTablesForSingleCriteria(comparisonResult.LargeImprovements);
            sw.WriteLine("\n");

            // Regressions
            sw.WriteLine($"### Regressions (>= 5% and < 20%): {comparisonResult.Regressions.Count()} \n");
            sw.AddTablesForSingleCriteria(comparisonResult.Regressions);
            sw.WriteLine("\n");

            // Improvements
            sw.WriteLine($"### Improvements (> -20% and <= -5%): {comparisonResult.Improvements.Count()} \n");
            sw.AddTablesForSingleCriteria(comparisonResult.Improvements);
            sw.WriteLine("\n");

            // Stale Regressions
            sw.WriteLine($"### Stale Regressions (> 0% and < 5%): {comparisonResult.StaleRegressions.Count()} \n");
            sw.AddTablesForSingleCriteria(comparisonResult.StaleRegressions);
            sw.WriteLine("\n");

            // Stale Improvements
            sw.WriteLine($"### Stale Improvements (> -5% and <= 0%): {comparisonResult.StaleImprovements.Count()} \n");
            sw.AddTablesForSingleCriteria(comparisonResult.StaleImprovements);
            sw.WriteLine("\n\n");
        }
        internal static void AddTablesForSingleCriteria(this StreamWriter sw,
                                                        IEnumerable<GCTraceMetricComparisonResult> comparisons)
        {
            var comparisonList = comparisons.ToList();
            if (comparisonList.Count == 0)
            {
                sw.WriteLine("No metrics in this category.\n");
                return;
            }

            foreach (var comparison in comparisonList)
            {
                sw.AddTableForSingleMetric(comparison);
                sw.WriteLine("\n");
            }
        }

        internal static void AddTableForSingleMetric(this StreamWriter sw,
                                                     GCTraceMetricComparisonResult comparison)
        {
            var runName = comparison.RunName;
            var metricName = comparison.MetricName;
            sw.WriteLine($" | {metricName} | Base | {runName} | Δ%  |  Δ |");
            sw.WriteLine($" | -----  | ---- | ------  | ---  |  --- |");
            int maxIterations = Math.Max(comparison.OriginalBaselineMetricCollection.Count(), comparison.OriginalComparandMetricCollection.Count());
            for (int idx = 0; idx < maxIterations; idx++)
            {
                string baseRow = $"| iteration.{idx} ";
                var baselineValue = comparison.OriginalBaselineMetricCollection?.ElementAtOrDefault(idx) ??double.NaN;
                var comparandValue = comparison.OriginalComparandMetricCollection?.ElementAtOrDefault(idx) ?? double.NaN;
                var baselineStr = double.IsNaN(baselineValue) || !(comparison.OutliersFreeBaselineMetricCollection?.Contains(baselineValue) ?? false) ? $"**~~{Math.Round(baselineValue, 2)}~~**" : Math.Round(baselineValue, 2).ToString();
                var comparandStr = double.IsNaN(comparandValue) || !(comparison.OutliersFreeComparandMetricCollection?.Contains(comparandValue) ?? false) ? $"**~~{Math.Round(comparandValue, 2)}~~**" : Math.Round(comparandValue, 2).ToString();
                baseRow += $"| {baselineStr} | {comparandStr} | | |";
                sw.WriteLine(baseRow);
            }

            // Add Original Averaged, Diff and DiffPerc
            var originalAveragedBaselineValue = comparison.OriginalAveragedBaselineMetric;
            var originalAveragedComparandValue = comparison.OriginalAveragedComparandMetric;
            var originalDiff = comparison.OriginalDelta;
            var originalDiffPerc = comparison.OriginalRegressionPercentageDelta;
            string originalAveragedRow = $"| Average | {Math.Round(originalAveragedBaselineValue, 2)} | {Math.Round(originalAveragedComparandValue, 2)} | {Math.Round(originalDiffPerc, 2)} | {Math.Round(originalDiff, 2)} |";
            sw.WriteLine(originalAveragedRow);

            // Add Corrective Averaged, Diff and DiffPerc
            var averagedBaselineValue = comparison.AveragedBaselineMetric;
            var averagedComparandValue = comparison.AveragedComparandMetric;
            var diff = comparison.Delta;
            var diffPerc = comparison.RegressionPercentageDelta;
            string averagedRow = $"| Average (Corrected) | {Math.Round(averagedBaselineValue, 2)} | {Math.Round(averagedComparandValue, 2)} | {Math.Round(diffPerc, 2)} | {Math.Round(diff, 2)} |";
            sw.WriteLine(averagedRow);
        }

        internal static GCTraceMetricComparisonResult?
            GetGCTraceMetricComparisonResultByMetricName(GCTraceMetricComparisonResults comparisonResults, string metricName)
        {
            return comparisonResults.ComparisonResults
                .FirstOrDefault(c => c.MetricName == metricName);
        }

        internal static GCTraceMetricComparisonResult?
            GetGCTraceMetricComparisonResultByRunNameAndMetricName(IEnumerable<GCTraceMetricComparisonResults> comparisonResultsCollection,
                                                                   string runName,
                                                                   string metricName)
        {
            return comparisonResultsCollection
                .SelectMany(c => c.ComparisonResults)
                .FirstOrDefault(c => c?.RunName == runName && c?.MetricName == metricName);
        }
    }
}
