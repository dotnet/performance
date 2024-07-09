using Microsoft.Data.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace GC.Analysis.API
{
    public static class GCAnalysisExt
    {
        public static List<GCProcessData> GetProcessGCData(this Analyzer analyzer, string processName)
        {
            if (!analyzer.AllGCProcessData.TryGetValue(processName, out var vals))
            {
                return new List<GCProcessData>();
            }

            return vals;
        }

        public static DataFrame Summarize(this GCProcessData processData, IEnumerable<TraceGC> gcs)
        {
            // Remove any duplicates.
            gcs = gcs.Distinct();

            double totalAllocated = gcs.Sum(gc => gc.AllocedSinceLastGCMB);
            double maxSizePeak = gcs.Max(gc => gc.HeapSizePeakMB);
            int gcCount = gcs.Count();
            int heapCount = gcs.First().HeapCount;

            var gen0 = gcs.Where(gc => gc.Generation == 0);
            var gen1 = gcs.Where(gc => gc.Generation == 1);
            var emp = gcs.Where(gc => gc.Generation == 1 || gc.Generation == 0);
            var gen2Blocking = gcs.Where(gc => gc.Generation == 2 && gc.Type != GCType.BackgroundGC);
            var bgc = gcs.Where(gc => gc.Generation == 2 && gc.Type == GCType.BackgroundGC);

            int gen0Count = gen0.Count;
            int gen1Count = gen1.Count;
            int empCount = emp.Count;
            int gen2BlockingCount = gen2Blocking.Count;
            int bgcCount = bgc.Count;

            // Pause Times
            double gen0TotalPauseTime = gen0.Sum(gc => gc.PauseDurationMSec);
            double gen1TotalPauseTime = gen1.Sum(gc => gc.PauseDurationMSec);
            double empTotalPauseTime = emp.Sum(gc => gc.PauseDurationMSec);
            double gen2BlockingTotalPauseTime = gen2Blocking.Sum(gc => gc.PauseDurationMSec);
            double bgcTotalPauseTime = bgc.Sum(gc => gc.PauseDurationMSec);

            // Promoted Bytes
            double gen0TotalPromotedMB = gen0.Sum(gc => gc.PromotedMB);
            double gen1TotalPromotedMB = gen1.Sum(gc => gc.PromotedMB);
            double empTotalPromotedMB = emp.Sum(gc => gc.PromotedMB);
            double gen2BlockingTotalPromotedMB = gen2Blocking.Sum(gc => gc.PromotedMB);
            double bgcTotalPromotedMB = bgc.Sum(gc => gc.PromotedMB);

            // Avg. Gen0 Pause Time (msec)
            double gen0AvgPauseTime = gen0.Average(gc => gc.PauseDurationMSec);
            // Avg. Gen1 Pause Time (msec)
            double gen1AvgPauseTime = gen1.Average(gc => gc.PauseDurationMSec);
            // Avg. Gen0 Promoted (mb) 
            double gen0AvgPromoted = gen0.Average(gc => gc.PromotedMB);
            // Avg. Gen1 Promoted (mb)
            double gen1AvgPromoted = gen1.Average(gc => gc.PromotedMB);

            // Avg. Gen0 Speed (mb/msec)
            double gen0AvgSpeed = gen0.Sum(gc => gc.PromotedMB) / gen0TotalPauseTime;
            // Avg. Gen1 Speed (mb/msec)
            double gen1AvgSpeed = gen1.Sum(gc => gc.PromotedMB) / gen1TotalPauseTime;
            // Avg. Gen0 Promoted (mb) / heap
            double gen0AvgPromotedPerHeap = gen0AvgPromoted / heapCount;
            // Avg. Gen1 Promoted (mb) / heap
            double gen1AvgPromotedPerHeap = gen1AvgPromoted / heapCount;
            // Avg. Gen0 Speed (mb/ms) / heap	
            double gen0AvgSpeedPerHeap = gen0AvgSpeed / heapCount;
            // Avg. Gen1 Speed (mb/ms) / heap	
            double gen1AvgSpeedPerHeap = gen1AvgSpeed / heapCount;

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

            AddStr("Total Allocated MB", totalAllocated);
            AddStr("Max Size Peak MB", maxSizePeak);

            // Counts.
            AddStr("GC Count", gcCount);
            AddStr("Heap Count", heapCount);
            AddStr("Gen0 Count", gen0Count);
            AddStr("Gen1 Count", gen1Count);
            AddStr("Ephemeral Count", empCount);
            AddStr("Gen2 Blocking Count", gen2BlockingCount);
            AddStr("BGC Count", bgcCount);

            // Pauses
            AddStr("Gen0 Total Pause Time MSec", gen0TotalPauseTime);
            AddStr("Gen1 Total Pause Time MSec", gen1TotalPauseTime);
            AddStr("Ephemeral Total Pause Time MSec", empTotalPauseTime);
            AddStr("Blocking Gen2 Total Pause Time MSec", gen2BlockingTotalPauseTime);
            AddStr("BGC Total Pause Time MSec", bgcTotalPauseTime);

            // Promotions
            AddStr("Gen0 Total Promoted MB", gen0TotalPromotedMB);
            AddStr("Gen1 Total Promoted MB", gen1TotalPromotedMB);
            AddStr("Ephemeral Total Promoted MB", empTotalPromotedMB);
            AddStr("Blocking Gen2 Total Promoted MB", gen2BlockingTotalPromotedMB);
            AddStr("BGC Total Promoted MB", bgcTotalPromotedMB);

            // Allocations
            AddStr("Mean Size Before MB", gcs.Average(gc => gc.HeapSizeBeforeMB));
            AddStr("Mean Size After MB", gcs.Average(gc => gc.HeapSizeAfterMB));

            // Speeds
            AddStr("Gen0 Average Speed (MB/MSec)", gen0AvgSpeed);
            AddStr("Gen1 Average Speed (MB/MSec)", gen1AvgSpeed);

            AddStr("Gen0 Average Pause Time (ms)", gen0AvgPauseTime);
            AddStr("Gen1 Average Pause Time (ms)", gen1AvgPauseTime);

            AddStr("Gen0 Average Promoted (mb)", gen0AvgPromoted);
            AddStr("Gen1 Average Promoted (mb)", gen1AvgPromoted);

            AddStr("Gen0 Average Promoted (mb) / heap", gen0AvgPromotedPerHeap);
            AddStr("Gen1 Average Promoted (mb) / heap", gen1AvgPromotedPerHeap);

            AddStr("Gen0 Average Speed (mb/ms) / heap", gen0AvgSpeedPerHeap);
            AddStr("Gen1 Average Speed (mb/ms) / heap", gen1AvgSpeedPerHeap);

            return new DataFrame(criteria, value);
        }
    }
}
