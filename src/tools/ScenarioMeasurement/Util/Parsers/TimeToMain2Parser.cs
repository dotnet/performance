using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Diagnostics.Tracing;
using Reporting;
using ScenarioMeasurement.TraceEventParsers;

namespace ScenarioMeasurement;

public class TimeToMain2Parser : IParser
{
    private readonly Action<string, string> environmentVariableSetter;

    public TimeToMain2Parser(Action<string, string> environmentVariableSetter)
    {
        this.environmentVariableSetter = environmentVariableSetter;
    }

    public void EnableKernelProvider(ITraceSession kernel)
    {
        kernel.EnableKernelProvider(TraceSessionManager.KernelKeyword.Process, TraceSessionManager.KernelKeyword.Thread, TraceSessionManager.KernelKeyword.ContextSwitch);
    }

    public void EnableUserProviders(ITraceSession user)
    {
        user.EnableUserProvider(PerfLabValues.EventSourceName, TraceEventLevel.Verbose);
        if (user is LinuxTraceSession)
        {
            user.AddRawEvent($"{PerfLabValues.LTTngProviderName}:{PerfLabValues.OnMainEventName}");
        }
        var baseDir = Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD") ?? Path.GetFullPath("..");
        environmentVariableSetter?.Invoke("DOTNET_STARTUP_HOOKS", Path.Join(baseDir, PerfLabValues.ForwarderName, $"{PerfLabValues.ForwarderName}.dll"));
    }

    public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
    {
        var results = new List<double>();
        var threadTimes = new List<double>();
        double threadTime = 0;
        var ins = new Dictionary<int, double>();
        double start = -1;
        int? pid = null;
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

            source.Source.Kernel.ThreadCSwitch += evt =>
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

            if (source.IsWindows)
            {
                source.Source.Dynamic.AddCallbackForProviderEvent(PerfLabValues.EventSourceName, PerfLabValues.OnMainEventName, evt =>
                {
                    if (pid.HasValue && evt.ProcessID == pid && evt.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessOnMainEvent(evt, results, threadTimes, ref pid, ref threadTime, ref start);
                    }
                });
            }
            else
            {
                var perfLabParser = new PerfLabGenericEventSourceLTTngProviderParser(source.Source);
                perfLabParser.OnMain += evt =>
                {
                    if (pid.HasValue && evt.ProcessID == pid)
                    {
                        ProcessOnMainEvent(evt, results, threadTimes, ref pid, ref threadTime, ref start);
                    }
                };
                source.Source.Clr.EventSourceEvent += evt =>
                {
                    if (pid.HasValue && evt.ProcessID == pid && evt.EventID == PerfLabValues.OnMainEventId)
                    {
                        ProcessOnMainEvent(evt, results, threadTimes, ref pid, ref threadTime, ref start);
                    }
                };
            }

            source.Process();
        }

        var result = new List<Counter>();
        result.Add(new Counter() { Name = "Time To Main", MetricName = "ms", DefaultCounter = true, TopCounter = true, Results = results.ToArray() });
        // below is supported only on Windows, other platform will have 0s
        if (!threadTimes.All(x => x == 0))
        {
            result.Add(new Counter() { Name = "Time on Thread", MetricName = "ms", TopCounter = true, Results = threadTimes.ToArray() });
        };
        return result;
    }

    private static void ProcessOnMainEvent(TraceEvent evt, List<double> results, List<double> threadTimes,
        ref int? pid, ref double threadTime, ref double start)
    {
        results.Add(evt.TimeStampRelativeMSec - start);
        threadTimes.Add(threadTime);
        pid = null;
        threadTime = 0;
        start = 0;
    }
}
