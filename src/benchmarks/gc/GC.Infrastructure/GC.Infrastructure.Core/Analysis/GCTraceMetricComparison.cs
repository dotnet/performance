using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Presentation.GCPerfSim;
using System.Collections.Concurrent;
using API = GC.Analysis.API;

namespace GC.Infrastructure.Core.Analysis
{
    public static class GCTraceMetricComparison
    {
        private static readonly int _CPUCount = System.Environment.ProcessorCount;

        public static IReadOnlyCollection<Tuple<KeyValuePair<string, Run>, CoreRunInfo, string>>
            GetAllTraceFiles(GCPerfSimConfiguration configuration)
        {
            ConcurrentBag<Tuple<KeyValuePair<string, Run>, CoreRunInfo, string>> allTraceFiles = new();

            Parallel.ForEach(configuration.coreruns, corerunKVP =>
            {
                var outputPath = Path.Combine(configuration.Output.Path, corerunKVP.Key);
                CoreRunInfo corerunInfo = corerunKVP.Value;
                corerunInfo.Name = corerunInfo.Name ?? corerunKVP.Key;
                Parallel.ForEach(configuration.Runs, runKVP =>
                {
                    var runName = runKVP.Key;
                    var pattern = OperatingSystem.IsWindows() ? $"{runName}.{corerunInfo.Name}.*.etl.zip" : $"{runName}.{corerunInfo.Name}.*.nettrace";
                    var candidateFiles = Directory.GetFiles(outputPath, pattern, SearchOption.AllDirectories);
                    foreach (var filePath in candidateFiles)
                    {
                        allTraceFiles.Add(Tuple.Create(runKVP, corerunInfo, filePath));
                    }
                });
            });


            return allTraceFiles;
        }

        public static IReadOnlyCollection<GCTraceMetrics>
            AnalyzeGCPerfsimResults(GCPerfSimConfiguration configuration,
                                    IReadOnlyCollection<Tuple<KeyValuePair<string, Run>, CoreRunInfo, string>> allTraceFiles)
        {
            ConcurrentBag<GCTraceMetrics> allGCPerfsimResults = new();
            string configurationName = Path.GetFileNameWithoutExtension(configuration.Output.Path);
            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = _CPUCount * 2
            };

            Parallel.ForEach(allTraceFiles, options, t =>
            {
                var runKVP = t.Item1;
                var coreRunInfo = t.Item2;
                var traceFilePath = t.Item3;

                using (var analyzer = API.AnalyzerManager.GetAnalyzer(traceFilePath))
                {
                    var p = AnalyzeTrace.GetGCProcessDataForGCPerfSim(analyzer);
                    if (p != null)
                    {
                        var gcTraceMetrics = new GCTraceMetrics(p, runKVP.Key, configurationName, parent: coreRunInfo);
                        allGCPerfsimResults.Add(gcTraceMetrics);
                    }
                }
            });

            return allGCPerfsimResults;
        }

        public static GCTraceMetricComparisonResult CompareGCTraceMetric(IEnumerable<GCTraceMetrics> baselines, IEnumerable<GCTraceMetrics> comparands, string nameOfMetric)
            => new GCTraceMetricComparisonResult(baselines, comparands, nameOfMetric);

        public static IReadOnlyCollection<GCTraceMetricComparisonResult>
            CompareGCTraceMetrics(GCPerfSimConfiguration configuration, IEnumerable<GCTraceMetrics> allGCPerfsimResults)
        {
            ConcurrentBag<GCTraceMetricComparisonResult> allComparisonResults = new();
            var GCPerfsimResultsGroupedByRun = allGCPerfsimResults.GroupBy(m => m.RunName);
            var requestedPropertyNames = new HashSet<string>(
                configuration.Output.Columns
                .Select(c => c.ToLowerInvariant().Replace(" ", "").Replace("(", ")").Replace(")", ""))
            );
            Parallel.ForEach(GCPerfsimResultsGroupedByRun, runGroup =>
            {
                var runName = runGroup.Key;
                var baselines = runGroup.Where(m => m.Parent!.is_baseline);
                var comparands = runGroup.Where(m => !m.Parent!.is_baseline);

                foreach (var property in typeof(GCTraceMetrics).GetProperties())
                {
                    if (property.PropertyType != typeof(double))
                    {
                        continue;
                    }

                    string propertyNameToCheck = property.Name.ToLowerInvariant();

                    if (!requestedPropertyNames.Contains(propertyNameToCheck))
                    {
                        continue;
                    }

                    var comparisonResult = CompareGCTraceMetric(baselines, comparands, property.Name);
                    allComparisonResults.Add(comparisonResult);
                }
            });

            return allComparisonResults;
        }

        public static IReadOnlyCollection<GCTraceMetricComparisonResults> GroupComparisonResultsByRunName(IEnumerable<GCTraceMetricComparisonResult> allComparisonResults)
        {
            List<GCTraceMetricComparisonResults> groupedResults = new();

            allComparisonResults
                .GroupBy(r => r.RunName)
                .ToList()
                .ForEach(g =>
                {
                    var runName = g.Key;
                    groupedResults.Add(new GCTraceMetricComparisonResults(runName, g.ToList()));
                });
            return groupedResults;
        }
    }
}