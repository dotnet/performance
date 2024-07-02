using Microsoft.Diagnostics.Symbols;

namespace GC.Analysis.API
{
    public static class CPUAnalysisExt
    {
        internal static List<CPUInfo> ConvertToCPUInfo(this CPUProcessData data, SortedDictionary<int, float> perGCCounts)
        {
            List<CPUInfo> cpuInfos = new List<CPUInfo>();
            GCProcessData gcData = data.Parent.Analyzer.GetProcessGCData(data.ProcessName).Single(g => g.ProcessID == data.ProcessID);
            foreach (var gc in gcData.GCs)
            {
                if (perGCCounts.TryGetValue(gc.Number, out float f))
                {
                    CPUInfo info = new CPUInfo(gc, f);
                    cpuInfos.Add(info);
                }
            }

            return cpuInfos;
        }

        public static void AddCPUAnalysis(this Analyzer analyzer, string yamlPath = "./GC.Analysis.API/CPUAnalysis/DefaultMethods.yaml", string symbolLogFile = "", string symbolPath = "")
        {
            analyzer.CPUAnalyzer = new CPUAnalyzer(analyzer, yamlPath, symbolLogFile, symbolPath);
        }

        public static List<CPUProcessData> GetCPUDataForProcessName(this CPUAnalyzer analyzer, string processName)
        {
            if (!analyzer.CPUAnalyzers.TryGetValue(processName, out var vals))
            {
                return new List<CPUProcessData>();
            }

            else
            {
                return vals;
            }
        }

        public static List<CPUInfo> GetPerGCMethodCost(this CPUProcessData processData, string methodName, string caller, bool isInclusiveCount = true)
        {
            if (processData is null)
            {
                throw new ArgumentNullException(nameof(processData));
            }

            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException($"'{nameof(methodName)}' cannot be null or empty.", nameof(methodName));
            }

            SortedDictionary<int, float> perGCCost = new();

            if (processData.PerGCData.TryGetValue(methodName, out var data))
            {
                // For each GC, for each thread, sum up the counts.
                foreach (var perGC in data)
                {
                    int gcNumber = perGC.Key;
                    if (!perGCCost.TryGetValue(gcNumber, out var value))
                    {
                        perGCCost[gcNumber] = 0;
                    }

                    foreach (var thread in perGC.Value)
                    {
                        foreach (var c in thread.Value)
                        {
                            if (c.Callers.Any(gc => gc.Contains(caller)))
                            {
                                perGCCost[gcNumber] += isInclusiveCount ? c.InclusiveCount : c.ExclusiveCount;
                            }
                        }
                    }
                }
            }

            // Special handling of the gc_heap::plan_phase.
            if (methodName == "gc_heap::plan_phase")
            {
                SortedDictionary<int, float> perGCCostForPlanPhase = new();
                var relocatePhase = GetPerGCMethodCost(processData, "gc_heap::relocate_phase", isInclusiveCount);
                var compactPhase = GetPerGCMethodCost(processData, "gc_heap::compact_phase", isInclusiveCount);

                foreach (var gc in perGCCost)
                {
                    if (!perGCCostForPlanPhase.TryGetValue(gc.Key, out var planCount))
                    {
                        perGCCostForPlanPhase[gc.Key] = gc.Value;
                    }

                    var relocateVal = relocatePhase.SingleOrDefault(g => g.GC.Number == gc.Key);
                    if (relocateVal != null)
                    {
                        perGCCostForPlanPhase[gc.Key] -= relocateVal.Count;
                    }

                    var compactVal = compactPhase.SingleOrDefault(g => g.GC.Number == gc.Key);
                    if (compactVal != null)
                    {
                        perGCCostForPlanPhase[gc.Key] -= compactVal.Count;
                    }
                }

                return processData.ConvertToCPUInfo(perGCCostForPlanPhase);
            }

            return processData.ConvertToCPUInfo(perGCCost);
        }

        public static List<CPUInfo> GetPerGCMethodCost(this CPUProcessData processData, string methodName, bool isInclusiveCount = true)
        {
            if (processData is null)
            {
                throw new ArgumentNullException(nameof(processData));
            }

            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException($"'{nameof(methodName)}' cannot be null or empty.", nameof(methodName));
            }

            SortedDictionary<int, float> perGCCost = new();

            if (processData.OverallData.TryGetValue(methodName, out var data))
            {
                // For each GC, for each thread, sum up the counts.
                foreach (var perGC in data)
                {
                    int gcNumber = perGC.Key;
                    perGCCost[gcNumber] = isInclusiveCount ? perGC.Value.InclusiveCount : perGC.Value.ExclusiveCount;
                }
            }

            // Special handling of the gc_heap::plan_phase.
            if (methodName == "gc_heap::plan_phase")
            {
                SortedDictionary<int, float> perGCCostForPlanPhase = new();
                var relocatePhase = GetPerGCMethodCost(processData, "gc_heap::relocate_phase", isInclusiveCount);
                var compactPhase = GetPerGCMethodCost(processData, "gc_heap::compact_phase", isInclusiveCount);
                var makeFreeListsPhase = GetPerGCMethodCost(processData, "gc_heap::make_free_lists", isInclusiveCount);

                foreach (var gc in perGCCost)
                {
                    if (!perGCCostForPlanPhase.TryGetValue(gc.Key, out var planCount))
                    {
                        perGCCostForPlanPhase[gc.Key] = gc.Value;
                    }

                    var relocateVal = relocatePhase.SingleOrDefault(g => g.GC.Number == gc.Key);
                    if (relocateVal != null)
                    {
                        perGCCostForPlanPhase[gc.Key] -= relocateVal.Count;
                    }

                    var compactVal = compactPhase.SingleOrDefault(g => g.GC.Number == gc.Key);
                    if (compactVal != null)
                    {
                        perGCCostForPlanPhase[gc.Key] -= compactVal.Count;
                    }

                    var makeFreeList = makeFreeListsPhase.SingleOrDefault(g => g.GC.Number == gc.Key);
                    if (makeFreeList != null)
                    {
                        perGCCostForPlanPhase[gc.Key] -= makeFreeList.Count;
                    }
                }

                return processData.ConvertToCPUInfo(perGCCostForPlanPhase);
            }

            return processData.ConvertToCPUInfo(perGCCost);
        }

        public static List<CPUInfo> GetPerGCMethodCost(this CPUProcessData processData,
                                                       string methodName,
                                                       HashSet<int> gcsToConsider,
                                                       bool isInclusiveCount = true)
        {
            if (processData is null)
            {
                throw new ArgumentNullException(nameof(processData));
            }

            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException($"'{nameof(methodName)}' cannot be null or empty.", nameof(methodName));
            }

            SortedDictionary<int, float> perGCCost = new();

            if (processData.OverallData.TryGetValue(methodName, out var data))
            {
                foreach (var perGC in data)
                {
                    int gcNumber = perGC.Key;
                    if (!gcsToConsider.Contains(gcNumber))
                    {
                        continue;
                    }

                    perGCCost[gcNumber] = isInclusiveCount ? perGC.Value.InclusiveCount : perGC.Value.ExclusiveCount;
                }
            }

            // Special handling of the gc_heap::plan_phase.
            if (methodName == "gc_heap::plan_phase")
            {
                SortedDictionary<int, float> perGCCostForPlanPhase = new();
                var relocatePhase = GetPerGCMethodCost(processData, "gc_heap::relocate_phase", isInclusiveCount);
                var compactPhase = GetPerGCMethodCost(processData, "gc_heap::compact_phase", isInclusiveCount);
                var makeFreeListsPhase = GetPerGCMethodCost(processData, "gc_heap::make_free_lists", isInclusiveCount);

                foreach (var gc in perGCCost)
                {
                    if (!perGCCostForPlanPhase.TryGetValue(gc.Key, out var planCount))
                    {
                        perGCCostForPlanPhase[gc.Key] = gc.Value;
                    }

                    var relocateVal = relocatePhase.SingleOrDefault(g => g.GC.Number == gc.Key);
                    if (relocateVal != null)
                    {
                        perGCCostForPlanPhase[gc.Key] -= relocateVal.Count;
                    }

                    var compactVal = compactPhase.SingleOrDefault(g => g.GC.Number == gc.Key);
                    if (compactVal != null)
                    {
                        perGCCostForPlanPhase[gc.Key] -= compactVal.Count;
                    }

                    var makeFreeList = makeFreeListsPhase.SingleOrDefault(g => g.GC.Number == gc.Key);
                    if (makeFreeList != null)
                    {
                        perGCCostForPlanPhase[gc.Key] -= makeFreeList.Count;
                    }
                }

                return processData.ConvertToCPUInfo(perGCCostForPlanPhase);
            }

            return processData.ConvertToCPUInfo(perGCCost);
        }

        public static void SetSourcePath(this CPUProcessData processData, string sourcePath)
            => processData.StackView.SymbolReader.SourcePath = sourcePath;
        public static string Annotate(this CPUProcessData processData, string methodName)
        {
            SourceLocation location = processData.StackView.GetSourceLocation(methodName, out var @out);
            SortedDictionary<int, float> metricsOnLine = @out;
            return processData.StackView.Annotate(metricsOnLine, location);
        }
    }
}
