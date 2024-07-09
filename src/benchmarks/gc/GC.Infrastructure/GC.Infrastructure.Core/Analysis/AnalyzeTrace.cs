using GC.Analysis.API;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Presentation.GCPerfSim;
using System.Collections.Concurrent;

namespace GC.Infrastructure.Core.Analysis
{
    public static class AnalyzeTrace
    {
        public static string GetExecutionDetailKeyForGCPerfSim(string runName, string corerunName) => $"{runName}.{corerunName}.0";

        public static GCProcessData? GetGCProcessDataForGCPerfSim(Analyzer analyzer)
        {
            GCProcessData? p = null;

            // The crank traces are targeted to the particular process.
            if (analyzer.TraceLogPath.EndsWith("nettrace"))
            {
                p = analyzer.AllGCProcessData.First().Value.First();
            }

            else // ETL* traces.
            {
                p = analyzer.GetProcessGCData("corerun").FirstOrDefault();
                if (p == null)
                {
                    p = analyzer.GetProcessGCData("GCPerfSim").FirstOrDefault();
                    if (p == null)
                    {
                        return p;
                    }
                }
            }

            return p;
        }

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, ResultItem>> GetTracesFromConfiguration(GCPerfSimConfiguration configuration)
        {
            ConcurrentDictionary<string, ConcurrentDictionary<string, ResultItem>> runToCorerunData = new();
            List<ComparisonResult> allComparisonResults = new();

            // Concurrently get all the traces.
            Parallel.ForEach(configuration.Runs, run =>
            {
                Dictionary<string, Analyzer> analyzers = AnalyzerManager.GetAllAnalyzers(Path.Combine(configuration.Output.Path, run.Key));

                foreach (var analyzer in analyzers)
                {
                    // Format: runName.corerunName.iterationIdx
                    string runName = run.Key;
                    string[] splitName = analyzer.Key.Split(".", StringSplitOptions.RemoveEmptyEntries);
                    string corerunName = splitName[1];
                    string idx = splitName[2];

                    // Only serialize the 0th iteration for now.
                    if (idx == "0")
                    {
                        if (!runToCorerunData.TryGetValue(runName, out var d))
                        {
                            d = runToCorerunData[runName] = new();
                        }

                        if (!d.TryGetValue(corerunName, out var processData))
                        {
                            GCProcessData? p = GetGCProcessDataForGCPerfSim(analyzer.Value);
                            string corerun = Path.GetFileNameWithoutExtension(configuration.Output.Path);

                            // If the process isn't found in the trace, substitute a null ResultItem.
                            if (p == null)
                            {
                                d[corerunName] = processData = ResultItem.GetNullItem(run.Key, corerun);
                            }

                            else
                            {
                                d[corerunName] = processData = new ResultItem(p, run.Key, corerun);
                            }
                        }
                    }
                }
            });

            return runToCorerunData;
        }

        public static IReadOnlyList<ComparisonResult> GetComparisons(GCPerfSimConfiguration configuration, Func<GCPerfSimConfiguration, string> keyFunctor)
        {
            ConcurrentDictionary<string, ConcurrentDictionary<string, ResultItem>> runToCorerunData = GetTracesFromConfiguration(configuration);
            List<ComparisonResult> allComparisonResults = new();

            // First corerun is the base.
            // TODO: Change this.
            string baseCoreRun = keyFunctor(configuration); // configuration.coreruns.First().Key;

            foreach (var run in configuration.Runs)
            {
                // Baseline.
                string baseExecutionItem = GetExecutionDetailKeyForGCPerfSim(run.Key, baseCoreRun);
                ResultItem baseRunItem = runToCorerunData[run.Key][baseCoreRun];

                List<ComparisonResult> comparisonResults = new();

                foreach (var corerun in configuration.coreruns)
                {
                    if (corerun.Key == baseCoreRun)
                    {
                        continue;
                    }

                    ResultItem comparandRunItem = runToCorerunData[run.Key][corerun.Key];
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
            }

            return allComparisonResults;
        }

        public static Dictionary<string, ComparisonResult> GetComparisons(string baselinePath, string runPath)
        {
            Analyzer baselineAnalyzer = AnalyzerManager.GetAnalyzer(baselinePath);
            GCProcessData? baselineProcessData = GetGCProcessDataForGCPerfSim(baselineAnalyzer);
            ResultItem baselineResultItem = (baselineProcessData != null)
                                             ? new ResultItem(baselineProcessData, baselinePath, baselinePath)
                                             : ResultItem.GetNullItem(baselinePath, baselinePath);

            Analyzer runAnalyzer = AnalyzerManager.GetAnalyzer(runPath);
            GCProcessData? runProcessData = GetGCProcessDataForGCPerfSim(runAnalyzer);
            ResultItem runResultItem = (runProcessData != null)
                                       ? new ResultItem(runProcessData, runPath, runPath)
                                       : ResultItem.GetNullItem(runPath, runPath);

            Dictionary<string, ComparisonResult> allComparisonResults = new();
            foreach (var property in typeof(ResultItem).GetProperties())
            {
                if (property.PropertyType != typeof(double))
                {
                    continue;
                }

                var resultItemComparison = new ResultItemComparison(baselineResultItem, runResultItem);
                ComparisonResult result = resultItemComparison.GetComparison(property.Name);
                allComparisonResults[property.Name] = result;
            }

            return allComparisonResults;
        }
    }
}
