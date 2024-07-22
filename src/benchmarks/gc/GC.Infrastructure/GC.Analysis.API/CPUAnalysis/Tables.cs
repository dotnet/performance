using Microsoft.Data.Analysis;

namespace GC.Analysis.API
{
    public static class Tables
    {
        internal static readonly IEnumerable<string> phases = new List<string>
        {
            "gc_heap::mark_phase",
            "gc_heap::plan_phase",
            "gc_heap::relocate_phase",
            "gc_heap::compact_phase",
            "gc_heap::make_free_lists",
            "gc_heap::background_mark_phase",
            "gc_heap::background_sweep"
        };

        public static DataFrame GetCPUDataForGCMethods(this CPUProcessData cpuData, IEnumerable<string> methods)
        {
            StringDataFrameColumn methodName = new StringDataFrameColumn("Method Name");
            DoubleDataFrameColumn inclusiveCount = new DoubleDataFrameColumn("Inclusive Count");
            DoubleDataFrameColumn exclusiveCount = new DoubleDataFrameColumn("Exclusive Count");

            foreach (var method in methods)
            {
                // Assumes, symbols are all well resolved.
                var node = cpuData.StackView.GetAllCPUDataByName(method);
                methodName.Append(node.Method);
                inclusiveCount.Append(node.InclusiveCount);
                exclusiveCount.Append(node.ExclusiveCount);
            }

            return new DataFrame(methodName, inclusiveCount, exclusiveCount).OrderByDescending("Exclusive Count");
        }

        public static DataFrame GetCPUDataForGCMethod(this CPUProcessData cpuData, string method)
        {
            StringDataFrameColumn methodName = new StringDataFrameColumn("Method Name");
            DoubleDataFrameColumn inclusiveCount = new DoubleDataFrameColumn("Inclusive Count");
            DoubleDataFrameColumn exclusiveCount = new DoubleDataFrameColumn("Exclusive Count");

            var node = cpuData.StackView.GetAllCPUDataByName(method);
            methodName.Append(node.Method);
            inclusiveCount.Append(node.InclusiveCount);
            exclusiveCount.Append(node.ExclusiveCount);

            return new DataFrame(methodName, inclusiveCount, exclusiveCount).OrderByDescending("Exclusive Count");
        }

        public static double GetIncCountForGCMethod(this CPUProcessData cpuData, string method)
        {
            var node = cpuData.StackView.GetAllCPUDataByName(method);
            return node.InclusiveCount;
        }

        public static DataFrame GetPerPhaseSummary(this CPUProcessData cpuData, bool showInclusiveCount = true)
        {
            StringDataFrameColumn phaseName = new("Phases");
            DoubleDataFrameColumn counts = new(showInclusiveCount ? "Inclusive Count" : "Exclusive Count");
            DoubleDataFrameColumn percentage = new("%");

            Dictionary<string, double> phaseToCount = new();
            double total = 0;

            foreach (var phase in phases)
            {
                double sum = 0;
                if (cpuData.MethodToData.TryGetValue(phase, out var data))
                {
                    sum += showInclusiveCount ? data.InclusiveCount : data.ExclusiveCount;
                }
                phaseToCount[phase] = sum;
                total += sum;
            }

            phaseToCount["gc_heap::plan_phase"] -= phaseToCount["gc_heap::relocate_phase"] + phaseToCount["gc_heap::compact_phase"];
            total -= phaseToCount["gc_heap::relocate_phase"] + phaseToCount["gc_heap::compact_phase"];

            foreach (var phase in phaseToCount)
            {
                phaseName.Append(phase.Key);
                counts.Append(phase.Value);
                percentage.Append(DataFrameHelpers.Round2((phase.Value / total) * 100));
            }

            return new DataFrame(phaseName, counts, percentage);
        }

        public static DataFrame Compare(this CPUProcessData cpuData, IEnumerable<CPUProcessData> others, bool showInclusiveCount = true)
        {
            StringDataFrameColumn phaseName = new("Phases");
            DoubleDataFrameColumn baselineCounts = new(showInclusiveCount ? "Inclusive Count - Baseline" : "Exclusive Count - Baseline");
            DoubleDataFrameColumn baselinPercentage = new("%");

            Dictionary<string, double> phaseToPercentage = new();
            Dictionary<string, double> phaseToCount = new();
            double total = 0;

            foreach (var phase in phases)
            {
                double sum = 0;
                if (cpuData.MethodToData.TryGetValue(phase, out var data))
                {
                    sum += showInclusiveCount ? data.InclusiveCount : data.ExclusiveCount;
                }
                phaseToCount[phase] = sum;
                total += sum;
            }

            phaseToCount["gc_heap::plan_phase"] -= phaseToCount["gc_heap::relocate_phase"] + phaseToCount["gc_heap::compact_phase"];
            total -= phaseToCount["gc_heap::relocate_phase"] + phaseToCount["gc_heap::compact_phase"];

            foreach (var phase in phaseToCount)
            {
                phaseName.Append(phase.Key);
                baselineCounts.Append(phase.Value);
                double percentage = DataFrameHelpers.Round2((phase.Value / total) * 100);
                phaseToPercentage[phase.Key] = percentage;
                baselinPercentage.Append(percentage);
            }

            List<DataFrameColumn> otherColumns = new();

            foreach (var o in others)
            {
                DoubleDataFrameColumn otherCounts = new(showInclusiveCount ? $"Inclusive Count: {o.ProcessID}" : $"Exclusive Count: {o.ProcessID}");
                DoubleDataFrameColumn otherDiffCount = new($"Diff: {o.ProcessID}");
                DoubleDataFrameColumn otherPercentage = new($"%: {o.ProcessID}");
                DoubleDataFrameColumn otherPercentageDiff = new($"Diff %: {o.ProcessID}");

                Dictionary<string, double> otherPhaseToCount = new();
                double otherTotal = 0;

                foreach (var phase in phases)
                {
                    double sum = 0;
                    if (o.MethodToData.TryGetValue(phase, out var data))
                    {
                        sum += showInclusiveCount ? data.InclusiveCount : data.ExclusiveCount;
                    }
                    otherPhaseToCount[phase] = sum;
                    otherTotal += sum;
                }

                otherPhaseToCount["gc_heap::plan_phase"] -= otherPhaseToCount["gc_heap::relocate_phase"] + otherPhaseToCount["gc_heap::compact_phase"];
                total -= otherPhaseToCount["gc_heap::relocate_phase"] + otherPhaseToCount["gc_heap::compact_phase"];

                foreach (var phase in otherPhaseToCount)
                {
                    otherCounts.Append(phase.Value);
                    double percentage = DataFrameHelpers.Round2((phase.Value / total) * 100);
                    otherPercentage.Append(percentage);
                    otherDiffCount.Append(phase.Value - phaseToCount[phase.Key]);
                    otherPercentageDiff.Append(DataFrameHelpers.Round2(percentage - phaseToPercentage[phase.Key]));
                }

                otherColumns.AddRange(new[] { otherCounts, otherPercentage, otherDiffCount, otherPercentageDiff });
            }

            List<DataFrameColumn> allColumns = new();
            allColumns.Add(phaseName);
            allColumns.Add(baselineCounts);
            allColumns.Add(baselinPercentage);
            allColumns.AddRange(otherColumns);
            return new DataFrame(allColumns);
        }

        public static DataFrame? GetPerGenerationSummary(this CPUProcessData cpuData)
        {
            StringDataFrameColumn phaseName = new(" ");

            StringDataFrameColumn gen0Count = new("Gen0 Count");
            //StringDataFrameColumn gen0Percent = new("Gen0 %");

            StringDataFrameColumn gen1Count = new("Gen1 Count");
            //StringDataFrameColumn gen1Percent = new("Gen1 %");

            StringDataFrameColumn bgcCount = new("BGC Count");
            //StringDataFrameColumn bgcPercent = new("BGC %");

            StringDataFrameColumn blockingGen2Count = new("Blocking Gen2 Count");
            //StringDataFrameColumn blockingGen2Percent = new("BGC %");

            var gcProcessData = cpuData.Parent.Analyzer.GetProcessGCData(cpuData.ProcessName).FirstOrDefault(p => p.ProcessID == cpuData.ProcessID);
            if (gcProcessData == null)
            {
                return null;
            }

            var gen0s = new HashSet<int>(gcProcessData.GCs.Where(gc => gc.Generation == 0).Select(gc => gc.Number));
            var gen1s = new HashSet<int>(gcProcessData.GCs.Where(gc => gc.Generation == 1).Select(gc => gc.Number));
            var bgcs = new HashSet<int>(gcProcessData.BGCs.Select(gc => gc.Number));
            var gen2Blocking = new HashSet<int>(gcProcessData.Gen2Blocking.Select(gc => gc.Number));

            foreach (var majorPhase in phases)
            {
                var phase = majorPhase;
                double gen0CountData = 0;
                double gen1CountData = 0;
                double bgcCountData = 0;
                double gen2BlockingCountData = 0;

                if (cpuData.PerGCData.TryGetValue(phase, out var gcToData))
                {
                    foreach (var gc in gcToData)
                    {
                        var count = gc.Value.Values.Sum(g => g.Sum(gc => gc.InclusiveCount));

                        if (gen0s.Contains(gc.Key))
                        {
                            gen0CountData += count;
                        }

                        else if (gen1s.Contains(gc.Key))
                        {
                            gen1CountData += count;
                        }

                        else if (bgcs.Contains(gc.Key))
                        {
                            bgcCountData += count;
                        }

                        else if (gen2Blocking.Contains(gc.Key))
                        {
                            gen2BlockingCountData += count;
                        }
                    }

                    phaseName.Append(phase);
                    gen0Count.Append(gen0CountData.ToString());
                    gen1Count.Append(gen1CountData.ToString());
                    bgcCount.Append(bgcCountData.ToString());
                    blockingGen2Count.Append(gen2BlockingCountData.ToString());
                }
            }

            return new DataFrame(phaseName, gen0Count, gen1Count, bgcCount, blockingGen2Count);
        }
    }
}
