using Microsoft.Diagnostics.Tracing;
using Reporting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace ScenarioMeasurement;

public class InnerLoopMsBuildParser : IParser
{
    public void EnableKernelProvider(ITraceSession kernel)
    {
        kernel.EnableKernelProvider(TraceSessionManager.KernelKeyword.Process, TraceSessionManager.KernelKeyword.Thread, TraceSessionManager.KernelKeyword.ContextSwitch);
    }

    public void EnableUserProviders(ITraceSession user)
    {
        user.EnableUserProvider("InnerLoopMarkerEventSource", TraceEventLevel.Verbose);
        user.EnableUserProvider("Microsoft-Build", TraceEventLevel.Verbose);
    }

    public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
    {
        var results = new List<double>();
        var threadTimes = new List<double>();
        var buildEvalTime = new List<double>();
        double threadTime = 0;
        var ins = new Dictionary<int, double>();
        double start = -1;
        double buildEvalStart = -1;
        int? pid = null;
        var firstRun = new Dictionary<string, List<double>>();
        var secondRun = new Dictionary<string, List<double>>();
        var currentRun = firstRun;
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


            source.Kernel.ProcessStop += evt =>
            {
                if (pid.HasValue && ParserUtility.MatchSingleProcessID(evt, source, (int)pid))
                {
                    if(!currentRun.ContainsKey(evt.EventName))
                    {
                        currentRun.Add(evt.EventName, new List<double>());
                        currentRun[evt.EventName].Add(evt.TimeStampRelativeMSec - start);
                    }
                    else
                    {
                        currentRun[evt.EventName].Add(evt.TimeStampRelativeMSec - start);
                    }
                    results.Add(evt.TimeStampRelativeMSec - start);
                    pid = null;
                    start = 0;
                    if (source.IsWindows)
                    {
                        if(!currentRun.ContainsKey("ThreadCSwitch"))
                        {
                            currentRun.Add("ThreadCSwitch", new List<double>());
                            currentRun["ThreadCSwitch"].Add(threadTime);
                        }
                        else
                        {
                            currentRun["ThreadCSwitch"].Add(threadTime);
                        }
                        threadTimes.Add(threadTime);
                        threadTime = 0;
                    }
                }
            };


            source.Source.Dynamic.AddCallbackForProviderEvent("Microsoft-Build", "Evaluate/Start", evt =>
            {
                if(pid.HasValue && evt.ProcessID == pid.Value)
                {
                    buildEvalStart = evt.TimeStampRelativeMSec;
                }
            });

            source.Source.Dynamic.AddCallbackForProviderEvent("Microsoft-Build", "Evaluate/Stop", evt =>
            {
                if(pid.HasValue && evt.ProcessID == pid.Value)
                {
                    if(!currentRun.ContainsKey(evt.EventName))
                    {
                        currentRun.Add(evt.EventName, new List<double>());
                        currentRun[evt.EventName].Add(evt.TimeStampRelativeMSec - buildEvalStart);
                    }
                    else
                    {
                        currentRun[evt.EventName].Add(evt.TimeStampRelativeMSec - buildEvalStart);
                    }
                }
            });

            source.Source.Dynamic.AddCallbackForProviderEvent("InnerLoopMarkerEventSource", "Split", evt =>
            {
                currentRun = secondRun;
            });

            source.Source.Dynamic.AddCallbackForProviderEvent("InnerLoopMarkerEventSource", "EndIteration", evt =>
            {
                currentRun = firstRun;
            });

            source.Process();
        }

        var diffGS = new List<double>();
        var diffTOT = new List<double>();
        var diffEBT = new List<double>();

        for(var i = 0; i < firstRun["Process/Stop"].Count; i++)
        {
            diffGS.Add(firstRun["Process/Stop"][i] - secondRun["Process/Stop"][i]);
        }
        for(var i = 0; i < firstRun["ThreadCSwitch"].Count; i++)
        {
            diffTOT.Add(firstRun["ThreadCSwitch"][i] - secondRun["ThreadCSwitch"][i]);
        }
        for(var i = 0; i < firstRun["Evaluate/Stop"].Count; i++)
        {
            diffEBT.Add(firstRun["Evaluate/Stop"][i] - secondRun["Evaluate/Stop"][i]);
        }

        return new[] {
            new Counter() { Name = "Generic Startup First Run", MetricName = "ms", TopCounter=true, Results = firstRun["Process/Stop"].ToArray() },
            new Counter() { Name = "Generic Startup Second Run", MetricName = "ms", TopCounter=true, Results = secondRun["Process/Stop"].ToArray() },
            new Counter() { Name = "Generic Startup Diff", MetricName = "ms", DefaultCounter=true, TopCounter=true, Results = diffGS.ToArray() },
            new Counter() { Name = "Time on Thread Diff", MetricName = "ms", TopCounter=true, Results = diffTOT.ToArray() },
            new Counter() { Name = "Build Evaluate Time First Run", MetricName = "ms", TopCounter=true, Results = firstRun["Evaluate/Stop"].ToArray() },
            new Counter() { Name = "Build Evaluate Time Second Run", MetricName = "ms", TopCounter=true, Results = secondRun["Evaluate/Stop"].ToArray() },
            new Counter() { Name = "Build Evaluate Time Diff", MetricName = "ms", TopCounter=true, Results = diffEBT.ToArray() }
        };
    }
}
