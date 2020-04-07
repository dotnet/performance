using Microsoft.Diagnostics.Tracing;
using Reporting;
using System;
using System.Collections.Generic;


namespace ScenarioMeasurement
{
    public class ProcessTimeParser : IParser
    {
        public void EnableKernelProvider(ITraceSession kernel)
        {
            kernel.EnableKernelProvider(TraceSessionManager.KernelKeyword.Process, TraceSessionManager.KernelKeyword.Thread, TraceSessionManager.KernelKeyword.ContextSwitch);
        }

        public void EnableUserProviders(ITraceSession user)
        {
        }

        public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
        {
            var results = new List<double>();
            double threadTime = 0;
            var threadTimes = new List<double>();
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

                source.Kernel.ProcessEndGroup += evt =>
                {
                    // is it possible that neither pid and evt.ProcessID has value?
                    if (pid.HasValue && pid == evt.ProcessID)
                    {
                        results.Add(evt.TimeStampRelativeMSec - start);
                        pid = null;
                        threadTimes.Add(threadTime);
                        threadTime = 0;
                        start = 0;
                    }
                };

                source.Process();
            }

            return new[] { new Counter() { Name = "Process Time", Results = results.ToArray(), TopCounter = true, DefaultCounter = true, HigherIsBetter = false, MetricName = "ms"},
                           new Counter() { Name = "Time on Thread", Results = threadTimes.ToArray(), TopCounter = true, DefaultCounter = false, HigherIsBetter = false, MetricName = "ms" }
            };
        }
    }
}
