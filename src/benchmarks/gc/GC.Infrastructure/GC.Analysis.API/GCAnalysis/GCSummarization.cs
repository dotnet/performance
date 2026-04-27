using Microsoft.Data.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System.Diagnostics;
using System.Text;

namespace GC.Analysis.API
{
    public static class GCSummarization
    {
        public static IEnumerable<DataFrame> Summarize(this Analyzer analyzer, int topN, string criteriaInGCStats = nameof(GCStats.Count))
        {
            List<(double value, DataFrame summary)> summaryData = new();

            foreach (var processes in analyzer.AllGCProcessData.Values)
            {
                foreach (var process in processes)
                {
                    summaryData.Add((ReflectionHelpers.GetDoubleValueForGCStatsField(process.Stats, criteriaInGCStats), Summarize(analyzer, process.ProcessID)));
                }
            }

            return summaryData.OrderByDescending(s => s.value).Select(s => s.summary).Take(topN);
        }

        public static IEnumerable<DataFrame> Summarize(this Analyzer analyzer, string processName)
        {
            List<DataFrame> dataFrames = new();

            var processes = analyzer.GetProcessGCData(processName);
            foreach (var processData in processes)
            {
                dataFrames.Add(analyzer.Summarize(processData.ProcessID));
            }

            return dataFrames;
        }

        internal static DataFrame Summarize(this Analyzer analyzer, int processID)
        {
            StringDataFrameColumn criteria = new(" ");
            StringDataFrameColumn value = new("Values");

            void AddStr(string c, object val)
            {
                criteria.Append(c);

                string valToString = val.ToString();

                if (double.TryParse(valToString, out var r))
                {
                    valToString = DataFrameHelpers.Round2(r).ToString();
                }

                value.Append(valToString);
            }

            GCProcessData? processData = null;
            foreach (var val in analyzer.AllGCProcessData.Values)
            {
                processData = val.FirstOrDefault(v => v.ProcessID == processID);
                if (processData != null)
                {
                    break;
                }
            }

            AddStr("Process ID", processData.ProcessID);
            AddStr("Process Name", processData.ProcessName);
            AddStr("Commandline", processData.CommandLine);

            AddStr("Process Duration (Sec)", (processData.Stats.ProcessDuration / 1000));
            AddStr("Total Allocated MB", processData.Stats.TotalAllocatedMB);
            AddStr("Max Size Peak MB", processData.Stats.MaxSizePeakMB);

            // Counts.
            AddStr("GC Count", processData.Stats.Count);
            AddStr("Heap Count", processData.Stats.HeapCount);
            AddStr("Gen0 Count", processData.Generations[0].Count);
            AddStr("Gen1 Count", processData.Generations[1].Count);
            AddStr("Ephemeral Count", processData.Generations[0].Count + processData.Generations[1].Count);
            AddStr("Gen2 Blocking Count", processData.Gen2Blocking.Count());
            AddStr("BGC Count", processData.BGCs.Count());

            // Pauses
            AddStr("Gen0 Total Pause Time MSec", processData.Generations[0].TotalPauseTimeMSec);
            AddStr("Gen1 Total Pause Time MSec", processData.Generations[1].TotalPauseTimeMSec);
            AddStr("Ephemeral Total Pause Time MSec", processData.Generations[0].TotalPauseTimeMSec + processData.Generations[1].TotalPauseTimeMSec);
            AddStr("Blocking Gen2 Total Pause Time MSec", processData.Gen2Blocking.Sum(gc => gc.PauseDurationMSec));
            AddStr("BGC Total Pause Time MSec", processData.BGCs.Sum(gc => gc.PauseDurationMSec));

            AddStr("GC Pause Time %", processData.Stats.GetGCPauseTimePercentage());

            // Promotions
            AddStr("Gen0 Total Promoted MB", processData.Generations[0].TotalPromotedMB);
            AddStr("Gen1 Total Promoted MB", processData.Generations[1].TotalPromotedMB);
            AddStr("Ephemeral Total Promoted MB", processData.Generations[0].TotalPromotedMB + processData.Generations[1].TotalPromotedMB);
            AddStr("BGC Total Promoted MB", processData.BGCs.Sum(gc => gc.PromotedMB));
            AddStr("Gen2 Total Promoted MB - Blocking", processData.Gen2Blocking.Sum(gc => gc.PromotedMB));

            // Allocations
            AddStr("Mean Size Before MB", processData.GCs.Average(gc => gc.HeapSizeBeforeMB));
            AddStr("Mean Size After MB", processData.Stats.MeanSizeAfterMB);

            // Speeds
            AddStr("Ephemeral Average Speed (MB/MSec)", (processData.Generations[0].TotalPromotedMB + processData.Generations[1].TotalPromotedMB) / (processData.Generations[0].TotalPauseTimeMSec + processData.Generations[1].TotalPauseTimeMSec));
            AddStr("Gen0 Average Speed (MB/MSec)", processData.Generations[0].TotalPromotedMB / processData.Generations[0].TotalPauseTimeMSec);
            AddStr("Gen1 Average Speed (MB/MSec)", processData.Generations[1].TotalPromotedMB / processData.Generations[1].TotalPauseTimeMSec);

            IEnumerable<TraceGC> gen0 = processData.GCs.Where(gc => gc.Generation == 0);
            IEnumerable<TraceGC> gen1 = processData.GCs.Where(gc => gc.Generation == 1);

            AddStr("Avg. Gen0 Pause Time (ms)", (gen0.Count() > 0 ? gen0.Average(gc => gc.PauseDurationMSec) : double.NaN));
            AddStr("Avg. Gen1 Pause Time (ms)", (gen1.Count() > 0 ? gen1.Average(gc => gc.PauseDurationMSec) : double.NaN));

            AddStr("Avg. Gen0 Promoted (mb)", (gen0.Count() > 0 ? gen0.Average(gc => gc.PromotedMB) : double.NaN));
            AddStr("Avg. Gen1 Promoted (mb)", (gen1.Count() > 0 ? gen1.Average(gc => gc.PromotedMB) : double.NaN));

            var gen0Speed = processData.Generations[0].TotalPromotedMB / processData.Generations[0].TotalPauseTimeMSec;
            AddStr("Avg. Gen0 Speed (mb/ms)", gen0Speed);

            var gen1Speed = processData.Generations[1].TotalPromotedMB / processData.Generations[1].TotalPauseTimeMSec;
            AddStr("Avg. Gen1 Speed (mb/ms)", gen1Speed);

            AddStr("Avg. Gen0 Promoted (mb) / heap", gen0.Count() > 0 ? gen0.Average(gc => gc.PromotedMB) / processData.Stats.HeapCount : double.NaN);
            AddStr("Avg. Gen1 Promoted (mb) / heap", gen1.Count() > 0 ? gen1.Average(gc => gc.PromotedMB) / processData.Stats.HeapCount : double.NaN);

            AddStr("Avg. Gen0 Speed (mb/ms) / heap", gen0Speed / processData.Stats.HeapCount);
            AddStr("Avg. Gen1 Speed (mb/ms) / heap", gen1Speed / processData.Stats.HeapCount);

            return new DataFrame(criteria, value);
        }

        public static DataFrame Compare(this GCProcessData processData, IEnumerable<GCProcessData> others)
        {
            StringDataFrameColumn criteria = new(" ");
            StringDataFrameColumn value = new("Baseline");
            List<StringDataFrameColumn> otherColumns = new(others.Count());
            List<StringDataFrameColumn> diffColumns = new(others.Count());
            List<StringDataFrameColumn> diffPercentColumns = new(others.Count());

            foreach (var o in others)
            {
                StringDataFrameColumn newValue = new($"{o.ProcessID}");
                otherColumns.Add(newValue);

                StringDataFrameColumn diffValue = new($"Diff: {o.ProcessID}");
                diffColumns.Add(diffValue);

                StringDataFrameColumn diffPercentValue = new($"Diff %: {o.ProcessID}");
                diffPercentColumns.Add(diffPercentValue);
            }

            void Add(string c, double baselineVal, IEnumerable<double> otherVals)
            {
                criteria.Append(c);
                value.Append(baselineVal.ToString("N3"));

                for (int i = 0; i < otherColumns.Count; i++)
                {
                    otherColumns[i].Append(otherVals.ElementAt(i).ToString("N3"));
                    var d = otherVals.ElementAt(i) - baselineVal;
                    diffColumns[i].Append((d).ToString("N3"));
                    diffPercentColumns[i].Append((d / baselineVal * 100.0).ToString("N3"));
                }
            }

            void AddStr(string c, string baselineVal, IEnumerable<string> otherVals)
            {
                criteria.Append(c);
                value.Append(baselineVal);

                for (int i = 0; i < otherVals.Count(); i++)
                {
                    otherColumns[i].Append(otherVals.ElementAt(i));
                    diffColumns[i].Append(string.Empty);
                    diffPercentColumns[i].Append(String.Empty);
                }
            }

            AddStr("Process ID", processData.ProcessID.ToString(), others.Select(p => p.ProcessID.ToString()));
            AddStr("Process Name", processData.ProcessName, others.Select(p => p.ProcessName));
            AddStr("Commandline", processData.CommandLine, others.Select(p => p.CommandLine));

            Add("Process Duration (Sec)", processData.Stats.ProcessDuration / 1000, others.Select(p => p.Stats.ProcessDuration / 1000));
            Add("Total Allocated MB", processData.Stats.TotalAllocatedMB, others.Select(p => p.Stats.TotalAllocatedMB));
            Add("Max Size Peak MB", processData.Stats.MaxSizePeakMB, others.Select(p => p.Stats.MaxSizePeakMB));

            // Counts.
            Add("GC Count", processData.Stats.Count, others.Select(p => (double)p.Stats.Count));
            Add("Heap Count", processData.Stats.HeapCount, others.Select(p => (double)p.Stats.HeapCount));
            Add("Gen0 Count", processData.Generations[0].Count, others.Select(p => (double)p.Generations[0].Count));
            Add("Gen1 Count", processData.Generations[1].Count, others.Select(p => (double)p.Generations[1].Count));
            Add("Ephemeral Count", processData.Generations[0].Count + processData.Generations[1].Count, others.Select(p => (double)p.Generations[0].Count + p.Generations[1].Count));
            Add("Gen2 Blocking Count", processData.Gen2Blocking.Count(), others.Select(p => (double)p.Gen2Blocking.Count()));
            Add("BGC Count", processData.BGCs.Count(), others.Select(p => (double)p.BGCs.Count()));

            // Pauses
            Add("Gen0 Total Pause Time MSec", processData.Generations[0].TotalPauseTimeMSec, others.Select(p => (double)p.Generations[0].TotalPauseTimeMSec));
            Add("Gen1 Total Pause Time MSec", processData.Generations[1].TotalPauseTimeMSec, others.Select(p => (double)p.Generations[1].TotalPauseTimeMSec));
            Add("Ephemeral Total Pause Time MSec", processData.Generations[0].TotalPauseTimeMSec + processData.Generations[1].TotalPauseTimeMSec, others.Select(p => (double)p.Generations[0].TotalPauseTimeMSec + p.Generations[1].TotalPauseTimeMSec));
            Add("Blocking Gen2 Total Pause Time MSec", processData.Gen2Blocking.Sum(gc => gc.PauseDurationMSec), others.Select(p => (double)p.Gen2Blocking.Sum(gc => gc.PauseDurationMSec)));
            Add("BGC Total Pause Time MSec", processData.BGCs.Sum(gc => gc.PauseDurationMSec), others.Select(p => p.BGCs.Sum(gc => gc.PauseDurationMSec)));

            Add("GC Pause Time %", processData.Stats.GetGCPauseTimePercentage(), others.Select(gc => gc.Stats.GetGCPauseTimePercentage()));

            // Speed
            // Pauses
            IEnumerable<TraceGC> gen0 = processData.GCs.Where(gc => gc.Generation == 0);
            IEnumerable<TraceGC> gen1 = processData.GCs.Where(gc => gc.Generation == 1);

            Dictionary<GCProcessData, IEnumerable<TraceGC>> gen0Cache = new();
            foreach (var other in others)
            {
                var gen0s = other.GCs.Where(gc => gc.Generation == 0);
                gen0Cache[other] = gen0s;
            }

            Dictionary<GCProcessData, IEnumerable<TraceGC>> gen1Cache = new();
            foreach (var other in others)
            {
                var gen1s = other.GCs.Where(gc => gc.Generation == 1);
                gen1Cache[other] = gen1s;
            }

            int heapCount = processData.Stats.HeapCount;

            Add("Avg. Gen0 Pause Time (ms)", gen0.Average(gc => gc.PauseDurationMSec), others.Select(p => gen0Cache[p].Average(gc => gc.PauseDurationMSec)));
            Add("Avg. Gen1 Pause Time (ms)", gen1.Average(gc => gc.PauseDurationMSec), others.Select(p => gen1Cache[p].Average(gc => gc.PauseDurationMSec)));

            Add("Avg. Gen0 Promoted (mb)", gen0.Average(gc => gc.PromotedMB), others.Select(p => (double)gen0Cache[p].Average(gc => gc.PromotedMB)));
            Add("Avg. Gen1 Promoted (mb)", gen1.Average(gc => gc.PromotedMB), others.Select(p => (double)gen1Cache[p].Average(gc => gc.PromotedMB)));

            Add("Avg. Gen0 Speed (mb/ms)", processData.Generations[0].TotalPromotedMB / processData.Generations[0].TotalPauseTimeMSec, others.Select(p => p.Generations[0].TotalPromotedMB / p.Generations[0].TotalPauseTimeMSec));
            Add("Avg. Gen1 Speed (mb/ms)", processData.Generations[1].TotalPromotedMB / processData.Generations[1].TotalPauseTimeMSec, others.Select(p => p.Generations[1].TotalPromotedMB / p.Generations[1].TotalPauseTimeMSec));

            Add("Avg. Gen0 Promoted (mb) / heap", gen0.Average(gc => gc.PromotedMB) / heapCount, others.Select(p => gen0Cache[p].Average(gc => gc.PromotedMB) / heapCount));
            Add("Avg. Gen1 Promoted (mb) / heap", gen1.Average(gc => gc.PromotedMB) / heapCount, others.Select(p => gen1Cache[p].Average(gc => gc.PromotedMB) / heapCount));

            Add("Avg. Gen0 Speed (mb/ms) / heap", processData.Generations[0].TotalPromotedMB / processData.Generations[0].TotalPauseTimeMSec / heapCount, others.Select(p => p.Generations[0].TotalPromotedMB / p.Generations[0].TotalPauseTimeMSec / heapCount));
            Add("Avg. Gen1 Speed (mb/ms) / heap", processData.Generations[1].TotalPromotedMB / processData.Generations[1].TotalPauseTimeMSec / heapCount, others.Select(p => p.Generations[1].TotalPromotedMB / p.Generations[1].TotalPauseTimeMSec / heapCount));

            List<DataFrameColumn> columns = new List<DataFrameColumn> { criteria, value };

            for (int i = 0; i < diffColumns.Count; i++)
            {
                columns.Add(otherColumns[i]);
                columns.Add(diffColumns[i]);
                columns.Add(diffPercentColumns[i]);
            }

            return new DataFrame(columns);
        }

        public static DataFrame CompareNormalizedByMaxTotalAllocations(this GCProcessData processData, IEnumerable<GCProcessData> others)
        {
            StringDataFrameColumn criteria = new(" ");
            StringDataFrameColumn value = new("Baseline");
            List<StringDataFrameColumn> otherColumns = new(others.Count());
            List<StringDataFrameColumn> diffColumns = new(others.Count());
            List<StringDataFrameColumn> diffPercentColumns = new(others.Count());

            foreach (var o in others)
            {
                StringDataFrameColumn newValue = new($"{o.ProcessID}");
                otherColumns.Add(newValue);

                StringDataFrameColumn diffValue = new($"Diff: {o.ProcessID}");
                diffColumns.Add(diffValue);

                StringDataFrameColumn diffPercentValue = new($"Diff %: {o.ProcessID}");
                diffPercentColumns.Add(diffPercentValue);
            }

            void Add(string c, double baselineVal, IEnumerable<double> otherVals, string format = "N3")
            {
                criteria.Append(c);
                value.Append(baselineVal.ToString(format));

                for (int i = 0; i < otherColumns.Count; i++)
                {
                    otherColumns[i].Append(otherVals.ElementAt(i).ToString(format));
                    var d = otherVals.ElementAt(i) - baselineVal;
                    diffColumns[i].Append((d).ToString(format));
                    diffPercentColumns[i].Append((d / baselineVal * 100.0).ToString(format));
                }
            }

            void AddStr(string c, string baselineVal, IEnumerable<string> otherVals)
            {
                criteria.Append(c);
                value.Append(baselineVal);

                for (int i = 0; i < otherVals.Count(); i++)
                {
                    otherColumns[i].Append(otherVals.ElementAt(i));
                    diffColumns[i].Append(string.Empty);
                    diffPercentColumns[i].Append(String.Empty);
                }
            }

            AddStr("Process ID", processData.ProcessID.ToString(), others.Select(p => p.ProcessID.ToString()));
            AddStr("Process Name", processData.ProcessName, others.Select(p => p.ProcessName));
            AddStr("Commandline", processData.CommandLine, others.Select(p => p.CommandLine));

            Add("Process Duration (Sec)", processData.Stats.ProcessDuration / 1000, others.Select(p => p.Stats.ProcessDuration / 1000));

            double maxTotalAllocated = processData.Stats.TotalAllocatedMB;
            foreach (var p in others)
            {
                maxTotalAllocated = Math.Max(maxTotalAllocated, p.Stats.TotalAllocatedMB);
            }
            double currentAllocRatio = processData.Stats.TotalAllocatedMB / maxTotalAllocated;

            Dictionary<GCProcessData, double> ratioMap = new();
            foreach (var o in others)
            {
                ratioMap[o] = o.Stats.TotalAllocatedMB / maxTotalAllocated;
            }

            Add("Allocation Ratio", Math.Round(currentAllocRatio, 10), others.Select(p => Math.Round(ratioMap[p], 10)), "N5");
            Add("Total Allocated MB", processData.Stats.TotalAllocatedMB / currentAllocRatio, others.Select(p => p.Stats.TotalAllocatedMB / ratioMap[p]));
            Add("Max Size Peak MB", processData.Stats.MaxSizePeakMB / currentAllocRatio, others.Select(p => p.Stats.MaxSizePeakMB / ratioMap[p]));

            // Counts.
            Add("GC Count", processData.Stats.Count, others.Select(p => (double)p.Stats.Count));
            Add("Heap Count", processData.Stats.HeapCount, others.Select(p => (double)p.Stats.HeapCount));
            Add("Gen0 Count", processData.Generations[0].Count, others.Select(p => (double)p.Generations[0].Count));
            Add("Gen1 Count", processData.Generations[1].Count, others.Select(p => (double)p.Generations[1].Count));
            Add("Ephemeral Count", processData.Generations[0].Count + processData.Generations[1].Count, others.Select(p => (double)p.Generations[0].Count + p.Generations[1].Count));
            Add("Gen2 Blocking Count", processData.Gen2Blocking.Count(), others.Select(p => (double)p.Gen2Blocking.Count()));
            Add("BGC Count", processData.BGCs.Count(), others.Select(p => (double)p.BGCs.Count()));

            // Pauses
            Add("Total Pause Time MSec", processData.Stats.TotalPauseTimeMSec / currentAllocRatio, others.Select(p => p.Stats.TotalPauseTimeMSec / ratioMap[p]));
            Add("Gen0 Total Pause Time MSec", processData.Generations[0].TotalPauseTimeMSec / currentAllocRatio, others.Select(p => p.Generations[0].TotalPauseTimeMSec / ratioMap[p]));
            Add("Gen1 Total Pause Time MSec", processData.Generations[1].TotalPauseTimeMSec / currentAllocRatio, others.Select(p => p.Generations[1].TotalPauseTimeMSec / ratioMap[p]));
            Add("Ephemeral Total Pause Time MSec", (processData.Generations[0].TotalPauseTimeMSec + processData.Generations[1].TotalPauseTimeMSec) / currentAllocRatio, others.Select(p => (p.Generations[0].TotalPauseTimeMSec + p.Generations[1].TotalPauseTimeMSec) / (ratioMap[p])));
            Add("Blocking Gen2 Total Pause Time MSec", processData.Gen2Blocking.Sum(gc => gc.PauseDurationMSec) / currentAllocRatio, others.Select(p => (double)p.Gen2Blocking.Sum(gc => gc.PauseDurationMSec) / ratioMap[p]));
            Add("BGC Total Pause Time MSec", processData.BGCs.Sum(gc => gc.PauseDurationMSec) / currentAllocRatio, others.Select(p => p.BGCs.Sum(gc => gc.PauseDurationMSec) / ratioMap[p]));

            Add("GC Pause Time %", processData.Stats.GetGCPauseTimePercentage(), others.Select(gc => gc.Stats.GetGCPauseTimePercentage()));

            // Speed
            // Pauses
            IEnumerable<TraceGC> gen0 = processData.GCs.Where(gc => gc.Generation == 0);
            IEnumerable<TraceGC> gen1 = processData.GCs.Where(gc => gc.Generation == 1);

            Dictionary<GCProcessData, IEnumerable<TraceGC>> gen0Cache = new();
            foreach (var other in others)
            {
                var gen0s = other.GCs.Where(gc => gc.Generation == 0);
                gen0Cache[other] = gen0s;
            }

            Dictionary<GCProcessData, IEnumerable<TraceGC>> gen1Cache = new();
            foreach (var other in others)
            {
                var gen1s = other.GCs.Where(gc => gc.Generation == 1);
                gen1Cache[other] = gen1s;
            }

            int heapCount = processData.Stats.HeapCount;

            Add("Avg. Gen0 Pause Time (ms)", gen0.Average(gc => gc.PauseDurationMSec), others.Select(p => gen0Cache[p].Average(gc => gc.PauseDurationMSec)));
            Add("Avg. Gen1 Pause Time (ms)", gen1.Average(gc => gc.PauseDurationMSec), others.Select(p => gen1Cache[p].Average(gc => gc.PauseDurationMSec)));

            Add("Avg. Gen0 Promoted (mb)", gen0.Average(gc => gc.PromotedMB), others.Select(p => (double)gen0Cache[p].Average(gc => gc.PromotedMB)));
            Add("Avg. Gen1 Promoted (mb)", gen1.Average(gc => gc.PromotedMB), others.Select(p => (double)gen1Cache[p].Average(gc => gc.PromotedMB)));

            Add("Avg. Gen0 Speed (mb/ms)", processData.Generations[0].TotalPromotedMB / processData.Generations[0].TotalPauseTimeMSec, others.Select(p => p.Generations[0].TotalPromotedMB / p.Generations[0].TotalPauseTimeMSec));
            Add("Avg. Gen1 Speed (mb/ms)", processData.Generations[1].TotalPromotedMB / processData.Generations[1].TotalPauseTimeMSec, others.Select(p => p.Generations[1].TotalPromotedMB / p.Generations[1].TotalPauseTimeMSec));

            Add("Avg. Gen0 Promoted (mb) / heap", gen0.Average(gc => gc.PromotedMB) / heapCount, others.Select(p => gen0Cache[p].Average(gc => gc.PromotedMB) / heapCount));
            Add("Avg. Gen1 Promoted (mb) / heap", gen1.Average(gc => gc.PromotedMB) / heapCount, others.Select(p => gen1Cache[p].Average(gc => gc.PromotedMB) / heapCount));

            Add("Avg. Gen0 Speed (mb/ms) / heap", processData.Generations[0].TotalPromotedMB / processData.Generations[0].TotalPauseTimeMSec / heapCount, others.Select(p => p.Generations[0].TotalPromotedMB / p.Generations[0].TotalPauseTimeMSec / heapCount));
            Add("Avg. Gen1 Speed (mb/ms) / heap", processData.Generations[1].TotalPromotedMB / processData.Generations[1].TotalPauseTimeMSec / heapCount, others.Select(p => p.Generations[1].TotalPromotedMB / p.Generations[1].TotalPauseTimeMSec / heapCount));

            List<DataFrameColumn> columns = new List<DataFrameColumn> { criteria, value };

            for (int i = 0; i < diffColumns.Count; i++)
            {
                columns.Add(otherColumns[i]);
                columns.Add(diffColumns[i]);
                columns.Add(diffPercentColumns[i]);
            }

            return new DataFrame(columns);
        }

        public static string CreateComparativeSummary(this GCProcessData baseline, GCProcessData comparand)
        {
            StringBuilder sb = new();

            double diffPercentage(double baseline, double comparand)
            {
                if (double.IsNaN(baseline) || double.IsNaN(comparand))
                {
                    return double.NaN;
                }

                return (comparand - baseline) / baseline * 100;
            }

            string GetDifferenceString(double baselineValue, double comparandValue, string name, string units = "", double tolerancePercentage = 5)
            {
                double diffPercent = diffPercentage(baseline.DurationMSec, comparand.DurationMSec);
                if (diffPercent > tolerancePercentage)
                {
                    return sb.AppendLine($"Comparand's {name} is higher than the Baseline's by {diffPercent}%. {comparandValue} {units} vs. {baseline} {units} ({comparandValue - baselineValue} {units})").ToString();
                }

                else if (diffPercent < -tolerancePercentage)
                {
                    return sb.AppendLine($"Baseline's {name} is higher than the Comparand's by {diffPercent}%. {baselineValue} {units} vs. {comparandValue} {units} ({baselineValue - comparandValue} {units})").ToString();
                }

                else
                {
                    return string.Empty;
                }
            }

            // Execution Time.
            sb.AppendLine(GetDifferenceString(baselineValue: baseline.DurationMSec * 100, comparandValue: comparand.DurationMSec * 100, "Execution Time (Seconds)", "Seconds", 0));

            // Allocated Bytes
            sb.AppendLine(GetDifferenceString(baselineValue: baseline.Stats.TotalAllocatedMB, comparandValue: comparand.Stats.TotalAllocatedMB, "Total Allocated (MB)", "MB", 5));

            // GC Pause Contribution.
            sb.AppendLine($"GC Pause Contributed: {baseline.Stats.TotalPauseTimeMSec} and {comparand.Stats.TotalPauseTimeMSec} to total execution time, %{baseline.Stats.TotalPauseTimeMSec / baseline.DurationMSec} / %{comparand.Stats.TotalPauseTimeMSec / comparand.DurationMSec} of the execution time, respectively");

            // GC Counts.

            return sb.ToString();
        }
    }
}
