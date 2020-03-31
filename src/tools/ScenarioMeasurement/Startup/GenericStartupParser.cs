﻿using Microsoft.Diagnostics.Tracing;
using Reporting;
using System;
using System.Collections.Generic;

namespace ScenarioMeasurement
{
    // [EventSource(Guid = "9bb228bd-1033-5cf0-1a56-c2dbbe0ebc86")]
    // class PerfLabGenericEventSource : EventSource
    // {
    //     public static PerfLabGenericEventSource Log = new PerfLabGenericEventSource();
    //     public void Startup() => WriteEvent(1);
    // }

    public class GenericStartupParser : IParser
    {
        public void EnableKernelProvider(ITraceSession kernel)
        {
            kernel.EnableKernelProvider(TraceSessionManager.KernelKeyword.Process, TraceSessionManager.KernelKeyword.Thread, TraceSessionManager.KernelKeyword.ContextSwitch);
        }

        public void EnableUserProviders(ITraceSession user)
        {
            user.EnableUserProvider("PerfLabGenericEventSource");
        }

        public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
        {
            var results = new List<double>();
            var threadTimes = new List<double>();
            double threadTime = 0;
            var ins = new Dictionary<int, double>();
            double start = -1;
            int? pid = null;
            using (var source = new ETWTraceEventSource(mergeTraceFile))
            {

                source.Kernel.ProcessStart += evt =>
                {
                    if (processName.Equals(evt.ProcessName, StringComparison.OrdinalIgnoreCase) && pids.Contains(evt.ProcessID) && evt.CommandLine.Trim() == commandLine.Trim())
                    {
                        if (pid.HasValue)
                        {
                            // Processes might be reentrant. For now this traces the first (outermost) process of a given name.
                            return;
                        }
                        pid = evt.ProcessID;
                        start = evt.TimeStampRelativeMSec;
                    }
                };

                source.Kernel.ThreadCSwitch += evt =>
                {
                    if (!pid.HasValue) // we're currently in a measurement interval
                        return;

                    if (evt.NewProcessID != pid && evt.OldProcessID != pid)
                        return; // but this isn't our process

                    if (evt.OldProcessID == pid) // this is a switch out from our process
                    {
                        if (ins.TryGetValue(evt.OldThreadID, out var value)) // had we ever recorded a switch in for this thread? 
                        {
                            threadTime += evt.TimeStampRelativeMSec - value;
                            ins.Remove(evt.OldThreadID);
                        }
                    }
                    else // this is a switch in to our process
                    {
                        ins[evt.NewThreadID] = evt.TimeStampRelativeMSec;
                    }
                };

                source.Dynamic.AddCallbackForProviderEvent("PerfLabGenericEventSource", "Startup", evt =>
                {
                    if (pid.HasValue && evt.ProcessID == pid && evt.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    {
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
                new Counter() { Name = "Generic Startup", MetricName = "ms", DefaultCounter=true, TopCounter=true, Results = results.ToArray() },
                new Counter() { Name = "Time on Thread", MetricName = "ms", TopCounter=true, Results = threadTimes.ToArray() }
            };
        }
    }
}
