using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Reporting;
using System.Collections.Generic;

namespace ScenarioMeasurement
{
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
            var loadingParser = new EventParser("Loading");
            var emittingParser = new EventParser("Emitting");
            var jittingParser = new EventParser("Jit");
            var compilationParser = new EventParser("Compilation");


            using (var source = new TraceSourceManager(mergeTraceFile))
            {
                var dynamicParser = new DynamicTraceEventParser(source.Source);

                loadingParser.AddStartStopCallbacks(source, dynamicParser, ProviderName, pids);
                emittingParser.AddStartStopCallbacks(source, dynamicParser, ProviderName, pids);
                jittingParser.AddStartStopCallbacks(source, dynamicParser, ProviderName, pids);
                compilationParser.AddStartStopCallbacks(source, dynamicParser, ProviderName, pids);

                source.Process();
            }

            var processTimeParser = new ProcessTimeParser();
            Counter processTimeCounter = null;
            foreach (var counter in processTimeParser.Parse(mergeTraceFile, processName, pids, commandLine))
            {
                if(counter.Name == "Process Time"){
                    processTimeCounter = counter;
                }
            }

            return new[] {
                processTimeCounter,
                new Counter() { Name = "Loading Interval", MetricName = "ms", DefaultCounter=false, TopCounter=true, Results = loadingParser.Intervals.ToArray() },
                new Counter() { Name = "Emitting Interval", MetricName = "ms", DefaultCounter=false, TopCounter=true, Results = emittingParser.Intervals.ToArray() },
                new Counter() { Name = "Jit Interval", MetricName = "ms", DefaultCounter=false, TopCounter=true, Results = jittingParser.Intervals.ToArray() },
                new Counter() { Name = "Compilation Interval", MetricName = "ms", DefaultCounter=false, TopCounter=true, Results = compilationParser.Intervals.ToArray() }
            };
        }
    }

    public sealed class EventParser
    {
        public string EventName { get; private set; }
        public Stack<double> Intervals { get; private set; } = new Stack<double>();
        private int? PrevPid = null;
        private int? Pid = null;
        private double Start = 0;
        private double Interval = 0;   

        public EventParser(string eventName)
        {
            EventName = eventName;
        }

        public void AddEventStartCallback(TraceSourceManager source, DynamicTraceEventParser dynamicParser, string provider, IList<int> pids)
        {
            dynamicParser.AddCallbackForProviderEvent(provider, $"{EventName}/Start", evt =>
            {
                if (!Pid.HasValue && ParserUtility.MatchProcessID(evt, source, pids))
                {
                    // For jitting, get the sum of multiple start&stop duration within each process
                    if (!PrevPid.HasValue || !ParserUtility.MatchSingleProcessID(evt, source, (int)PrevPid))
                    {
                        // Initialize interval for another process
                        Interval = 0;
                    }
                    Pid = evt.ProcessID;
                    Start = evt.TimeStampRelativeMSec;
                }
            });
        }

        public void AddEventStopCallback(TraceSourceManager source, DynamicTraceEventParser dynamicParser, string provider)
        {
            dynamicParser.AddCallbackForProviderEvent(provider, $"{EventName}/Stop", evt =>
            {
                if (Pid.HasValue && ParserUtility.MatchSingleProcessID(evt, source, (int)Pid))
                {
                    Interval += evt.TimeStampRelativeMSec - Start;
                    if (PrevPid.HasValue && ParserUtility.MatchSingleProcessID(evt, source, (int)PrevPid))
                    {
                        // Initialize interval for another process
                        double lastValue = Intervals.Pop();
                        Intervals.Push(lastValue + (evt.TimeStampRelativeMSec - Start));
                    }
                    else
                    {
                        Intervals.Push(Interval);
                    }
                    Start = 0;
                    PrevPid = Pid;
                    Pid = null;
                }
            });
        }

        public void AddStartStopCallbacks(TraceSourceManager source, DynamicTraceEventParser dynamicParser, string provider, IList<int> pids)
        {
            AddEventStartCallback(source, dynamicParser, provider, pids);
            AddEventStopCallback(source, dynamicParser, provider);
        }
    }
}
