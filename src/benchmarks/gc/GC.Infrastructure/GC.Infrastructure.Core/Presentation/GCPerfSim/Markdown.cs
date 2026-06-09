using GC.Analysis.API;
using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using System.Text;

namespace GC.Infrastructure.Core.Presentation.GCPerfSim
{
    public static class Markdown
    {
        public static void GenerateComparisonTable(ResultItem baseResultItem, ResultItem comparandResultItem, string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                StringBuilder sb = new();
                sw.WriteLine("# Summary");
                sw.WriteLine();

                sw.WriteLine("| *ExecutionTime (MSec)* - base | comparand | Δ% | *% GC Pause Time* - base | comparand | Δ% |");
                sw.WriteLine("|  ---------------------------  | --------- | ---| ------------------------------- | --------- | ---|");
                // Go through all the runs, get the baseline and the comparand values.

                double metric1_Base = baseResultItem.ExecutionTimeMSec;
                double metric2_Base = baseResultItem.PctTimePausedInGC;

                double metric1_Comparand = comparandResultItem.ExecutionTimeMSec;
                double metric2_Comparand = comparandResultItem.PctTimePausedInGC;

                sw.WriteLine($"| {metric1_Base:N2} | {metric1_Comparand:N2} | {((metric1_Comparand - metric1_Base) / metric1_Base) * 100:N2} | {metric2_Base:N2} |  {metric2_Comparand:N2} | {((metric2_Comparand - metric2_Base) / metric2_Base * 100):N2}| ");

                sw.WriteLine();

                // HeapSizeBeforeMB
                sw.WriteLine("| *Mean Heap Size Before (MB)* - base | comparand | Δ% |");
                sw.WriteLine("|  ---------------------------  | --------- | ---|");

                metric1_Base = baseResultItem.HeapSizeBeforeMB_Mean;
                metric1_Comparand = comparandResultItem.HeapSizeBeforeMB_Mean;

                sw.WriteLine($"| {metric1_Base:N2} | {metric1_Comparand:N2} | {((metric1_Comparand - metric1_Base) / metric1_Base) * 100:N2} |");
                sw.WriteLine();

                // Pauses.
                sw.WriteLine("| *Mean Ephemeral Pause (MSec)* - base | comparand | Δ% |");
                sw.WriteLine("| ---------------------------  | --------- | ---|");

                // Go through all the runs, get the baseline and the comparand values.
                metric1_Base = baseResultItem.PauseDurationMSec_MeanWhereIsEphemeral;
                metric1_Comparand = comparandResultItem.PauseDurationMSec_MeanWhereIsEphemeral;

                sw.WriteLine($"| {metric1_Base:N2} | {metric1_Comparand:N2} | {((metric1_Comparand - metric1_Base) / metric1_Base) * 100:N2} |");

                sw.WriteLine();

                sw.WriteLine($"| *Mean BGC Pause (MSec)* - base | comparand | Δ% | *Mean Full Blocking GC Pause (MSec)* - base | comparand | Δ% |");
                sw.WriteLine("| ---------------------------  | --------- | ---| ------------------------------- | --------- | ---|");

                // Go through all the runs, get the baseline and the comparand values.
                metric1_Base = baseResultItem.PauseDurationMSec_MeanWhereIsBackground;
                metric2_Base = baseResultItem.PauseDurationMSec_MeanWhereIsBlockingGen2;

                metric1_Comparand = comparandResultItem.PauseDurationMSec_95PWhereIsBackground;
                metric2_Comparand = comparandResultItem.PauseDurationMSec_95PWhereIsBlockingGen2;

                sw.WriteLine($"| {metric1_Base:N2} | {metric1_Comparand:N2} | {((metric1_Comparand - metric1_Comparand) / metric1_Base) * 100:N2} | {metric2_Base:N2} |  {metric2_Comparand:N2} | {((metric2_Comparand - metric2_Base) / metric2_Base * 100):N2}| ");
                sb.AppendLine();

                sb.AppendLine("# Individual Results");

                List<ComparisonResult> comparisonResults = new();

                var resultItemComparison = new ResultItemComparison(baseResultItem, comparandResultItem);
                foreach (var property in typeof(ResultItem).GetProperties())
                {
                    if (property.PropertyType != typeof(double))
                    {
                        continue;
                    }

                    string propertyNameToCheck = property.Name.ToLowerInvariant();

                    ComparisonResult result = resultItemComparison.GetComparison(property.Name);
                    comparisonResults.Add(result);
                }

                sb.AppendLine("#### Large Regressions (>20%)");
                sb.AppendLine();

                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.RegressionPercentageDelta > 20)))
                {
                    sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Large Improvements (>20%)");
                sb.AppendLine();

                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.RegressionPercentageDelta < -20)))
                {
                    sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Regressions (5% - 20%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.RegressionPercentageDelta > 5 && c.RegressionPercentageDelta < 20)))
                {
                    sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Improvements (5 - 20%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.RegressionPercentageDelta < -5 && c.RegressionPercentageDelta > -20)))
                {
                    sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Stale Regression (< 5%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.RegressionPercentageDelta >= 0 && c.RegressionPercentageDelta < 5)))
                {
                    sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Stale Improvements (< 5%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.RegressionPercentageDelta < 0 && c.RegressionPercentageDelta > -5)))
                {
                    sb.AppendLine($"|{r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }

                sb.AppendLine();
                sw.WriteLine("\n");
                sw.WriteLine(sb.ToString());
            }
        }

        public static void GenerateForAnalyzeCommand(GCPerfSimConfiguration configuration,
                                                     IEnumerable<GCTraceMetricComparisonResults> comparisonResultsCollection,
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
                    var execTimeDeltaPercentage = executionTimeComparisonResult?.PercentageDelta ?? double.NaN;

                    var gcPauseTimeComparisonResult = GetGCTraceMetricComparisonResultByRunNameAndMetricName(
                        comparisonResultsCollection, runName, "PctTimePausedInGC");
                    var gcPauseTimeBaseline = gcPauseTimeComparisonResult?.AveragedBaselineMetric ?? double.NaN;
                    var gcPauseTimeComparand = gcPauseTimeComparisonResult?.AveragedComparandMetric ?? double.NaN;
                    var gcPauseTimeDeltaPercentage = gcPauseTimeComparisonResult?.PercentageDelta ?? double.NaN;

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
                    var heapSizeBeforeDeltaPercentage = heapSizeBeforeComparisonResult?.PercentageDelta ?? double.NaN;
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
                    var meanEphemeralPauseDeltaPercentage = meanEphemeralPauseComparisonResult?.PercentageDelta ?? double.NaN;
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
                    var meanBGCPauseDeltaPercentage = meanBGCPauseComparisonResult?.PercentageDelta ?? double.NaN;

                    var meanFullBlockingGCPauseComparisonResult = GetGCTraceMetricComparisonResultByRunNameAndMetricName(
                        comparisonResultsCollection, runName, "PauseDurationMSec_MeanWhereIsBlockingGen2");
                    var meanFullBlockingGCPauseBaseline = meanFullBlockingGCPauseComparisonResult?.AveragedBaselineMetric ?? double.NaN;
                    var meanFullBlockingGCPauseComparand = meanFullBlockingGCPauseComparisonResult?.AveragedComparandMetric ?? double.NaN;
                    var meanFullBlockingGCPauseDeltaPercentage = meanFullBlockingGCPauseComparisonResult?.PercentageDelta ?? double.NaN;
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
                    sw.AddDetailsOfSingleComparison(configuration, comparisonResults);
                }
            }
        }

        internal static void AddDetailsOfSingleComparison(this StreamWriter sw,
                                                          GCPerfSimConfiguration configuration,
                                                          GCTraceMetricComparisonResults comparisonResult)
        {
            // Large Regressions
            sw.WriteLine($"### Large Regressions (>= 20%): {comparisonResult.LargeRegressions.Count()} \n");
            sw.AddTableForSingleCriteria(comparisonResult.LargeRegressions);
            sw.WriteLine("\n");

            // Large Improvements
            sw.WriteLine($"### Large Improvements (<= -20%): {comparisonResult.LargeImprovements.Count()} \n");
            sw.AddTableForSingleCriteria(comparisonResult.LargeImprovements);
            sw.WriteLine("\n");

            // Regressions
            sw.WriteLine($"### Regressions (>= 5% and < 20%): {comparisonResult.Regressions.Count()} \n");
            sw.AddTableForSingleCriteria(comparisonResult.Regressions);
            sw.WriteLine("\n");

            // Improvements
            sw.WriteLine($"### Improvements (> -20% and <= -5%): {comparisonResult.Improvements.Count()} \n");
            sw.AddTableForSingleCriteria(comparisonResult.Improvements);
            sw.WriteLine("\n");

            // Stale Regressions
            sw.WriteLine($"### Stale Regressions (> 0% and < 5%): {comparisonResult.StaleRegressions.Count()} \n");
            sw.AddTableForSingleCriteria(comparisonResult.StaleRegressions);
            sw.WriteLine("\n");

            // Stale Improvements
            sw.WriteLine($"### Stale Improvements (> -5% and <= 0%): {comparisonResult.StaleImprovements.Count()} \n");
            sw.AddTableForSingleCriteria(comparisonResult.StaleImprovements);
            sw.WriteLine("\n\n");
        }

        internal static void AddTableForSingleCriteria(this StreamWriter sw,
                                                       IEnumerable<GCTraceMetricComparisonResult> comparisons)
        {
            if (comparisons.ToList().Count == 0)
            {
                sw.WriteLine("No metrics in this category.\n");
                return;
            }
            var runName = comparisons.FirstOrDefault()?.RunName;
            sw.WriteLine($" | Metric | Base | {runName} | Δ%  |  Δ |");
            sw.WriteLine($" | -----  | ---- | ------  | ---  |  --- |");
            foreach (var r in comparisons)
            {
                sw.WriteLine($"| {r.MetricName} | {r.AveragedBaselineMetric:N2} | {r.AveragedComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
            }
        }

        internal static GCTraceMetricComparisonResult?
            GetGCTraceMetricComparisonResultByRunNameAndMetricName(IEnumerable<GCTraceMetricComparisonResults> comparisonResultsCollection,
                                                                   string runName,
                                                                   string metricName)
        {
            return comparisonResultsCollection
                .SelectMany(c => c.ComparisonResults)
                .FirstOrDefault(c => c.RunName == runName && c.MetricName == metricName, null);
        }
    }
}
