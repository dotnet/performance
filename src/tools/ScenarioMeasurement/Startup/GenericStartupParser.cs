using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Reporting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScenarioMeasurement
{
    // [EventSource(Guid = "9bb228bd-1033-5cf0-1a56-c2dbbe0ebc86")]
    // class PerfLabGenericEventSource : EventSource
    // {
    //     public static PerfLabGenericEventSource Log = new PerfLabGenericEventSource();
    //     public void Startup() => WriteEvent(1);
    // }

    internal class GenericStartupParser : IParser
    {
        public void EnableKernelProvider(TraceEventSession kernel)
        {
            kernel.EnableKernelProvider((KernelTraceEventParser.Keywords)(KernelTraceEventParser.Keywords.Process | KernelTraceEventParser.Keywords.Thread | KernelTraceEventParser.Keywords.ContextSwitch));
        }

        public void EnableUserProviders(TraceEventSession user)
        {
            user.EnableProvider("PerfLabGenericEventSource");
        }

        public IList<Counter> Parse(string mergeTraceFile, string processName)
        {
            var results = new List<double>();
            var threadTimes = new List<double>();
            double threadTime = 0;
            var ins = new Dictionary<int, double?>();
            double start = -1;
            int? pid = null;
            using (var source = new ETWTraceEventSource(mergeTraceFile))
            {

                source.Kernel.ProcessStart += evt =>
                {
                    if (evt.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (pid.HasValue)
                        {
                            throw new Exception("found a start when we already had a start");
                        }
                        pid = evt.ProcessID;
                        start = evt.TimeStampRelativeMSec;
                    }
                };

                source.Kernel.ThreadCSwitch += evt =>
                {
                    if (!pid.HasValue) // we're currently in a measurement interval
                        return;

                    if (evt.NewProcessID != pid && evt.OldProcessID != pid) // but this isn't it
                        return;

                    if (evt.OldProcessID == pid && ins.ContainsKey(evt.OldThreadID))
                    {
                        if (!ins[evt.OldThreadID].HasValue)
                        {
                            return;
                        }
                        threadTime += evt.TimeStampRelativeMSec - ins[evt.OldThreadID].Value;
                        ins.Remove(evt.OldThreadID);
                    }
                    else
                    {
                        ins[evt.NewThreadID] = evt.TimeStampRelativeMSec;
                    }

                };

                source.Dynamic.AddCallbackForProviderEvent("PerfLabGenericEventSource", "Startup", evt =>
                {
                    if (!evt.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                        return;
                    if (pid.HasValue)
                    {
                        if (pid != evt.ProcessID)
                        {
                            throw new Exception("found mismatched end");
                        }
                        results.Add(evt.TimeStampRelativeMSec - start);
                        threadTimes.Add(threadTime);
                        pid = null;
                        threadTime = 0;
                        start = 0;
                    }
                });

                source.Process();
            }
            return new[] {
                new Counter() { Name = "Generic Startup", MetricName = "ms", Results = results.ToArray() },
                new Counter() { Name = "Time on Thread", MetricName = "ms", Results = threadTimes.ToArray() }
            };
        }
    }
}