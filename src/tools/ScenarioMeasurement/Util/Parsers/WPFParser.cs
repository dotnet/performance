using Microsoft.Diagnostics.Tracing;
using Reporting;
using System;
using System.Collections.Generic;

namespace ScenarioMeasurement;

public class WPFParser : IParser
{
    public void EnableKernelProvider(ITraceSession kernel)
    {
        kernel.EnableKernelProvider(TraceSessionManager.KernelKeyword.Process, TraceSessionManager.KernelKeyword.Thread, TraceSessionManager.KernelKeyword.ContextSwitch);
    }

    public void EnableUserProviders(ITraceSession user)
    {
        user.EnableUserProvider("E13B77A8-14B6-11DE-8069-001B212B5009", TraceEventLevel.Verbose);
    }

    public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
    {
        var results = new List<double>();
        var threadTimes = new List<double>();
        double threadTime = 0;
        var ins = new Dictionary<int, double>();
        double start = -1;
        int? pid = null;
        var doneParsingXaml = false;
        var lastKnownTime = 0.0;
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

            //We want to find the first Present/Stop after the last XamlParse event
            source.Dynamic.AddCallbackForProviderEvent("Microsoft-Windows-WPF", "WClientParseXamlBamlInfo", evt =>
            {
                if (pid.HasValue && evt.ProcessID == pid && evt.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                {
                    doneParsingXaml = true; //So when we find a Xaml parse event set the flag
                }
            });

            source.Dynamic.AddCallbackForProviderEvent("Microsoft-Windows-WPF", "WClientUcePresent/Stop", evt =>
            {
                //Then when we see a Present/Stop after the flag has been set
                if (doneParsingXaml && pid.HasValue && evt.ProcessID == pid && evt.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                {
                    lastKnownTime = evt.TimeStampRelativeMSec; //Grab the current time
                    doneParsingXaml = false; //And reset the flag
                }
            });

            //Once the process ends take the last time we set as that will be the corret one
            source.Kernel.ProcessStop += evt =>
            {
                if (pid.HasValue && evt.ProcessID == pid)
                {
                    results.Add(lastKnownTime - start);
                    threadTimes.Add(threadTime);
                    pid = null;
                    threadTime = 0;
                    start = 0;
                }
            };

            source.Process();
        }
        return new[] {
            new Counter() { Name = "WPF Startup", MetricName = "ms", DefaultCounter=true, TopCounter=true, Results = results.ToArray() },
            new Counter() { Name = "Time on Thread", MetricName = "ms", TopCounter=true, Results = threadTimes.ToArray() }
        };
    }
}
