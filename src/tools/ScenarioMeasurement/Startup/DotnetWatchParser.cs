using Microsoft.Diagnostics.Tracing;
using Reporting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace ScenarioMeasurement;

public class DotnetWatchParser : IParser
{
    public DotnetWatchParser()
    {

    }

    public void EnableKernelProvider(ITraceSession kernel)
    {
        kernel.EnableKernelProvider(TraceSessionManager.KernelKeyword.Process, TraceSessionManager.KernelKeyword.Thread, TraceSessionManager.KernelKeyword.ContextSwitch);
    }

    public void EnableUserProviders(ITraceSession user)
    {
        user.EnableUserProvider("HotReload", TraceEventLevel.Verbose);
    }

    public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
    {
        var results = new List<double>();
        var reloadResults = new List<double>();
        var warmReloadResults = new List<double>();
        var ins = new Dictionary<int, double>();
        double start = -1;
        int? pid = null;
        var firstHotReload = true;
        using (var source = new TraceSourceManager(mergeTraceFile))
        {

            source.Kernel.ProcessStart += evt =>
            {
                if (!pid.HasValue && ParserUtility.MatchProcessStart(evt, source, processName, pids, commandLine))
                {
                    Console.WriteLine("Process Start");
                    pid = evt.ProcessID;
                    start = evt.TimeStampRelativeMSec;
                }
            };

            source.Source.Dynamic.AddCallbackForProviderEvent("HotReload", "HotReload/Start", evt =>
            {
                if (evt.PayloadStringByName("handlerType").ToLower() == "main")
                {
                    if (pid.HasValue)
                    {
                        if (firstHotReload)
                        {
                            results.Add(evt.TimeStampRelativeMSec - start);
                            start = evt.TimeStampRelativeMSec;
                        }
                        else
                        {
                            start = evt.TimeStampRelativeMSec;
                        }
                    }
                }
            });

            source.Source.Dynamic.AddCallbackForProviderEvent("HotReload", "HotReloadEnd", evt =>
            {
                if (evt.PayloadStringByName("handlerType").ToLower() == "main")
                {
                    if (pid.HasValue)
                    {
                        if (firstHotReload)
                        {
                            reloadResults.Add(evt.TimeStampRelativeMSec - start);
                            firstHotReload = false;
                        }
                        else
                        {
                            firstHotReload = true;
                            warmReloadResults.Add(evt.TimeStampRelativeMSec - start);
                            start = 0;
                            pid = null;
                        }
                    }
                }
            });

            source.Process();
        }

        return new[] {
            new Counter() { Name = "Time to Hot Reload Start", MetricName = "ms", TopCounter=true, Results = results.ToArray() },
            new Counter() { Name = "First Hot Reload Time", MetricName = "ms", TopCounter=true, DefaultCounter=true, Results = reloadResults.ToArray() },
            new Counter() { Name = "Second Hot Reload Time", MetricName = "ms", TopCounter=true, Results = warmReloadResults.ToArray() },
        };
    }
}
