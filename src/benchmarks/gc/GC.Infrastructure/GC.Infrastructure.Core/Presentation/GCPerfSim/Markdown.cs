using GC.Analysis.API;
using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using System.Collections.Concurrent;
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
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta > 20)))
                {
                    sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Large Improvements (>20%)");
                sb.AppendLine();

                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < -20)))
                {
                    sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Regressions (5% - 20%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta > 5 && c.PercentageDelta < 20)))
                {
                    sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Improvements (5 - 20%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < -5 && c.PercentageDelta > -20)))
                {
                    sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Stale Regression (< 5%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta >= 0 && c.PercentageDelta < 5)))
                {
                    sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }
                sb.AppendLine();

                sb.AppendLine("#### Stale Improvements (< 5%)");
                sb.AppendLine();
                sb.AppendLine($" | Metric | Base | Comparand | Δ%  |  Δ |");
                sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < 0 && c.PercentageDelta > -5)))
                {
                    sb.AppendLine($"|{r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                }

                sb.AppendLine();
                sw.WriteLine("\n");
                sw.WriteLine(sb.ToString());
            }
        }

        private static ResultItem TryGetResultItemFromDictionary(this ConcurrentDictionary<string, ConcurrentDictionary<string, ResultItem>> cache, string run, string corerun)
        {
            if (cache.TryGetValue(run, out var r) && r.TryGetValue(corerun, out var result))
            {
                return result;
            }

            else
            {
                return ResultItem.GetNullItem(run, corerun);
            }
        }

        public static IReadOnlyList<ComparisonResult> GenerateTable(GCPerfSimConfiguration configuration, Dictionary<string, ProcessExecutionDetails> executionDetails, string path)
        {
            ConcurrentDictionary<string, ConcurrentDictionary<string, ResultItem>> runToCorerunData = AnalyzeTrace.GetTracesFromConfiguration(configuration);

            List<ComparisonResult> allComparisonResults = new();

            using (StreamWriter sw = new StreamWriter(path))
            {
                StringBuilder sb = new();

                // First corerun is the base.
                string baseCoreRun = configuration.coreruns.First().Key;

                string GetExecutionDetailKey(string runName, string corerunName) => $"{runName}.{corerunName}.0";

                sw.WriteLine("# Summary");
                sw.WriteLine();

                sw.WriteLine("| Name | *ExecutionTime (MSec)* - base | comparand | Δ% | *% GC Pause Time* - base | comparand | Δ% |");
                sw.WriteLine("| ---- | ---------------------------  | --------- | ---| ------------------------------- | --------- | ---|");
                // Go through all the runs, get the baseline and the comparand values.

                foreach (var run in configuration.Runs)
                {
                    string runName = run.Key;
                    ResultItem baseResultItem = runToCorerunData.TryGetResultItemFromDictionary(runName, baseCoreRun);

                    double metric1_Base = Math.Round(baseResultItem.ExecutionTimeMSec, 2);
                    double metric2_Base = Math.Round(baseResultItem.PctTimePausedInGC, 2);

                    double metric1_Comparand = double.NaN;
                    double metric2_Comparand = double.NaN;

                    foreach (var corerun in configuration.coreruns)
                    {
                        if (baseCoreRun == corerun.Key)
                        {
                            continue;
                        }

                        ResultItem comparandResultItem = runToCorerunData.TryGetResultItemFromDictionary(runName, corerun.Key);   // [runName][corerun.Key];
                        metric1_Comparand = comparandResultItem.ExecutionTimeMSec;
                        metric2_Comparand = comparandResultItem.PctTimePausedInGC;
                    }

                    sw.WriteLine($"| {runName} | {metric1_Base:N2} | {metric1_Comparand:N2} | {((metric1_Comparand - metric1_Base) / metric1_Base) * 100:N2} | {metric2_Base:N2} |  {metric2_Comparand:N2} | {((metric2_Comparand - metric2_Base) / metric2_Base * 100):N2}| ");
                }

                sw.WriteLine();

                // HeapSizeBeforeMB
                sw.WriteLine("| Name | *Mean Heap Size Before (MB)* - base | comparand | Δ% |");
                sw.WriteLine("| ---- | ---------------------------  | --------- | ---|");
                // Go through all the runs, get the baseline and the comparand values.
                foreach (var run in configuration.Runs)
                {
                    string runName = run.Key;
                    ResultItem baseResultItem = runToCorerunData.TryGetResultItemFromDictionary(runName, baseCoreRun);  // [runName][baseCoreRun];

                    double metric1_Base = Math.Round(baseResultItem.HeapSizeBeforeMB_Mean, 2);

                    double metric1_Comparand = double.NaN;

                    foreach (var corerun in configuration.coreruns)
                    {
                        if (baseCoreRun == corerun.Key)
                        {
                            continue;
                        }

                        ResultItem comparandResultItem = runToCorerunData.TryGetResultItemFromDictionary(runName, corerun.Key);
                        metric1_Comparand = Math.Round(comparandResultItem.HeapSizeBeforeMB_Mean, 2);
                    }

                    sw.WriteLine($"| {runName} | {metric1_Base:N2} | {metric1_Comparand:N2} | {((metric1_Comparand - metric1_Base) / metric1_Base) * 100:N2} |");
                }
                sw.WriteLine();

                sw.WriteLine("| Name | *Mean Ephemeral Pause (MSec)* - base | comparand | Δ% |");
                sw.WriteLine("| ---- | ---------------------------  | --------- | ---|");
                // Go through all the runs, get the baseline and the comparand values.
                foreach (var run in configuration.Runs)
                {
                    string runName = run.Key;
                    ResultItem baseResultItem = runToCorerunData.TryGetResultItemFromDictionary(runName, baseCoreRun);

                    double metric1Base = Math.Round(baseResultItem.PauseDurationMSec_MeanWhereIsEphemeral, 2);
                    double metric1_Comparand = double.NaN;

                    foreach (var corerun in configuration.coreruns)
                    {
                        if (baseCoreRun == corerun.Key)
                        {
                            continue;
                        }

                        ResultItem comparandResultItem = runToCorerunData.TryGetResultItemFromDictionary(runName, corerun.Key);
                        metric1_Comparand = comparandResultItem.PauseDurationMSec_MeanWhereIsEphemeral;
                    }

                    sw.WriteLine($"| {runName} | {metric1Base:N2} | {metric1_Comparand:N2} | {((metric1_Comparand - metric1Base) / metric1Base) * 100:N2} |");
                }

                sw.WriteLine();

                // PauseDurationMSec_95PWhereIsBackground and PauseDurationMSec_95PWhereIsBlockingGen2
                sw.WriteLine($"| Name | *Mean BGC Pause (MSec)* - base | comparand | Δ% | *Mean Full Blocking GC Pause (MSec)* - base | comparand | Δ% |");
                sw.WriteLine("| ---- | ---------------------------  | --------- | ---| ------------------------------- | --------- | ---|");

                // Go through all the runs, get the baseline and the comparand values.
                foreach (var run in configuration.Runs)
                {
                    string runName = run.Key;
                    ResultItem baseResultItem = runToCorerunData.TryGetResultItemFromDictionary(runName, baseCoreRun);

                    double metric1_Base = baseResultItem.PauseDurationMSec_MeanWhereIsBackground;
                    double metric2_Base = baseResultItem.PauseDurationMSec_MeanWhereIsBlockingGen2;

                    double metric1_Comparand = double.NaN;
                    double metric2_Comparand = double.NaN;

                    foreach (var corerun in configuration.coreruns)
                    {
                        if (baseCoreRun == corerun.Key)
                        {
                            continue;
                        }

                        ResultItem comparandResultItem = runToCorerunData.TryGetResultItemFromDictionary(runName, corerun.Key);
                        metric1_Comparand = Math.Round(comparandResultItem.PauseDurationMSec_MeanWhereIsBackground, 2);
                        metric2_Comparand = Math.Round(comparandResultItem.PauseDurationMSec_MeanWhereIsBlockingGen2, 2);
                    }

                    sw.WriteLine($"| {runName} | {metric1_Base:N2} | {metric1_Comparand:N2} | {((metric1_Comparand - metric1_Base) / metric1_Base) * 100:N2} | {metric2_Base} |  {metric2_Comparand:N2} | {((metric2_Comparand - metric2_Base) / metric2_Base * 100):N2}| ");
                }
                sb.AppendLine();

                sb.AppendLine("# Individual Results");

                foreach (var run in configuration.Runs)
                {
                    sb.AppendLine($"## {run.Key}");
                    sb.AppendLine();

                    sb.AppendLine($"### Repro Steps:");

                    sb.AppendLine($"#### {baseCoreRun}: ");

                    // Baseline.
                    string baseExecutionItem = GetExecutionDetailKey(run.Key, baseCoreRun);
                    if (executionDetails.TryGetValue(baseExecutionItem, out var resultItem))
                    {
                        ProcessExecutionDetails processExecutionDetails = executionDetails[$"{run.Key}.{baseCoreRun}.0"];
                        ProcessExecutionDetails baseRunDetails = processExecutionDetails;
                        foreach (var env in baseRunDetails.EnvironmentVariables)
                        {
                            sb.AppendLine($" ```set {env.Key}={env.Value}```\n");
                        }
                        sb.AppendLine($"\n```{baseRunDetails.CommandlineArgs}```\n");
                    }

                    ResultItem baseRunItem = runToCorerunData.TryGetResultItemFromDictionary(run.Key, baseCoreRun); // [run.Key][baseCoreRun];

                    List<ComparisonResult> comparisonResults = new();

                    foreach (var corerun in configuration.coreruns)
                    {
                        if (corerun.Key == baseCoreRun)
                        {
                            continue;
                        }

                        sb.AppendLine($"#### {corerun.Key}: ");

                        string runKey = GetExecutionDetailKey(run.Key, corerun.Key);
                        if (executionDetails.TryGetValue(runKey, out var bped))
                        {
                            ProcessExecutionDetails runDetails = executionDetails[$"{run.Key}.{corerun.Key}.0"];
                            foreach (var env in runDetails.EnvironmentVariables)
                            {
                                sb.AppendLine($" ```set {env.Key}={env.Value}```\n");
                            }
                            sb.AppendLine($"\n```{runDetails.CommandlineArgs}```\n");
                        }

                        ResultItem comparandRunItem = runToCorerunData.TryGetResultItemFromDictionary(run.Key, corerun.Key);
                        var resultItemComparison = new ResultItemComparison(baseRunItem, comparandRunItem);

                        HashSet<string> requestedPropertyNames = new HashSet<string>(GoodLinq.Select(configuration.Output.Columns, (c => c.ToLowerInvariant().Replace(" ", "").Replace("(", ")").Replace(")", ""))));

                        foreach (var property in typeof(ResultItem).GetProperties())
                        {
                            if (property.PropertyType != typeof(double))
                            {
                                continue;
                            }

                            string propertyNameToCheck = property.Name.ToLowerInvariant();

                            // TODO: Add the property filter logic back in.
                            /*
                            if (!requestedPropertyNames.Contains(propertyNameToCheck))
                            {
                                //continue;
                            }
                            */

                            ComparisonResult result = resultItemComparison.GetComparison(property.Name);
                            comparisonResults.Add(result);
                            allComparisonResults.Add(result);
                        }
                    }

                    sb.AppendLine("#### Large Regressions (>20%)");
                    sb.AppendLine();

                    sb.AppendLine($" | Metric | Base | {run.Key} | Δ%  |  Δ |");
                    sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                    foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta > 20)))
                    {
                        sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                    }
                    sb.AppendLine();

                    sb.AppendLine("#### Large Improvements (>20%)");
                    sb.AppendLine();

                    sb.AppendLine($" | Metric | Base | {run.Key} | Δ%  |  Δ |");
                    sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                    foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < -20)))
                    {
                        sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                    }
                    sb.AppendLine();

                    sb.AppendLine("#### Regressions (5% - 20%)");
                    sb.AppendLine();
                    sb.AppendLine($" | Metric | Base | {run.Key} | Δ%  |  Δ |");
                    sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                    foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta > 5 && c.PercentageDelta < 20)))
                    {
                        sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                    }
                    sb.AppendLine();

                    sb.AppendLine("#### Improvements (5 - 20%)");
                    sb.AppendLine();
                    sb.AppendLine($" | Metric | Base | {run.Key} | Δ%  |  Δ |");
                    sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                    foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < -5 && c.PercentageDelta > -20)))
                    {
                        sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                    }
                    sb.AppendLine();

                    sb.AppendLine("#### Stale Regression (< 5%)");
                    sb.AppendLine();
                    sb.AppendLine($" | Metric | Base | {run.Key} | Δ%  |  Δ |");
                    sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                    foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta >= 0 && c.PercentageDelta < 5)))
                    {
                        sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                    }
                    sb.AppendLine();

                    sb.AppendLine("#### Stale Improvements (< 5%)");
                    sb.AppendLine();
                    sb.AppendLine($" | Metric | Base | {run.Key} | Δ%  |  Δ |");
                    sb.AppendLine($" | -----  | ---- | ------  | ---  |  --- |");
                    foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < 0 && c.PercentageDelta > -5)))
                    {
                        sb.AppendLine($"| {r.MetricName} | {r.BaselineMetric:N2} | {r.ComparandMetric:N2} | {r.PercentageDelta:N2} | {r.Delta:N2} |");
                    }
                    sb.AppendLine();
                }

                // Add the Summary.
                sw.AddIncompleteTestsSection(executionDetails);

                sw.WriteLine("\n");
                sw.WriteLine(sb.ToString());
            }

            return allComparisonResults;
        }
    }
}
