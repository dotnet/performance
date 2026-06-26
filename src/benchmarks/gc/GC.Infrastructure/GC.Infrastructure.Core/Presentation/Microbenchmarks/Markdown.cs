using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Analysis.Microbenchmarks;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using API = GC.Analysis.API;

namespace GC.Infrastructure.Core.Presentation.Microbenchmarks
{
    public static class Markdown
    {
        public static void GenerateTable(MicrobenchmarkConfiguration configuration, IReadOnlyCollection<MicrobenchmarkComparisonResults> comparisonResultsCollection, Dictionary<string, ProcessExecutionDetails> executionDetails, string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                // Create summary.
                sw.WriteLine("# Summary");

                string header = $"| Criteria | {string.Join("|", API.GoodLinq.Select(comparisonResultsCollection, s => $"[{s.BaselineName} {s.RunName}]({s.MarkdownIdentifier})"))}|";
                sw.WriteLine(header);
                sw.WriteLine($"| ----- | {string.Join("|", Enumerable.Repeat(" ----- ", comparisonResultsCollection.Count))} |");
                sw.WriteLine($"| Large Regressions (>= 20%) | {API.GoodLinq.Sum(comparisonResultsCollection, s => s.LargeRegressions.Count())}|");
                sw.WriteLine($"| Regressions (>= 5% and < 20%) | {API.GoodLinq.Sum(comparisonResultsCollection, s => s.Regressions.Count())}|");
                sw.WriteLine($"| Stale Regressions (> 0% and < 5%) | {API.GoodLinq.Sum(comparisonResultsCollection, s => s.StaleRegressions.Count())}|");
                sw.WriteLine($"| Stale Improvements (> -5% and <= 0%) | {API.GoodLinq.Sum(comparisonResultsCollection, s => s.StaleImprovements.Count())}|");
                sw.WriteLine($"| Improvements (> -20% and <= -5%) | {API.GoodLinq.Sum(comparisonResultsCollection, s => s.Improvements.Count())}|");
                sw.WriteLine($"| Large Improvements (<= -20%) | {API.GoodLinq.Sum(comparisonResultsCollection, s => s.LargeImprovements.Count())}|");
                sw.WriteLine($"| Total | {comparisonResultsCollection.Count} |");
                sw.WriteLine("\n");

                // Incomplete Tests.
                sw.AddIncompleteTestsSection(executionDetails);

                // Results.
                sw.WriteLine("# Results");

                sw.AddReproSection(executionDetails);

                sw.WriteLine("## Individual Results");

                // Add details of Each Comparison.
                foreach (var comparisonResults in comparisonResultsCollection)
                {
                    AddDetailsOfSingleComparison(sw, configuration, comparisonResults);
                    sw.WriteLine("\n");
                }
            }
        }

        internal static void AddDetailsOfSingleComparison(this StreamWriter sw,
                                                          MicrobenchmarkConfiguration configuration,
                                                          MicrobenchmarkComparisonResults comparisonResult)
        {
            sw.WriteLine($"## {comparisonResult.BaselineName} vs {comparisonResult.RunName}");
            sw.WriteLine("\n");

            // Large Regressions
            sw.WriteLine($"### Large Regressions (>= 20%): {comparisonResult.LargeRegressions.Count()} \n");
            sw.AddTablesForSingleCriteria(configuration, comparisonResult.LargeRegressions);
            sw.WriteLine("\n");

            // Large Improvements
            sw.WriteLine($"### Large Improvements (<= -20%): {comparisonResult.LargeImprovements.Count()} \n");
            sw.AddTablesForSingleCriteria(configuration, comparisonResult.LargeImprovements);
            sw.WriteLine("\n");

            // Regressions
            sw.WriteLine($"### Regressions (>= 5% and < 20%): {comparisonResult.Regressions.Count()} \n");
            sw.AddTablesForSingleCriteria(configuration, comparisonResult.Regressions);
            sw.WriteLine("\n");

            // Improvements
            sw.WriteLine($"### Improvements (> -20% and <= -5%): {comparisonResult.Improvements.Count()} \n");
            sw.AddTablesForSingleCriteria(configuration, comparisonResult.Improvements);
            sw.WriteLine("\n");

            // Stale Regressions
            sw.WriteLine($"### Stale Regressions (> 0% and < 5%): {comparisonResult.StaleRegressions.Count()} \n");
            sw.AddTablesForSingleCriteria(configuration, comparisonResult.StaleRegressions);
            sw.WriteLine("\n");

            // Stale Improvements
            sw.WriteLine($"### Stale Improvements (> -5% and <= 0%): {comparisonResult.StaleImprovements.Count()} \n");
            sw.AddTablesForSingleCriteria(configuration, comparisonResult.StaleImprovements);
            sw.WriteLine("\n\n");

            if (configuration.Output.additional_report_metrics != null)
            {
                foreach (var metric in configuration.Output.additional_report_metrics)
                {
                    sw.WriteLine($"## Comparison by {metric}");
                    var ordered = comparisonResult.Comparisons
                        .Where(c => c.OtherMetricsDiffPerc.ContainsKey(metric))
                        .OrderByDescending(c => c.OtherMetricsDiffPerc[metric]);

                    // Large Regressions
                    var largeRegression = API.GoodLinq.Where(ordered, o => o.OtherMetricsDiffPerc[metric] >= 20);
                    sw.WriteLine($"### Large Regressions (>= 20%): {largeRegression.Count()} \n");
                    sw.AddTablesForSingleCriteria(configuration, largeRegression, metric);
                    sw.WriteLine("\n");

                    // Large Improvements
                    var largeImprovements = API.GoodLinq.Where(ordered, o => o.OtherMetricsDiffPerc[metric] <= -20);
                    largeImprovements.Reverse();
                    sw.WriteLine($"### Large Improvements (<= -20%): {largeImprovements.Count()} \n");
                    sw.AddTablesForSingleCriteria(configuration, largeImprovements, metric);
                    sw.WriteLine("\n");

                    // Regressions
                    var regressions = API.GoodLinq.Where(ordered, o => o.OtherMetricsDiffPerc[metric] >= 5 && o.OtherMetricsDiffPerc[metric] < 20);
                    sw.WriteLine($"### Regressions (>= 5% and < 20%): {regressions.Count()} \n");
                    sw.AddTablesForSingleCriteria(configuration, regressions, metric);
                    sw.WriteLine("\n");

                    // Improvements
                    var improvements = API.GoodLinq.Where(ordered, o => o.OtherMetricsDiffPerc[metric] <= -5 && o.OtherMetricsDiffPerc[metric] > -20);
                    improvements.Reverse();
                    sw.WriteLine($"### Improvements (> -20% and <= -5%): {improvements.Count()} \n");
                    sw.AddTablesForSingleCriteria(configuration, improvements, metric);
                    sw.WriteLine("\n");

                    // Stale Regressions
                    var staleRegressions = API.GoodLinq.Where(ordered, o => o.OtherMetricsDiffPerc[metric] > 0.0 && o.OtherMetricsDiffPerc[metric] < 5);
                    sw.WriteLine($"### Stale Regressions (> 0% and < 5%): {staleRegressions.Count()} \n");
                    sw.AddTablesForSingleCriteria(configuration, staleRegressions, metric);
                    sw.WriteLine("\n");

                    // Stale Improvements
                    var staleImprovements = API.GoodLinq.Where(ordered, o => o.OtherMetricsDiffPerc[metric] > -5 && o.OtherMetricsDiffPerc[metric] <= 0.0);
                    staleImprovements.Reverse();
                    sw.WriteLine($"### Stale Improvements (> -5% and <= 0%): {staleImprovements.Count()} \n");
                    sw.AddTablesForSingleCriteria(configuration, staleImprovements, metric);
                    sw.WriteLine("\n");
                }
            }
        }

        internal static void AddTablesForSingleCriteria(this StreamWriter sw,
                                                        MicrobenchmarkConfiguration configuration,
                                                        IReadOnlyCollection<MicrobenchmarkComparisonResult> comparisons,
                                                        string? metricName = null)
        {
            if (comparisons.Count() == 0)
            {
                sw.WriteLine("No benchmarks in this category.");
                return;
            }
            foreach (var comparison in comparisons)
            {
                sw.AddTableForSingleBenchmark(configuration, comparison, metricName);
                sw.WriteLine("\n");
            }
        }

        internal static void AddTableForSingleBenchmark(this StreamWriter sw,
                                                        MicrobenchmarkConfiguration configuration,
                                                        MicrobenchmarkComparisonResult comparison,
                                                        string? metricName = null)
        {
            string benchmarkName = comparison.MicrobenchmarkName.Replace("<", "\\<").Replace(">", "\\>");
            string tableHeader0 = "";
            if (!string.IsNullOrEmpty(metricName))
            {
                tableHeader0 = $"| {benchmarkName} | Baseline {metricName} | Comparand {metricName} | Δ {metricName} | Δ% {metricName} |";
            }
            else
            {
                tableHeader0 = $"| {benchmarkName} | Baseline Mean Duration (MSec) | Comparand Mean Duration (MSec) | Δ Mean Duration (MSec) | Δ% Mean Duration |";
            }
            string tableHeader1 = "| --- | --- | --- | --- | --- | ";

            if (configuration.Output.Columns != null)
            {
                foreach (var column in configuration.Output.Columns)
                {
                    tableHeader0 += $"Baseline {column}  | Comparand {column} |  Δ {column} |  Δ% {column} |";
                    tableHeader1 += "--- | --- | --- | --- |";
                }
            }

            // TODO: Add CPU columns if needed in the future.
            //if (configuration.Output.cpu_columns != null)
            //{
            //    foreach (var column in configuration.Output.cpu_columns)
            //    {
            //        tableHeader0 += $"Baseline {column}  | Comparand {column} |  Δ {column} |  Δ% {column} |";
            //        tableHeader1 += "--- | --- | --- | --- |";
            //    }
            //}

            sw.WriteLine(tableHeader0);
            sw.WriteLine(tableHeader1);

            for (int idx = 0; idx < Math.Max(comparison.Baselines.Count(), comparison.Comparands.Count()); idx++)
            {
                try
                {
                    string baseRow = $"| iteration.{idx} ";

                    string baselineStr = "";
                    string comparandStr = "";
                    if (!String.IsNullOrEmpty(metricName))
                    {
                        var baselineValue = comparison.OriginalBaselineOtherMetrics.GetValueOrDefault(metricName)?.ElementAtOrDefault(idx) ?? double.NaN;
                        var comparandValue = comparison.OriginalComparandOtherMetrics.GetValueOrDefault(metricName)?.ElementAtOrDefault(idx) ?? double.NaN;
                        baselineStr = double.IsNaN(baselineValue) || !(comparison.OutliersFreeBaselineOtherMetrics.GetValueOrDefault(metricName)?.Contains(baselineValue) ?? false) ? $"**~~{Math.Round(baselineValue, 2)}~~**" : Math.Round(baselineValue, 2).ToString();
                        comparandStr = double.IsNaN(comparandValue) || !(comparison.OutliersFreeComparandOtherMetrics.GetValueOrDefault(metricName)?.Contains(comparandValue) ?? false) ? $"**~~{Math.Round(comparandValue, 2)}~~**" : Math.Round(comparandValue, 2).ToString();
                    }
                    else
                    {
                        var baselineValue = comparison.OriginalBaselineMeanValueCollection.ElementAtOrDefault(idx);
                        var comparandValue = comparison.OriginalComparandMeanValueCollection.ElementAtOrDefault(idx);
                        baselineStr = double.IsNaN(baselineValue) || !comparison.OutliersFreeBaselineMeanValueCollection.Contains(baselineValue) ? $"**~~{Math.Round(baselineValue, 2)}~~**" : Math.Round(baselineValue, 2).ToString();
                        comparandStr = double.IsNaN(comparandValue) || !comparison.OutliersFreeComparandMeanValueCollection.Contains(comparandValue) ? $"**~~{Math.Round(comparandValue, 2)}~~**" : Math.Round(comparandValue, 2).ToString();
                    }

                    baseRow += $"| {baselineStr} | {comparandStr} | | |";

                    if (configuration.Output.Columns != null)
                    {
                        foreach (var column in configuration.Output.Columns)
                        {
                            double baselineColumnValue = comparison.OriginalBaselineOtherMetrics.GetValueOrDefault(column)?.ElementAtOrDefault(idx) ?? double.NaN;
                            double comparandColumnValue = comparison.OriginalComparandOtherMetrics.GetValueOrDefault(column)?.ElementAtOrDefault(idx) ?? double.NaN;

                            string baselineResult = double.IsNaN(baselineColumnValue) || !(comparison.OutliersFreeBaselineOtherMetrics.GetValueOrDefault(column)?.Contains(baselineColumnValue) ?? false) ? $"**~~{Math.Round(baselineColumnValue, 2)}~~**" : Math.Round(baselineColumnValue, 2).ToString();
                            string comparandResult = double.IsNaN(comparandColumnValue) || !(comparison.OutliersFreeComparandOtherMetrics.GetValueOrDefault(column)?.Contains(comparandColumnValue) ?? false) ? $"**~~{Math.Round(comparandColumnValue, 2)}~~**" : Math.Round(comparandColumnValue, 2).ToString();

                            baseRow += $"{baselineResult} | {comparandResult} | | |";
                        }
                    }

                    // TODO: Add CPU columns if needed in the future.
                    //if (configuration.Output.cpu_columns != null)
                    //{
                    //    foreach (var column in configuration.Output.cpu_columns)
                    //    {
                    //        if (!lr.Baseline.OtherMetrics.TryGetValue(column, out double? baselineValue))
                    //        {
                    //            lr.Baseline.OtherMetrics[column] = baselineValue = lr.Baseline.CPUData?.GetIncCountForGCMethod(column) ?? null;
                    //        }
                    //        string baselineResult = baselineValue.HasValue ? Math.Round(baselineValue.Value, 2).ToString() : string.Empty;

                    //        if (!lr.Comparand.OtherMetrics.TryGetValue(column, out double? comparandValue))
                    //        {
                    //            lr.Comparand.OtherMetrics[column] = comparandValue = lr.Comparand.CPUData?.GetIncCountForGCMethod(column) ?? null;
                    //        }
                    //        string comparandResult = comparandValue.HasValue ? Math.Round(comparandValue.Value, 2).ToString() : string.Empty;

                    //        double? delta = baselineValue.HasValue && comparandValue.HasValue ? comparandValue.Value - baselineValue.Value : null;
                    //        string deltaResult = delta.HasValue ? Math.Round(delta.Value, 2).ToString() : string.Empty;

                    //        double? deltaPercent = delta.HasValue ? (delta / baselineValue.Value) * 100 : null;
                    //        string deltaPercentResult = deltaPercent.HasValue ? Math.Round(deltaPercent.Value, 2).ToString() : string.Empty;

                    //        baseRow += $"{baselineResult} | {comparandResult} | {deltaResult} | {deltaPercentResult} |";
                    //    }
                    //}

                    sw.WriteLine(baseRow);
                }

                catch (Exception e)
                {
                    Console.WriteLine($"Exception while processing: {benchmarkName} for {comparison.BaselineRunName} x {comparison.ComparandRunName}");
                    Console.WriteLine(e.StackTrace);
                }
            }

            // Add Original Averaged, Diff and DiffPerc
            double originalAveragedBaselineValue;
            double originalAveragedComparandValue;
            double originalDiff;
            double originalDiffPerc;
            if (!String.IsNullOrEmpty(metricName))
            {
                originalAveragedBaselineValue = comparison.OriginalAveragedBaselineOtherMetrics.GetValueOrDefault(metricName, double.NaN);
                originalAveragedComparandValue = comparison.OriginalAveragedComparandOtherMetrics.GetValueOrDefault(metricName, double.NaN);
                originalDiff = comparison.OriginalOtherMetricsDiff.GetValueOrDefault(metricName, double.NaN);
                originalDiffPerc = comparison.OriginalOtherMetricsDiffPerc.GetValueOrDefault(metricName, double.NaN);
            }
            else
            {
                originalAveragedBaselineValue = comparison.OriginalAveragedBaselineMeanValue;
                originalAveragedComparandValue = comparison.OriginalAveragedComparandMeanValue;
                originalDiff = comparison.OriginalMeanDiff;
                originalDiffPerc = comparison.OriginalMeanDiffPerc;
            }
            string originalAveragedRow = $"| Average | {Math.Round(originalAveragedBaselineValue, 2)} | {Math.Round(originalAveragedComparandValue, 2)} | {Math.Round(originalDiff, 2)} | {Math.Round(originalDiffPerc, 2)} |";
            if (configuration.Output.Columns != null)
            {
                foreach (var column in configuration.Output.Columns)
                {
                    double originalAveragedBaselineColumnValue = comparison.OriginalAveragedBaselineOtherMetrics.GetValueOrDefault(column, double.NaN);
                    double originalAveragedComparandColumnValue = comparison.OriginalAveragedComparandOtherMetrics.GetValueOrDefault(column, double.NaN);
                    double originalColumnDiff = comparison.OriginalOtherMetricsDiff.GetValueOrDefault(column, double.NaN);
                    double originalColumnDiffPerc = comparison.OriginalOtherMetricsDiffPerc.GetValueOrDefault(column, double.NaN);
                    originalAveragedRow += $" {Math.Round(originalAveragedBaselineColumnValue, 2)} | {Math.Round(originalAveragedComparandColumnValue, 2)} | {Math.Round(originalColumnDiff, 2)} | {Math.Round(originalColumnDiffPerc, 2)} |";
                }
            }
            sw.WriteLine(originalAveragedRow);

            // Add Corrective Averaged, Diff and DiffPerc
            double averagedBaselineValue;
            double averagedComparandValue;
            double diff;
            double diffPerc;
            if (!String.IsNullOrEmpty(metricName))
            {
                averagedBaselineValue = comparison.AveragedBaselineOtherMetrics.GetValueOrDefault(metricName, double.NaN);
                averagedComparandValue = comparison.AveragedComparandOtherMetrics.GetValueOrDefault(metricName, double.NaN);
                diff = comparison.OtherMetricsDiff.GetValueOrDefault(metricName, double.NaN);
                diffPerc = comparison.OtherMetricsDiffPerc.GetValueOrDefault(metricName, double.NaN);
            }
            else
            {
                averagedBaselineValue = comparison.AveragedBaselineMeanValue;
                averagedComparandValue = comparison.AveragedComparandMeanValue;
                diff = comparison.MeanDiff;
                diffPerc = comparison.MeanDiffPerc;
            }
            string averagedRow = $"| Corrective Average | {Math.Round(averagedBaselineValue, 2)} | {Math.Round(averagedComparandValue, 2)} | {Math.Round(diff, 2)} | {Math.Round(diffPerc, 2)} |";
            if (configuration.Output.Columns != null)
            {
                foreach (var column in configuration.Output.Columns)
                {
                    double averagedBaselineColumnValue = comparison.AveragedBaselineOtherMetrics.GetValueOrDefault(column, double.NaN);
                    double averagedComparandColumnValue = comparison.AveragedComparandOtherMetrics.GetValueOrDefault(column, double.NaN);
                    double columnDiff = comparison.OtherMetricsDiff.GetValueOrDefault(column, double.NaN);
                    double columnDiffPerc = comparison.OtherMetricsDiffPerc.GetValueOrDefault(column, double.NaN);
                    averagedRow += $" {Math.Round(averagedBaselineColumnValue, 2)} | {Math.Round(averagedComparandColumnValue, 2)} | {Math.Round(columnDiff, 2)} | {Math.Round(columnDiffPerc, 2)} |";
                }
            }
            sw.WriteLine(averagedRow);
        }
    }
}
