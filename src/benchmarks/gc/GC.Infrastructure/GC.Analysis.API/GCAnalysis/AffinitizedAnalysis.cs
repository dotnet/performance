using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace GC.Analysis.API
{
    public sealed class AffinitizedCPUData
    {
        public int ProcessorNumber { get; set; }
        public string Name { get; set; }
        public int Priority { get; set; }
        public int NumberOfSamples { get; set; }
    }

    public static class AffinitizedAnalysis
    {
        private const int DEFAULT_GC_PRIORITY = 14;
        public static Dictionary<int, Dictionary<int, AffinitizedCPUData>> GetAffinitizedAnalysis(this GCProcessData processData, double startTime, double endTime, int priorityUpperBound = DEFAULT_GC_PRIORITY)
        {
            var filteredEvents = processData.Parent.TraceLog.Events.Filter(e => e is SampledProfileTraceData && e.TimeStampRelativeMSec >= startTime && e.TimeStampRelativeMSec <= endTime);

            // ProcessorNumber -> < ProcessID  -> AffinitizedCPUData >
            Dictionary<int, Dictionary<int, AffinitizedCPUData>> data = new();

            // Get the list of processors
            HashSet<int> gcThreadProcessorNumbers = new();

            // Get the processor numbers of interest and add the contribution by the GC of the pertinent process.
            foreach (var @event in filteredEvents)
            {
                SampledProfileTraceData? sampledProfileTraceData = @event as SampledProfileTraceData;

                // GC Threads Only.
                if (@event.ProcessID == processData.ProcessID && processData.GCThreadIDsToHeapNumbers.ContainsKey(@event.ThreadID))
                {
                    if (!data.TryGetValue(@event.ProcessorNumber, out var r))
                    {
                        data[@event.ProcessorNumber] = r = new();
                    }

                    if (!r.TryGetValue(processData.ProcessID, out var cpuData))
                    {
                        cpuData = r[processData.ProcessID] = new AffinitizedCPUData
                        {
                            Name = $"GC Thread",
                            ProcessorNumber = @event.ProcessorNumber,
                            Priority = sampledProfileTraceData?.Priority ?? -1,
                            NumberOfSamples = 0
                        };
                    }

                    cpuData.NumberOfSamples++;
                    gcThreadProcessorNumbers.Add(sampledProfileTraceData.ProcessorNumber);
                }
            }

            foreach (var @event in filteredEvents)
            {
                SampledProfileTraceData? sampledProfileTraceData = @event as SampledProfileTraceData;
                int processorNumber = sampledProfileTraceData?.ProcessorNumber ?? -1;

                // High Pri, running on the same processor as the GC thread and not one of the GC Threads.
                if (sampledProfileTraceData?.Priority >= priorityUpperBound &&
                    gcThreadProcessorNumbers.Contains(processorNumber) &&
                    (processData.ProcessID == sampledProfileTraceData.ProcessID && !processData.GCThreadIDsToHeapNumbers.ContainsKey(@event.ThreadID)))
                {
                    if (!data[processorNumber].TryGetValue(@event.ProcessID, out var cpuData))
                    {
                        data[processorNumber][@event.ProcessID] = cpuData = new AffinitizedCPUData
                        {
                            Name = sampledProfileTraceData.ProcessName,
                            ProcessorNumber = processorNumber,
                            Priority = sampledProfileTraceData.Priority,
                            NumberOfSamples = 0
                        };
                    }

                    cpuData.NumberOfSamples++;
                }
            }

            return data;
        }
    }
}
