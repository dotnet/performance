using GC.Analysis.API;
using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GC.Infrastructure.Core.Presentation.GCPerfSim
{
    public static class Json
    {
        public static void GenerateComparisonDictionary(ResultItem baseResultItem, ResultItem comparandResultItem, string path)
        {
            Dictionary<string, List<ComparisonResult>> comparisonResultsJson = new();
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

            string key;
            // Large Regressions
            key = "LargeRegressions";
            foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta > 20)))
            {
                comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                comparisonResultsJson[key].Add(r);
            }

            // Large Improvements
            key = "LargeImprovements";
            foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < -20)))
            {
                comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                comparisonResultsJson[key].Add(r);
            }

            // Regressions
            key = "Regressions";
            foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta > 5 && c.PercentageDelta < 20)))
            {
                comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                comparisonResultsJson[key].Add(r);
            }

            // Improvements
            key = "Improvements";
            foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < -5 && c.PercentageDelta > -20)))
            {
                comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                comparisonResultsJson[key].Add(r);
            }

            // Stale Regressions
            key = "StaleRegressions";
            foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta >= 0 && c.PercentageDelta < 5)))
            {
                comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                comparisonResultsJson[key].Add(r);
            }

            // Stale Improvements
            key = "StaleImprovements";
            foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < 0 && c.PercentageDelta > -5)))
            {
                comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                comparisonResultsJson[key].Add(r);
            }

            string json = JsonConvert.SerializeObject(comparisonResultsJson);
            File.WriteAllText(path, json);
        }

        public static void GenerateDictionary(GCPerfSimConfiguration configuration, Dictionary<string, ProcessExecutionDetails> executionDetails, string path)
        {
            string GetExecutionDetailKey(string runName, string corerunName) => $"{runName}.{corerunName}.0";
            string baseCoreRun = configuration.coreruns.First().Key;

            ConcurrentDictionary<string, ConcurrentDictionary<string, ResultItem>> runToCorerunData = AnalyzeTrace.GetTracesFromConfiguration(configuration);

            Dictionary<string, Dictionary<string, List<ComparisonResult>>> allComparisonResultsJson = new();
            foreach (var run in configuration.Runs)
            {
                allComparisonResultsJson[run.Key] = allComparisonResultsJson.GetValueOrDefault(run.Key, new());
                // Baseline.
                string baseExecutionItem = GetExecutionDetailKey(run.Key, baseCoreRun);

                ResultItem baseRunItem = runToCorerunData.TryGetResultItemFromDictionary(run.Key, baseCoreRun); // [run.Key][baseCoreRun];

                List<ComparisonResult> comparisonResults = new();

                foreach (var corerun in configuration.coreruns)
                {
                    if (corerun.Key == baseCoreRun)
                    {
                        continue;
                    }

                    string runKey = GetExecutionDetailKey(run.Key, corerun.Key);

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

                        ComparisonResult result = resultItemComparison.GetComparison(property.Name);
                        comparisonResults.Add(result);
                    }
                }

                Dictionary<string, List<ComparisonResult>> comparisonResultsJson = new();
                string key;

                // Large Regressions
                key = "LargeRegressions";
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta > 20)))
                {
                    comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                    comparisonResultsJson[key].Add(r);
                }

                // Large Improvements
                key = "LargeImprovements";
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < -20)))
                {
                    comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                    comparisonResultsJson[key].Add(r);
                }

                // Regressions
                key = "Regressions";
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta > 5 && c.PercentageDelta < 20)))
                {
                    comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                    comparisonResultsJson[key].Add(r);
                }

                // Improvements
                key = "Improvements";
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < -5 && c.PercentageDelta > -20)))
                {
                    comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                    comparisonResultsJson[key].Add(r);
                }

                // Stale Regressions
                key = "StaleRegressions";
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta >= 0 && c.PercentageDelta < 5)))
                {
                    comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                    comparisonResultsJson[key].Add(r);
                }

                // Stale Improvements
                key = "StaleImprovements";
                foreach (var r in GoodLinq.Where(comparisonResults, (c => c.PercentageDelta < 0 && c.PercentageDelta > -5)))
                {
                    comparisonResultsJson[key] = comparisonResultsJson.GetValueOrDefault(key, new List<ComparisonResult>());
                    comparisonResultsJson[key].Add(r);
                }

                allComparisonResultsJson[run.Key] = comparisonResultsJson;
            }

            string json = JsonConvert.SerializeObject(allComparisonResultsJson);
            File.WriteAllText(path, json);
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
    }
}
