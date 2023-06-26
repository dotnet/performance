using Microsoft.Diagnostics.Tracing;
using Reporting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace ScenarioMeasurement;

public class InnerLoopParser : IParser
{
    private readonly bool useLoggingExtension = false;

    public InnerLoopParser()
    {

    }
    public InnerLoopParser(bool _useLoggingExtension)
    {
        useLoggingExtension = !_useLoggingExtension;
    }
    public void EnableKernelProvider(ITraceSession kernel)
    {
        kernel.EnableKernelProvider(TraceSessionManager.KernelKeyword.Process, TraceSessionManager.KernelKeyword.Thread, TraceSessionManager.KernelKeyword.ContextSwitch);
    }

    public void EnableUserProviders(ITraceSession user)
    {
        user.EnableUserProvider("InnerLoopMarkerEventSource", TraceEventLevel.Verbose);
        if(useLoggingExtension)
        {
            user.EnableUserProvider("Microsoft-Extensions-Logging", TraceEventLevel.Verbose);
        }
    }

    public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
    {
        var results = new List<double>();
        var threadTimes = new List<double>();
        double threadTime = 0;
        var ins = new Dictionary<int, double>();
        double start = -1;
        int? pid = null;
        var firstRun = new List<List<double>>();
        var secondRun = new List<List<double>>();
        using (var source = new TraceSourceManager(mergeTraceFile))
        {

            source.Kernel.ProcessStart += evt =>
            {
                if (!pid.HasValue && ParserUtility.MatchProcessStart(evt, source, processName, pids, commandLine))
                {
                    pid = evt.ProcessID;
                    start = evt.TimeStampRelativeMSec;
                }
            };

            if (source.IsWindows)
            {
                ((ETWTraceEventSource)source.Source).Kernel.ThreadCSwitch += evt =>
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
            }

            if(useLoggingExtension)
            {
                source.Source.Dynamic.AddCallbackForProviderEvent("Microsoft-Extensions-Logging", "FormattedMessage", evt =>
                {
                    if(evt.PayloadString(5).ToLower() == "hosting started")
                    {
                        if (pid.HasValue)
                        {
                            results.Add(evt.TimeStampRelativeMSec - start);
                            pid = null;
                            start = 0;
                            if (source.IsWindows)
                            {
                                threadTimes.Add(threadTime);
                                threadTime = 0;
                            }
                        }
                    }
                });
            }
            else
            {
                source.Kernel.ProcessStop += evt =>
                {
                    if (pid.HasValue && ParserUtility.MatchSingleProcessID(evt, source, (int)pid))
                    {
                        results.Add(evt.TimeStampRelativeMSec - start);
                        pid = null;
                        start = 0;
                        if (source.IsWindows)
                        {
                            threadTimes.Add(threadTime);
                            threadTime = 0;
                        }
                    }
                };
            }

            source.Source.Dynamic.AddCallbackForProviderEvent("InnerLoopMarkerEventSource", "Split", evt =>
            {
                if(firstRun.Count == 0)
                {
                    firstRun.Add(new List<double>());
                    firstRun.Add(new List<double>());
                }
                firstRun[0].AddRange(results);
                firstRun[1].AddRange(threadTimes);
                results = new List<double>(); 
                threadTimes = new List<double>();
            });

            source.Source.Dynamic.AddCallbackForProviderEvent("InnerLoopMarkerEventSource", "EndIteration", evt =>
            {
                if(secondRun.Count == 0)
                {
                    secondRun.Add(new List<double>());
                    secondRun.Add(new List<double>());
                }
                secondRun[0].AddRange(results);
                secondRun[1].AddRange(threadTimes);
                results = new List<double>(); 
                threadTimes = new List<double>();
            });

            source.Process();
        }

        var diffGS = new List<double>();
        var diffTOT = new List<double>();
        for(var i = 0; i < firstRun[0].Count; i++)
        {
            diffGS.Add(firstRun[0][i] - secondRun[0][i]);
        }
        for(var i = 0; i < firstRun[1].Count; i++)
        {
            diffTOT.Add(firstRun[1][i] - secondRun[1][i]);
        }

        return new[] {
            new Counter() { Name = "Generic Startup First Run", MetricName = "ms", TopCounter=true, Results = firstRun[0].ToArray() },
            new Counter() { Name = "Generic Startup Second Run", MetricName = "ms", TopCounter=true, Results = secondRun[0].ToArray() },
            new Counter() { Name = "Generic Startup Diff", MetricName = "ms", DefaultCounter=true, TopCounter=true, Results = diffGS.ToArray() },
            new Counter() { Name = "Time on Thread Diff", MetricName = "ms", TopCounter=true, Results = diffTOT.ToArray() }
        };
    }
}
