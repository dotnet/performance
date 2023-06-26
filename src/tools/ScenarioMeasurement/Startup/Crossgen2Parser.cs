using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Reporting;
using System;
using System.Collections.Generic;

namespace ScenarioMeasurement;

public class Crossgen2Parser : IParser
{
    public static readonly string ProviderName = "Microsoft-ILCompiler-Perf";
    public void EnableKernelProvider(ITraceSession kernel)
    {
        kernel.EnableKernelProvider(TraceSessionManager.KernelKeyword.Process, TraceSessionManager.KernelKeyword.Thread, TraceSessionManager.KernelKeyword.ContextSwitch);
    }

    public void EnableUserProviders(ITraceSession user)
    {
        user.EnableUserProvider(ProviderName, TraceEventLevel.Verbose);
    }

    public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
    {
        var loadingParser = new EventParser("Loading", (1, 2));
        var emittingParser = new EventParser("Emitting", (3, 4));
        var compilationParser = new EventParser("Compilation", (5, 6));
        var jittingParser = new EventParser("Jit", (7, 8));
        

        using (var source = new TraceSourceManager(mergeTraceFile))
        {
            var dynamicParser = new DynamicTraceEventParser(source.Source);
            var clrParser = new ClrTraceEventParser(source.Source);

            loadingParser.AddStartStopCallbacks(source, dynamicParser, clrParser, ProviderName, pids);
            emittingParser.AddStartStopCallbacks(source, dynamicParser, clrParser, ProviderName, pids);
            compilationParser.AddStartStopCallbacks(source, dynamicParser, clrParser, ProviderName, pids);
            jittingParser.AddStartStopCallbacks(source, dynamicParser, clrParser, ProviderName, pids);

            source.Process();
        }

        var processTimeParser = new ProcessTimeParser();
        Counter processTimeCounter = null;
        Counter timeOnThread = null;
        if (!Util.IsWindows())
        {
            processName = "corerun"; 
        }
        foreach (var counter in processTimeParser.Parse(mergeTraceFile, processName, pids, commandLine))
        {
            if (counter.Name == "Process Time")
            {
                processTimeCounter = counter;
            }
            if (counter.Name == "Time on Thread")
            {
                timeOnThread = counter;
            }
        }

        var counters =  new List<Counter> {
            processTimeCounter,
            new Counter() { Name = "Loading Interval", MetricName = "ms", DefaultCounter=false, TopCounter=true, Results = loadingParser.Intervals.ToArray() },
            new Counter() { Name = "Emitting Interval", MetricName = "ms", DefaultCounter=false, TopCounter=true, Results = emittingParser.Intervals.ToArray() },
            new Counter() { Name = "Jit Interval", MetricName = "ms", DefaultCounter=false, TopCounter=true, Results = jittingParser.Intervals.ToArray() },
            new Counter() { Name = "Compilation Interval", MetricName = "ms", DefaultCounter=false, TopCounter=true, Results = compilationParser.Intervals.ToArray() }
        };

        // Time on Thread is currently only supported on Windows.
        if(timeOnThread != null)
        {
            counters.Add(timeOnThread);
        }
        
        return counters.ToArray();
    }
}

public sealed class EventParser
{
    public string EventName { get; private set; }
    public (int StartEventID, int StopEventID) EventID { get; private set; } // (StartEventID, StopEventID)
    public Stack<double> Intervals { get; private set; } = new Stack<double>(); // final counter results
    private int? PrevPid = null; // pid of the previous event, used to track multiple events of the same type (jitting) within a single process
    private int? Pid = null; // pid of the current event
    private double Start = 0; // start of the current event

    public EventParser(string eventName, (int StartEventID, int StopEventID) eventID)
    {
        EventName = eventName;
        EventID = eventID;
    }

    public void AddEventStartCallback(TraceSourceManager source, DynamicTraceEventParser dynamicParser, ClrTraceEventParser clrParser, string provider, IList<int> pids)
    {
        if (source.IsWindows)
        {
            // Use dynamic parser for Windows events because the provider is Microsoft-ILCompiler-Perf
            dynamicParser.AddCallbackForProviderEvent(provider, $"{EventName}/Start", evt =>
            {
                if (!Pid.HasValue && ParserUtility.MatchProcessID(evt, source, pids))
                {
                    ParseStartEvent(evt);
                }
            });
        }
        else
        {
            // Use clr parser for Linux events because the provider is DotnetRuntime and event name is EventSource for all clr events 
            clrParser.EventSourceEvent += evt =>
            {
                // In addition to pid, match provider name and crossgen2 event name in the payload as well
                if (!Pid.HasValue &&
                   pids.Contains(evt.ProcessID) && 
                   // Check EventID but not EventName to skip calling PayloadByName() which lowers performance
                   evt.EventID == EventID.StartEventID)
                {
                    ParseStartEvent(evt);
                }
            };
        }
    }

    public void AddEventStopCallback(TraceSourceManager source, DynamicTraceEventParser dynamicParser, ClrTraceEventParser clrParser, string provider)
    {
        if (source.IsWindows)
        {
            dynamicParser.AddCallbackForProviderEvent(provider, $"{EventName}/Stop", evt =>
            {
                if (Pid.HasValue && ParserUtility.MatchSingleProcessID(evt, source, (int)Pid))
                {
                    ParseStopEvent(evt, source);
                }
            });
        }
        else
        {
            clrParser.EventSourceEvent += evt =>
            {
                if (Pid.HasValue &&
                    evt.ProcessID == Pid &&
                    evt.EventID == EventID.StopEventID)
                {
                    ParseStopEvent(evt, source);
                }
            };
        }

    }

    public void AddStartStopCallbacks(TraceSourceManager source, DynamicTraceEventParser dynamicParser, ClrTraceEventParser clrParser, string provider, IList<int> pids)
    {
        AddEventStartCallback(source, dynamicParser, clrParser, provider, pids);
        AddEventStopCallback(source, dynamicParser, clrParser, provider);
    }

    private void ParseStartEvent(TraceEvent evt)
    {
        Pid = evt.ProcessID;
        Start = evt.TimeStampRelativeMSec;
    }

    private void ParseStopEvent(TraceEvent evt, TraceSourceManager source)
    {
        // For some crossgen2 events (ex: jitting), there could be multiple start&stop pairs within one process, thus we sum up 
        // time elapsed between each pair as the interval of this event. 

        // Get time elapsed for this pair of start&stop events
        var interval = evt.TimeStampRelativeMSec - Start;
        // If previous pid exists, this is the same process and time elapsed is added to the last value in the stack.
        if (PrevPid.HasValue && evt.ProcessID == PrevPid)
        {
            var lastValue = Intervals.Pop();
            Intervals.Push(lastValue + interval);
        }
        // If previous pid doesn't exist, this is the next process and time elapsed is a new value pushed to the stack.
        else
        {
            Intervals.Push(interval);
        }
        Start = 0;
        PrevPid = Pid;
        Pid = null;
    }

}
