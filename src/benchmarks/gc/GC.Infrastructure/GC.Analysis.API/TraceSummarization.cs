using Microsoft.Data.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace GC.Analysis.API
{
    internal sealed class FirstLastData
    {
        public double FirstTimeStamp { get; set; }
        public double LastTimeStamp { get; set; }
        public int Count { get; set; }
    }

    public static class TraceSummarization
    {
        public static IEnumerable<DataFrame> SummarizeTrace(this Analyzer analyzer, string processName)
        {
            var processData = analyzer.GetProcessGCData(processName);
            if (!processData.Any())
            {
                Console.WriteLine($"No Processes with Process Name: {processName} found.");
                return Enumerable.Empty<DataFrame>();
            }

            var traceLog = analyzer.TraceLog;
            var eventSource = traceLog.Events.GetSource();

            HashSet<int> processIds = new HashSet<int>(processData.Select(p => p.ProcessID));

            Dictionary<int, FirstLastData> cpuData = new();
            Dictionary<int, FirstLastData> cswitchData = new();
            Dictionary<int, int> readyThreadCount = new();

            eventSource.Kernel.ThreadCSwitch += (data) =>
            {
                if (!processIds.Contains(data.ProcessID))
                {
                    return;
                }

                if (!cswitchData.TryGetValue(data.ProcessID, out var firstLast))
                {
                    cswitchData[data.ProcessID] = firstLast = new()
                    {
                        FirstTimeStamp = data.TimeStampRelativeMSec
                    };
                }

                firstLast.LastTimeStamp = data.TimeStampRelativeMSec;
                ++firstLast.Count;
            };

            eventSource.Kernel.PerfInfoSample += (data) =>
            {
                if (!processIds.Contains(data.ProcessID))
                {
                    return;
                }

                if (!cpuData.TryGetValue(data.ProcessID, out var firstLast))
                {
                    cpuData[data.ProcessID] = firstLast = new()
                    {
                        FirstTimeStamp = data.TimeStampRelativeMSec
                    };
                }

                firstLast.LastTimeStamp = data.TimeStampRelativeMSec;
                firstLast.Count += 1;
            };

            eventSource.Kernel.DispatcherReadyThread += (data) =>
            {
                if (!processIds.Contains(data.ProcessID))
                {
                    return;
                }

                if (!readyThreadCount.TryGetValue(data.ProcessID, out var _))
                {
                    readyThreadCount[data.ProcessID] = 0;
                }

                readyThreadCount[data.ProcessID] += 1;
            };
            eventSource.Process();

            List<DataFrame> dataFrames = new();

            // For each Process.
            foreach (var process in processData)
            {
                int processId = process.ProcessID;

                StringDataFrameColumn criteria = new($"Process ID: {processId}");
                StringDataFrameColumn startMS = new("Start (ms)");
                StringDataFrameColumn startGCNumber = new("Start GC Index");
                StringDataFrameColumn endMS = new("End (ms)");
                StringDataFrameColumn endGCNumber = new("End GC Index");
                StringDataFrameColumn notes = new("Notes");

                IEnumerable<TraceGC> traceGCs = process.GCs;

                // GC Data
                criteria.Append("GC");
                var firstGC = traceGCs.First(gc => gc.Type != GCType.BackgroundGC);
                var lastGC = traceGCs.Last(gc => gc.Type != GCType.BackgroundGC);
                startMS.Append(DataFrameHelpers.Round2(firstGC.StartRelativeMSec).ToString());
                startGCNumber.Append(firstGC.Number.ToString());
                endMS.Append(DataFrameHelpers.Round2(lastGC.StartRelativeMSec + lastGC.DurationMSec).ToString());
                endGCNumber.Append(lastGC.Number.ToString());
                notes.Append($"{traceGCs.Count()} GCs found for Process: {processId}");

                (int, int) GetStartAndEndGCIndexes(FirstLastData firstLast)
                {
                    // Get range of the first and last.
                    double firstTimestamp = firstLast.FirstTimeStamp;
                    double lastTimestamp = firstLast.LastTimeStamp;

                    var encompassedGCs = traceGCs.Where(gc =>
                    {
                        return gc.Type != GCType.BackgroundGC &&           // For all background GCs,
                               (firstTimestamp < gc.StartRelativeMSec) &&  // get all GCs after the first timestamp.
                               (gc.PauseDurationMSec + gc.PauseStartRelativeMSec < lastTimestamp); // And all GCs before the last timestamp.
                    });
                    int startGCIdx = encompassedGCs.FirstOrDefault()?.Number ?? -1;
                    int endGCIdx = encompassedGCs.LastOrDefault()?.Number ?? -1;

                    return (startGCIdx, endGCIdx);
                }

                // CPU Data
                criteria.Append("CPU Samples");
                if (cpuData.TryGetValue(processId, out var cpuFirstLast))
                {
                    startMS.Append(DataFrameHelpers.Round2(cpuFirstLast.FirstTimeStamp).ToString());
                    endMS.Append(DataFrameHelpers.Round2(cpuFirstLast.LastTimeStamp).ToString());
                    var (firstIdx, lastIdx) = GetStartAndEndGCIndexes(cpuFirstLast);
                    startGCNumber.Append(firstIdx.ToString());
                    endGCNumber.Append(lastIdx.ToString());
                    notes.Append($"Total Number of CPU Sample Events: {cpuFirstLast.Count}");
                }
                else
                {
                    startMS.Append(string.Empty);
                    endMS.Append(string.Empty);
                    startGCNumber.Append("-1");
                    endGCNumber.Append("-1");
                    notes.Append($"No CPU Events Found for Process: {processId}");
                }

                // CSwitch Data
                criteria.Append("CSwitch Data");
                if (cswitchData.TryGetValue(processId, out var cswitchFirstLast))
                {
                    startMS.Append(DataFrameHelpers.Round2(cswitchFirstLast.FirstTimeStamp).ToString());
                    endMS.Append(DataFrameHelpers.Round2(cswitchFirstLast.LastTimeStamp).ToString());
                    var (firstIdx, lastIdx) = GetStartAndEndGCIndexes(cswitchFirstLast);
                    startGCNumber.Append(firstIdx.ToString());
                    endGCNumber.Append(lastIdx.ToString());

                    if (readyThreadCount.TryGetValue(processId, out var r))
                    {
                        notes.Append($"Ready Thread Event Count: {r}");
                    }

                    else
                    {
                        notes.Append($"No Ready Thread Events Found for Process: {processId}.");
                    }
                }
                else
                {
                    startMS.Append(string.Empty);
                    endMS.Append(string.Empty);
                    startGCNumber.Append("-1");
                    endGCNumber.Append("-1");
                    notes.Append($"No CSwitch Events Found for Process: {processId}.");
                }

                dataFrames.Add(new DataFrame(criteria, startMS, startGCNumber, endMS, endGCNumber, notes));
            }

            return dataFrames;
        }
    }
}