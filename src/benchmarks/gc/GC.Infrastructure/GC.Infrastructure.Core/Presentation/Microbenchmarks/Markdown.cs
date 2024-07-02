using API = GC.Analysis.API;
using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Analysis.Microbenchmarks;
using GC.Analysis.API;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;

namespace GC.Infrastructure.Core.Presentation.Microbenchmarks
{
    public static class Markdown
    {
        private const string baseTableString = "| Benchmark Name | Baseline | Comparand | Baseline Mean Duration (MSec) | Comparand Mean Duration (MSec) | Δ Mean Duration (MSec) | Δ% Mean Duration |";
        private const string baseTableRows = "| --- | --- | -- | --- | --- | --- | --- | ";

        public static void GenerateTable(MicrobenchmarkConfiguration configuration, IReadOnlyList<MicrobenchmarkComparisonResults> comparisonResults, Dictionary<string, ProcessExecutionDetails> executionDetails, string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                // Create summary.
                sw.WriteLine("# Summary");

                string header = $"| Criteria | {string.Join("|", GoodLinq.Select(comparisonResults, s => $"[{s.BaselineName} {s.RunName}]({s.MarkdownIdentifier})"))}|";
                sw.WriteLine(header);
                sw.WriteLine($"| ----- | {string.Join("|", Enumerable.Repeat(" ----- ", comparisonResults.Count))} |");
                sw.WriteLine($"| Large Regressions (>20%) | {GoodLinq.Sum(comparisonResults, s => s.LargeRegressions.Count())}|");
                sw.WriteLine($"| Regressions (5% - 20%) | {GoodLinq.Sum(comparisonResults, s => s.Regressions.Count())}|");
                sw.WriteLine($"| Stale Regressions (0% - 5%) | {GoodLinq.Sum(comparisonResults, s => s.StaleRegressions.Count())}|");
                sw.WriteLine($"| Stale Improvements (0% - 5%) | {GoodLinq.Sum(comparisonResults, s => s.StaleImprovements.Count())}|");
                sw.WriteLine($"| Improvements (5% - 20%) | {GoodLinq.Sum(comparisonResults, s => s.Improvements.Count())}|");
                sw.WriteLine($"| Large Improvements (>20%) | {GoodLinq.Sum(comparisonResults, s => s.LargeImprovements.Count())}|");
                sw.WriteLine($"| Total | {comparisonResults.Count} |");
                sw.WriteLine("\n");

                // Incomplete Tests.
                sw.AddIncompleteTestsSection(executionDetails);

                // Results.
                sw.WriteLine("# Results");

                sw.AddReproSection(executionDetails);

                sw.WriteLine("## Individual Results");

                // Add details of Each Comparison.
                foreach (var comparisonResult in comparisonResults)
                {
                    AddDetailsOfSingleComparison(sw, configuration, comparisonResult);
                    sw.WriteLine("\n");
                }
            }
        }

        internal static void AddDetailsOfSingleComparison(this StreamWriter sw, MicrobenchmarkConfiguration configuration, MicrobenchmarkComparisonResults comparisonResult)
        {
            sw.WriteLine($"## {comparisonResult.BaselineName} vs {comparisonResult.RunName}");
            sw.WriteLine("\n");

            // Large Regressions
            sw.WriteLine($"### Large Regressions (>20%): {comparisonResult.LargeRegressions.Count()} \n");
            sw.AddTableForSingleCriteria(configuration, comparisonResult.LargeRegressions);
            sw.WriteLine("\n");

            // Large Improvements
            sw.WriteLine($"### Large Improvements (>20%): {comparisonResult.LargeImprovements.Count()} \n");
            sw.AddTableForSingleCriteria(configuration, comparisonResult.LargeImprovements);
            sw.WriteLine("\n");

            // Regressions
            sw.WriteLine($"### Regressions (5% - 20%): {comparisonResult.Regressions.Count()} \n");
            sw.AddTableForSingleCriteria(configuration, comparisonResult.Regressions);
            sw.WriteLine("\n");

            // Improvements
            sw.WriteLine($"### Improvements (5% - 20%): {comparisonResult.Improvements.Count()} \n");
            sw.AddTableForSingleCriteria(configuration, comparisonResult.Improvements);
            sw.WriteLine("\n");

            // Stale Regressions
            sw.WriteLine($"### Stale Regressions (Same or percent difference within 5% margin): {comparisonResult.StaleRegressions.Count()} \n");
            sw.AddTableForSingleCriteria(configuration, comparisonResult.StaleRegressions);
            sw.WriteLine("\n");

            // Stale Improvements
            sw.WriteLine($"### Stale Improvements (Same or percent difference within 5% margin): {comparisonResult.StaleImprovements.Count()} \n");
            sw.AddTableForSingleCriteria(configuration, comparisonResult.StaleImprovements);
            sw.WriteLine("\n\n");

            if (configuration.Output.additional_report_metrics != null)
            {
                foreach (var metric in configuration.Output.additional_report_metrics)
                {
                    sw.WriteLine($"## Comparison by {metric}");
                    var ordered = comparisonResult.Comparisons.OrderByDescending(c => c.GetDiffPercentFromOtherMetrics(metric));

                    // Large Regressions
                    sw.WriteLine($"### Large Regressions (>20%): {comparisonResult.LargeRegressions.Count()} \n");
                    sw.AddTableForSingleCriteria(configuration, GoodLinq.Where(ordered, o => o.GetDiffPercentFromOtherMetrics(metric) > 0.2));
                    sw.WriteLine("\n");

                    // Large Improvements
                    sw.WriteLine($"### Large Improvements (>20%): {comparisonResult.LargeImprovements.Count()} \n");
                    var largeImprovements = GoodLinq.Where(ordered, o => o.GetDiffPercentFromOtherMetrics(metric) < -0.2);
                    largeImprovements.Reverse();
                    sw.AddTableForSingleCriteria(configuration, largeImprovements);
                    sw.WriteLine("\n");

                    // Regressions
                    sw.WriteLine($"### Regressions (5% - 20%): {comparisonResult.Regressions.Count()} \n");
                    sw.AddTableForSingleCriteria(configuration, GoodLinq.Where(ordered, o => o.GetDiffPercentFromOtherMetrics(metric) > 0.05 && o.GetDiffPercentFromOtherMetrics(metric) < 0.2));
                    sw.WriteLine("\n");

                    // Improvements
                    sw.WriteLine($"### Improvements (5% - 20%): {comparisonResult.Improvements.Count()} \n");
                    var improvements = GoodLinq.Where(ordered, o => o.GetDiffPercentFromOtherMetrics(metric) > 0.05 && o.GetDiffPercentFromOtherMetrics(metric) < 0.2);
                    improvements.Reverse();
                    sw.AddTableForSingleCriteria(configuration, improvements);
                    sw.WriteLine("\n");

                    // Stale Regressions
                    sw.WriteLine($"### Stale Regressions (Same or percent difference within 5% margin): {comparisonResult.StaleRegressions.Count()} \n");
                    sw.AddTableForSingleCriteria(configuration, GoodLinq.Where(ordered, o => o.GetDiffPercentFromOtherMetrics(metric) < 0.05 && o.GetDiffPercentFromOtherMetrics(metric) >= 0.0));
                    sw.WriteLine("\n");

                    // Stale Improvements
                    sw.WriteLine($"### Stale Improvements (Same or percent difference within 5% margin): {comparisonResult.StaleImprovements.Count()} \n");
                    var staleImprovements = GoodLinq.Where(ordered, o => o.GetDiffPercentFromOtherMetrics(metric) > -0.05 && o.GetDiffPercentFromOtherMetrics(metric) < 0.0);
                    staleImprovements.Reverse();
                    sw.AddTableForSingleCriteria(configuration, staleImprovements);
                    sw.WriteLine("\n");
                }
            }
        }

        internal static void AddTableForSingleCriteria(this StreamWriter sw, MicrobenchmarkConfiguration configuration, IEnumerable<MicrobenchmarkComparisonResult> comparisons)
        {
            // Check if all comparisons have traces.
            string tableHeader0 = baseTableString;
            string tableHeader1 = baseTableRows;

            if (configuration.Output.Columns != null)
            {
                foreach (var column in configuration.Output.Columns)
                {
                    tableHeader0 += $"Baseline {column}  | Comparand {column} |  Δ {column} |  Δ% {column} |";
                    tableHeader1 += "--- | --- | --- | --- |";
                }
            }

            if (configuration.Output.cpu_columns != null)
            {
                foreach (var column in configuration.Output.cpu_columns)
                {
                    tableHeader0 += $"Baseline {column}  | Comparand {column} |  Δ {column} |  Δ% {column} |";
                    tableHeader1 += "--- | --- | --- | --- |";
                }
            }

            sw.WriteLine(tableHeader0);
            sw.WriteLine(tableHeader1);

            foreach (var lr in comparisons)
            {
                try
                {
                    var baseRow = $"| {lr.MicrobenchmarkName} | {lr.BaselineRunName} | {lr.ComparandRunName} | {Math.Round(lr.Baseline.Statistics.Mean.Value, 2)} | {Math.Round(lr.Comparand.Statistics.Mean.Value, 2)} | {Math.Round(lr.MeanDiff, 2)}| {Math.Round(lr.MeanDiffPerc, 2)}|";

                    if (configuration.Output.Columns != null)
                    {
                        foreach (var column in configuration.Output.Columns)
                        {
                            if (!lr.Baseline.OtherMetrics.TryGetValue(column, out double? baselineValue))
                            {
                                lr.Baseline.OtherMetrics[column] = baselineValue = API.GCProcessData.LookupAggregateCalculation(column, lr.Baseline.GCData) ?? MicrobenchmarkResult.LookupStatisticsCalculation(column, lr.Baseline);
                            }
                            string baselineResult = baselineValue.HasValue ? Math.Round(baselineValue.Value, 4).ToString() : string.Empty;

                            if (!lr.Comparand.OtherMetrics.TryGetValue(column, out double? comparandValue))
                            {
                                lr.Comparand.OtherMetrics[column] = comparandValue = API.GCProcessData.LookupAggregateCalculation(column, lr.Comparand.GCData) ?? MicrobenchmarkResult.LookupStatisticsCalculation(column, lr.Comparand);
                            }
                            string comparandResult = comparandValue.HasValue ? Math.Round(comparandValue.Value, 4).ToString() : string.Empty;

                            double? delta = baselineValue.HasValue && comparandValue.HasValue ? comparandValue.Value - baselineValue.Value : null;
                            string deltaResult = delta.HasValue ? Math.Round(delta.Value, 4).ToString() : string.Empty;

                            double? deltaPercent = delta.HasValue ? (delta / baselineValue.Value) * 100 : null;
                            string deltaPercentResult = deltaPercent.HasValue ? Math.Round(deltaPercent.Value, 4).ToString() : string.Empty;

                            baseRow += $"{baselineResult} | {comparandResult} | {deltaResult} | {deltaPercentResult} |";
                        }
                    }

                    if (configuration.Output.cpu_columns != null)
                    {
                        foreach (var column in configuration.Output.cpu_columns)
                        {
                            if (!lr.Baseline.OtherMetrics.TryGetValue(column, out double? baselineValue))
                            {
                                lr.Baseline.OtherMetrics[column] = baselineValue = lr.Baseline.CPUData?.GetIncCountForGCMethod(column) ?? null;
                            }
                            string baselineResult = baselineValue.HasValue ? Math.Round(baselineValue.Value, 2).ToString() : string.Empty;

                            if (!lr.Comparand.OtherMetrics.TryGetValue(column, out double? comparandValue))
                            {
                                lr.Comparand.OtherMetrics[column] = comparandValue = lr.Comparand.CPUData?.GetIncCountForGCMethod(column) ?? null;
                            }
                            string comparandResult = comparandValue.HasValue ? Math.Round(comparandValue.Value, 2).ToString() : string.Empty;

                            double? delta = baselineValue.HasValue && comparandValue.HasValue ? comparandValue.Value - baselineValue.Value : null;
                            string deltaResult = delta.HasValue ? Math.Round(delta.Value, 2).ToString() : string.Empty;

                            double? deltaPercent = delta.HasValue ? (delta / baselineValue.Value) * 100 : null;
                            string deltaPercentResult = deltaPercent.HasValue ? Math.Round(deltaPercent.Value, 2).ToString() : string.Empty;

                            baseRow += $"{baselineResult} | {comparandResult} | {deltaResult} | {deltaPercentResult} |";
                        }
                    }

                    sw.WriteLine(baseRow);
                }

                catch (Exception e)
                {
                    Console.WriteLine($"Exception while processing: {lr.MicrobenchmarkName} for {lr.BaselineRunName} x {lr.ComparandRunName}");
                    Console.WriteLine(e.StackTrace);
                }
            }

            // Dispose all the Analyzers now that we are persisting the values.
            foreach (var comparison in comparisons)
            {
                comparison.Baseline?.GCData?.Parent?.Dispose();
                comparison.Baseline?.CPUData?.Parent?.Analyzer?.Dispose();
                comparison.Comparand?.GCData?.Parent?.Dispose();
                comparison.Comparand?.CPUData?.Parent?.Analyzer?.Dispose();
            }
        }
    }
}
