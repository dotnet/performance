// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #define NEW_JOIN_ANALYSIS // Also must do this in MoreAnalysis.cs

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Diagnostics.Tracing.StackSources;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Stacks;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System.Diagnostics;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Utilities;

namespace GCPerf
{
    using ProcessID = Int32;
    using ThreadID = Int32;
    using EtlxNS = Microsoft.Diagnostics.Tracing.Etlx;

    public class StackView
    {
        private static readonly char[] SymbolSeparator = new char[] { '!' };

        private EtlxNS.TraceLog _traceLog;
        private StackSource _rawStackSource;
        private SymbolReader _symbolReader;
        private CallTree _callTree;
        private List<CallTreeNodeBase> _byName;
        private HashSet<string> _resolvedSymbolModules = new HashSet<string>();

        public StackView(EtlxNS.TraceLog traceLog, StackSource stackSource, SymbolReader symbolReader)
        {
            _traceLog = traceLog;
            _rawStackSource = stackSource;
            _symbolReader = symbolReader;
            LookupWarmNGENSymbols();
        }

        public CallTree CallTree
        {
            get
            {
                if (_callTree == null)
                {
                    FilterStackSource filterStackSource = new FilterStackSource(new FilterParams(), _rawStackSource, ScalingPolicyKind.ScaleToData);
                    _callTree = new CallTree(ScalingPolicyKind.ScaleToData)
                    {
                        StackSource = filterStackSource
                    };
                }
                return _callTree;
            }
        }

        private IEnumerable<CallTreeNodeBase> ByName
        {
            get
            {
                if (_byName == null)
                {
                    _byName = CallTree.ByIDSortedExclusiveMetric();
                }

                return _byName;
            }
        }

        public CallTreeNodeBase FindNodeByName(string nodeNamePat)
        {
            var regEx = new Regex(nodeNamePat, RegexOptions.IgnoreCase);
            foreach (var node in ByName)
            {
                if (regEx.IsMatch(node.Name))
                {
                    return node;
                }
            }
            return CallTree.Root;
        }
        public CallTreeNode GetCallers(string focusNodeName)
        {
            var focusNode = FindNodeByName(focusNodeName);
            return AggregateCallTreeNode.CallerTree(focusNode);
        }
        public CallTreeNode GetCallees(string focusNodeName)
        {
            var focusNode = FindNodeByName(focusNodeName);
            return AggregateCallTreeNode.CalleeTree(focusNode);
        }

        public CallTreeNodeBase GetCallTreeNode(string symbolName)
        {
            string[] symbolParts = symbolName.Split(SymbolSeparator);
            if (symbolParts.Length != 2)
            {
                return null;
            }

            // Try to get the call tree node.
            CallTreeNodeBase node = FindNodeByName(Regex.Escape(symbolName));

            // Check to see if the node matches.
            if (node.Name.StartsWith(symbolName, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            // Check to see if we should attempt to load symbols.
            if (!_resolvedSymbolModules.Contains(symbolParts[0]))
            {
                // Look for an unresolved symbols node for the module.
                string unresolvedSymbolsNodeName = symbolParts[0] + "!?";
                node = FindNodeByName(unresolvedSymbolsNodeName);
                if (node.Name.Equals(unresolvedSymbolsNodeName, StringComparison.OrdinalIgnoreCase))
                {
                    // Symbols haven't been resolved yet.  Try to resolve them now.
                    EtlxNS.TraceModuleFile moduleFile = _traceLog.ModuleFiles.Where(m => m.Name.Equals(symbolParts[0], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (moduleFile != null)
                    {
                        // Special handling for NGEN images.
                        if(symbolParts[0].EndsWith(".ni", StringComparison.OrdinalIgnoreCase))
                        {
                            SymbolReaderOptions options = _symbolReader.Options;
                            try
                            {
                                _symbolReader.Options = SymbolReaderOptions.CacheOnly;
                                _traceLog.CallStacks.CodeAddresses.LookupSymbolsForModule(_symbolReader, moduleFile);
                            }
                            finally
                            {
                                _symbolReader.Options = options;
                            }
                        }
                        else
                        {
                            _traceLog.CallStacks.CodeAddresses.LookupSymbolsForModule(_symbolReader, moduleFile);
                        }
                        InvalidateCachedStructures();
                    }
                }

                // Mark the module as resolved so that we don't try again.
                _resolvedSymbolModules.Add(symbolParts[0]);

                // Try to get the call tree node one more time.
                node = FindNodeByName(Regex.Escape(symbolName));

                // Check to see if the node matches.
                if (node.Name.StartsWith(symbolName, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }
            }

            return null;
        }

        private void LookupWarmNGENSymbols()
        {
            TraceEventStackSource asTraceEventStackSource = GetTraceEventStackSource(_rawStackSource);
            if (asTraceEventStackSource == null)
            {
                return;
            }

            SymbolReaderOptions savedOptions = _symbolReader.Options;
            try
            {
                // NGEN PDBs (even those not yet produced) are considered to be in the cache.
                _symbolReader.Options = SymbolReaderOptions.CacheOnly;

                // Resolve all NGEN images.
                asTraceEventStackSource.LookupWarmSymbols(1, _symbolReader, _rawStackSource, s => s.Name.EndsWith(".ni", StringComparison.OrdinalIgnoreCase));

                // Invalidate cached data structures to finish resolving symbols.
                InvalidateCachedStructures();
            }
            finally
            {
                _symbolReader.Options = savedOptions;
            }
        }

        /// <summary>
        /// Unwind the wrapped sources to get to a TraceEventStackSource if possible. 
        /// </summary>
        private static TraceEventStackSource GetTraceEventStackSource(StackSource source)
        {
            StackSourceStacks rawSource = source;
            TraceEventStackSource asTraceEventStackSource = null;
            for (; ; )
            {
                asTraceEventStackSource = rawSource as TraceEventStackSource;
                if (asTraceEventStackSource != null)
                {
                    return asTraceEventStackSource;
                }

                var asCopyStackSource = rawSource as CopyStackSource;
                if (asCopyStackSource != null)
                {
                    rawSource = asCopyStackSource.SourceStacks;
                    continue;
                }
                var asStackSource = rawSource as StackSource;
                if (asStackSource != null && asStackSource != asStackSource.BaseStackSource)
                {
                    rawSource = asStackSource.BaseStackSource;
                    continue;
                }
                return null;
            }
        }

        private void InvalidateCachedStructures()
        {
            _byName = null;
            _callTree = null;
        }
    }

#if !NEW_JOIN_ANALYSIS
    // With new join analysis, this will come from PerfView 
    [Obsolete]
    public struct ThreadIDAndTime
    {
        public readonly ThreadID ThreadID;
        public readonly long TimeQPC;

        public ThreadIDAndTime(ThreadID threadID, long timeQPC)
        {
            ThreadID = threadID;
            TimeQPC = timeQPC;
        }
    }
#endif

    public static class PythonnetUtil
    {
        // pythonnet doesn't handle out parameters, return an option instead
        // Can't be generic -- https://github.com/pythonnet/pythonnet/issues/962
        public static MarkInfo? TryGetValue(IReadOnlyDictionary<int, MarkInfo> d, int key) =>
            d.TryGetValue(key, out MarkInfo value) ? value : (MarkInfo?) null;
    }

    internal static class Util
    {
        public static void AssertEqualList<T>(IReadOnlyList<T> a, IReadOnlyList<T> b, Func<T, T, bool> eq)
        {
            if (!All(a.Zip(b, eq)))
            {
                string show(IReadOnlyList<T> l) => string.Join(", ", from x in l select x.ToString());
                Console.WriteLine("Expected lists to be equal:");
                Console.WriteLine(show(a));
                Console.WriteLine(show(b));
                Assert(false);
            }
        }

        public static bool All(IEnumerable<bool> e) =>
            e.All(b => b);

        public static IReadOnlyList<T> Repeat<T>(uint n, T value) =>
            (from i in Enumerable.Range(0, (int) n) select value).ToArray();

        public static U? Find<T, U>(IEnumerable<T> a, Func<T, U?> f) where U : struct
        {
            foreach (T t in a)
            {
                U? u = f(t);
                if (u != null)
                    return u;
            }
            return null;
        }

        public static T MaxBy<T, U>(IEnumerable<T> a, Func<T, U> f) where U : IComparable =>
            a.Aggregate((x, y) => f(x).CompareTo(f(y)) > 0 ? x : y);

        public static T? TryIndex<T>(IReadOnlyList<T> a, uint index) where T : struct =>
            index < a.Count ? a[(int) index] : (T?) null;

        public static IEnumerable<T> Unique<T>(IEnumerable<T> i)
        {
            HashSet<T> set = new HashSet<T>();
            foreach (T x in i)
            {
                if (!set.Contains(x))
                {
                    yield return x;
                    set.Add(x);
                }
            }
        }

        public static string NullableToString<T>(T? value) where T : struct =>
            value == null ? "null" : value.Value.ToString();

        public const ThreadID THREAD_ID_IDLE = 0;

        public static bool AboutEquals(double a, double b, double tolerance) => Math.Abs(a - b) <= tolerance;

        public static T NonNull<T>(T? t) where T : class =>
            t ?? throw new NullReferenceException();
        public static T NonNull<T>(T? t) where T : struct =>
            t ?? throw new NullReferenceException();

        public static void Assert(bool b, string message = "Assertion failed")
        {
            if (!b)
            {
                throw new Exception(message);
            }
        }
        public static void Assert(bool b, Func<string> getMessage)
        {
            if (!b)
            {
                throw new Exception(getMessage());
            }
        }

        public static void AssertAboutEquals(double a, double b, double tolerance = 0.001, string description = "")
        {
            if (!AboutEquals(a, b, tolerance))
            {
                throw new Exception($"{description}: Expected {a} ~= {b}");
            }
        }

        public static void AddToArray(double[] a, IReadOnlyList<double> b)
        {
            Util.Assert(a.Length == b.Count, "must be same length");
            for (int i = 0; i < a.Length; i++)
            {
                a[i] += b[i];
            }
        }

        public static bool IsEmpty<T>(IReadOnlyList<T> l) =>
            l.Count == 0;

        public static void AddToDictionary<K>(Dictionary<K, double> to, K key, double value)
        {
            double cur = to.TryGetValue(key, out var v) ? v : 0;
            to[key] = cur + value;
        }

        public static void AddToDictionary<K>(Dictionary<K, double> to, IReadOnlyDictionary<K, double> from)
        {
            foreach (KeyValuePair<K, double> pair in from)
            {
                AddToDictionary<K>(to, pair.Key, pair.Value);
            }
        }

        public static IReadOnlyDictionary<K, double> SumDictionaries<K>(IReadOnlyDictionary<K, double> a, IReadOnlyDictionary<K, double> b) =>
            SumDictionaries(new IReadOnlyDictionary<K, double>[] { a, b });

        public static IReadOnlyDictionary<K, double> SumDictionaries<K>(IEnumerable<IReadOnlyDictionary<K, double>> ds)
        {
            Dictionary<K, double> res = new Dictionary<K, double>();
            foreach (IReadOnlyDictionary<K, double> d in ds)
            {
                AddToDictionary<K>(res, d);
            }
            return res;
        }

        public static IReadOnlyList<T> EmptyReadOnlyList<T>() =>
            new T[0];

        public static IReadOnlyDictionary<K, V> EmptyReadOnlyDictionary<K, V>() =>
            new Dictionary<K, V>();

        public static IEnumerable<T> Single<T>(T value)
        {
            yield return value;
        }

        public static IEnumerable<uint> Range(uint n) =>
            Enumerable.Range(0, (int)n).Select(i => (uint)i);

        public static long MSecTo100NSec(double msec) =>
            (long)(msec * 10_000.0);

        public static T? OpLast<T>(List<T> l) where T : struct =>
            l.Any() ? l.Last() : (T?)null;

        public static ulong IntToULong(int i)
        {
            Debug.Assert(i >= 0);
            return (ulong)i;
        }
    }

    public readonly struct TimeSpan
    {
        public readonly double StartMSec;
        public readonly double EndMSec;

        private TimeSpan(double startMSec, double endMSec)
        {
            StartMSec = startMSec;
            EndMSec = endMSec;
        }

        public static TimeSpan FromStartEndMSec(double startMSec, double endMSec)
        {
            Util.Assert(startMSec <= endMSec, $"Duration must not be negative. Start: {startMSec}, end: {endMSec}");
            return new TimeSpan(startMSec, endMSec);
        }

        public static TimeSpan FromStartLengthMSec(double startMSec, double lengthMSec)
        {
            Util.Assert(lengthMSec >= 0, "Must have non-negative length");
            return new TimeSpan(startMSec, startMSec + lengthMSec);
        }

        public static TimeSpan FromStartLengthMSecAllowNegativeLength(double startMSec, double lengthMSec) =>
            lengthMSec < 0
                ? FromStartLengthMSec(startMSec + lengthMSec, -lengthMSec)
                : FromStartLengthMSec(startMSec, lengthMSec);

        public override string ToString() =>
            $"{StartMSec} {EndMSec} ({DurationMSec} ms)";

        public bool Contains(double timeMSec) =>
            StartMSec <= timeMSec && timeMSec <= EndMSec;
        public double DurationMSec =>
            EndMSec - StartMSec;

        public TimeSpan MergeWith(TimeSpan other)
        {
            Util.Assert(EndMSec == other.StartMSec, "must be adjacent");
            return FromStartEndMSec(StartMSec, other.EndMSec);
        }

        public static TimeSpan Empty(double timeMSec) =>
            FromStartLengthMSec(timeMSec, 0);

        public TimeSpan Union(TimeSpan other) =>
            FromStartEndMSec(Math.Min(StartMSec, other.StartMSec), Math.Max(EndMSec, other.EndMSec));

        public static TimeSpan Union(IEnumerable<TimeSpan> spans)
        {
            TimeSpan? ts = null;
            foreach (TimeSpan span in spans)
            {
                ts = ts == null ? span : Util.NonNull(ts).Union(span);
            }
            return ts ?? Empty(0);
        }
    }

    // Python needs this in a separate class to avoid TimeSpan referring to itself
    public static class TimeSpanUtil
    {
        public static TimeSpan FromStartEndMSec(double startMSec, double endMSec) =>
            TimeSpan.FromStartEndMSec(startMSec, endMSec);
    }


    public struct TraceTimeRange
    {
        public TraceTimeRange(double beginMsec, double endMsec)
        {
            BeginMsec = beginMsec;
            EndMsec = endMsec;
        }
        public readonly double BeginMsec;
        public readonly double EndMsec;
    }

    // TODO: Better name?
    public struct SampleMetrics
    {
        // Something to identify what the metrics are for. For example, this might be
        // the corresponding function name or perhaps (in a diff scenario) something
        // to identify the type of run or filter which is represented by this data.
        public string Identifier;

        public int InclusiveSamples;

        public int ExclusiveSamples;
    }

    /// <summary>
    /// Our representation of an ETL trace. Makes it easy to get to the important data
    /// that matters to our use cases.
    /// </summary>
    public class EtlTrace
    {
        public EtlTrace(string id, string file, string symPath)
        {
            Identifier = id;
            FilePath = file;
            SymPath = symPath;

            AllCpuStacks = null;
            FunctionNameToStacks = new Dictionary<string, StackSource>();
            FunctionNameToMetrics = new Dictionary<string, SampleMetrics>();
            TotalCpuSampleCount = 0;
        }

        // A string representing this trace. Ideally this name is meaningful enough
        // that when data is presented with this name, it is clear what scenario it
        // represents (for example, when diffing two traces a good example of names
        // would be "BeforeChangingFoo" and "AfterChangingFoo").
        public string Identifier;

        // Path to the ETL file
        public string FilePath;

        // Symbol path to use for this trace
        public string SymPath;

        // The total number of CPU samples in this trace
        public int TotalCpuSampleCount;

        // StackSource representing the stack for every CPU sample in the trace
        public TraceEventStackSource? AllCpuStacks;

        // Dictionaries to get from function name -> filtered stacks or to metrics.
        // Make this more efficient if needed. Most likely, it doesn't matter since the 
        // count of interesting functions is likely very small in practice.
        public Dictionary<string, StackSource> FunctionNameToStacks;
        public Dictionary<string, SampleMetrics> FunctionNameToMetrics;
    }

    /// <summary>
    /// Enum for indicating desired sort order
    /// </summary>
    public enum SortMetric
    {
        Inclusive,
        Exclusive,

        // TODO: Enable sorting by inc/exc diff
    }

    public enum ServerGcThreadState
    {
        Unknown = 0,            // Event capture occured in the middle of GC
        Ready = 1,              // GC thread is in processor run queue but not executing
        WaitingInJoin = 2,      // GC thread is waiting on other GC threads to synchronize
        SingleThreaded = 3,     // GC thread is last thread to synchronize
        WaitingInRestart = 4,   // GC thread is waiting on last thread to release join lock
    }

#if NEW_JOIN_ANALYSIS
    // This will be more usable by Python, which doesn't have out parameters.
    [Obsolete]
    public struct HistDict<T> where T : struct
    {
        private HistoryDictionary<T> inner;

        public HistDict(HistoryDictionary<T> inner)
        {
            this.inner = inner;
        }

        public T? get(ulong id, double timeMSec)
            => inner.TryGetValue(id, msecTo100NS(timeMSec), out var res) ? res : (T?) null;

        private static long msecTo100NS(double timeMSec) =>
            (long) (timeMSec * 10000.0);
    }
#endif

#if !NEW_JOIN_ANALYSIS
    // With NEW_JOIN_ANALYSIS this interface is in perfview
    [Obsolete]
    public interface IThreadIDToProcessID
    {
        int? ThreadIDToProcessID(int threadID, long timeQPC);
    }
#endif

    [Obsolete]
    public interface IProcessIDToProcessName
    {
        string? ProcessIDToProcessName(int processID, long timeQPC);
    }

#if NEW_JOIN_ANALYSIS
    [Obsolete]
    class ProcessIDToProcessNameFromHistoryDictionary : IProcessIDToProcessName
    {
        private HistoryDictionary<string> names;
        
        public ProcessIDToProcessNameFromHistoryDictionary(HistoryDictionary<string> names)
        {
            this.names = names;
        }

        public string? ProcessIDToProcessName(ProcessID processID, long timeQPC) =>
            names.TryGetValue(Util.IntToULong(processID), timeQPC, out string res) && res != ""
                ? res
                : (string?) null;
    }
#endif

    [Obsolete]
    class ProcessIDToProcessNameFromDictionary : IProcessIDToProcessName
    {
        private Dictionary<ProcessID, string> names;

        public ProcessIDToProcessNameFromDictionary(Dictionary<ProcessID, string> names)
        {
            this.names = names;
        }

        public string? ProcessIDToProcessName(ProcessID processID, long timeQPC) =>
            names.TryGetValue(processID, out string res) && res != ""
                ? res
                : (string?)null;
    }

    [Obsolete] // allow us to use experimental PerfView features
    public class Analysis
    {
#if NEW_JOIN_ANALYSIS
        [Obsolete]
        class MyThreadIDToProcessIDImpl : IThreadIDToProcessID
        {
            HistoryDictionary<ProcessID> d;
            public MyThreadIDToProcessIDImpl(HistoryDictionary<ProcessID> d)
            {
                this.d = d;
            }

            public ProcessID? ThreadIDToProcessID(ThreadID threadID, long timeQPC) =>
                d.TryGetValue((ulong)threadID, timeQPC, out ProcessID p) ? p : (ProcessID?) null;

            public IEnumerable<ThreadIDAndTime> ProcessIDToThreadIDsAndTimes(ProcessID processID) =>
                //TODO: perfview would probably be a lot more performant if it collapsed adjacent entries with the same value!
                Util.Unique(from entry in d.Entries where entry.Value == processID select new ThreadIDAndTime((ThreadID)entry.Key, entry.StartTime));
        }
#endif

        public class TracedProcesses
        {
            public readonly IReadOnlyDictionary<string, uint>? event_names;
            public readonly IReadOnlyCollection<double>? per_heap_history_times;
            public readonly TraceProcesses processes;
            public readonly IThreadIDToProcessID? my_thread_id_to_process_id;
            public readonly IThreadIDToProcessID? thread_id_to_process_id;
            public readonly IProcessIDToProcessName? process_id_to_process_name;
            // null if collectEventNames was not requested
            public readonly TimeSpan? events_time_span;

            public TracedProcesses(
                IReadOnlyDictionary<string, uint>? event_names,
                IReadOnlyCollection<double>? per_heap_history_times,
                TraceProcesses processes,
                IThreadIDToProcessID? my_thread_id_to_process_id,
                IThreadIDToProcessID? thread_id_to_process_id,
                IProcessIDToProcessName? process_id_to_process_name,
                TimeSpan? events_time_span)
            {
                this.event_names = event_names;
                this.per_heap_history_times = per_heap_history_times;
                this.processes = processes;
                this.my_thread_id_to_process_id = my_thread_id_to_process_id;
                this.thread_id_to_process_id = thread_id_to_process_id;
                this.process_id_to_process_name = process_id_to_process_name;
                this.events_time_span = events_time_span;
            }
        }

        private static Regex? ToRegex(string? s) =>
            s == null ? null : new Regex(s, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static void PrintEvents(
            string tracePath,
            TimeSpan? timeSpan,
            string? includeRegex,
            string? excludeRegex,
            ThreadID? threadID,
            uint? maxEvents,
            bool useTraceLog)
        {
            Regex? includeRgx = ToRegex(includeRegex);
            Regex? excludeRgx = ToRegex(excludeRegex);
            Func<TraceEvent, bool> filter = te => FilterEvent(te, timeSpan, includeRgx, excludeRgx, threadID);
            if (useTraceLog)
            {
                PrintEventsWithTraceLog(tracePath, filter, maxEvents);
            }
            else
            {
                PrintEventsWithoutTraceLog(tracePath, filter, maxEvents);
            }
        }

        public static void PrintEventsWithoutTraceLog(string tracePath, Func<TraceEvent, bool> filter, uint? maxEvents)
        {
            using TraceEventDispatcher source = TraceEventDispatcher.GetDispatcherFromFileName(tracePath)!;
            Util.Assert(source != null, $"PrintEventsWithoutTraceLog: Bad path {tracePath}.");

            uint n = 0;
            source!.AllEvents += (TraceEvent te) =>
            {
                if ((maxEvents == null || n < maxEvents.Value) && filter(te))
                {
                    PrintEvent(te);
                    n++;
                }
            };
            TraceLoadedDotNetRuntimeExtensions.NeedLoadedDotNetRuntimes(source);
            source.Process();
            Console.WriteLine("done");
        }

        public static void PrintEventsWithTraceLog(string tracePath, Func<TraceEvent, bool> filter, uint? maxEvents)
        {
            Console.WriteLine("Getting trace log");
            Microsoft.Diagnostics.Tracing.Etlx.TraceLog log = Microsoft.Diagnostics.Tracing.Etlx.TraceLog.OpenOrConvert(tracePath);
            Console.WriteLine("done");
            uint n = 0;
            foreach (TraceEvent te in log.Events.ByEventType<TraceEvent>())
            {
                if (filter(te))
                {
                    PrintEvent(te);
                    n++;
                    if (n >= maxEvents)
                        break;
                }
            }
        }

        private static bool FilterEvent(TraceEvent te, TimeSpan? timeSpan, Regex? includeRgx, Regex? excludeRgx, ThreadID? threadID) =>
            !(te is UnhandledTraceEvent)
            && (timeSpan == null || timeSpan.Value.Contains(te.TimeStampRelativeMSec))
            && (includeRgx == null || includeRgx.IsMatch(te.EventName))
            && (excludeRgx == null || !excludeRgx.IsMatch(te.EventName))
            && (threadID == null || ThreadIDMatches(te, threadID.Value));

        private static bool ThreadIDMatches(TraceEvent te, ThreadID? threadID) =>
            te.ThreadID == threadID ||
            (te is CSwitchTraceData cs && (cs.OldThreadID == threadID || cs.NewThreadID == threadID));

        private static void PrintEvent(TraceEvent te)
        {
            StringBuilder sb = new StringBuilder();
            te.ToXml(sb);
            sb.Append($" Processor: {te.ProcessorNumber}");
            Console.WriteLine(sb.ToString());
        }

        public static void SliceTraceFile(string inputTracePath, string outputTracePath, TimeSpan timeSpan)
        {
            using ETWReloggerTraceEventSource relogger = new ETWReloggerTraceEventSource(inputTracePath, outputTracePath);
            relogger.AllEvents += (TraceEvent data) =>
            {
                if (timeSpan.Contains(data.TimeStampRelativeMSec))
                {
                    relogger.WriteEvent(data);
                }
            };
            relogger.Process();
        }

        public static TracedProcesses GetTracedProcesses(string tracePath, bool collectEventNames, bool collectPerHeapHistoryTimes)
        {
            using TraceEventDispatcher source = TraceEventDispatcher.GetDispatcherFromFileName(tracePath)!;
            Util.Assert(source != null, $"GetTracedProcesses: Bad path {tracePath}.");

            Dictionary<string, uint>? eventNames = null;
            List<double>? perHeapHistoryTimes = null;

            double? firstEventTimeMSec = null;
            double? lastEventTimeMSec = null;

            if (collectPerHeapHistoryTimes)
            {
                perHeapHistoryTimes = new List<double>();
                source!.Clr.GCPerHeapHistory += (GCPerHeapHistoryTraceData data) =>
                {
                    perHeapHistoryTimes.Add(data.TimeStampRelativeMSec);
                };
            }

            //HistoryDictionary<string> processNames = new HistoryDictionary<string>();
            List<(ProcessID, long)> processIDsAndTimes = new List<(int, long)>();
            //HashSet<ProcessID> seenProcessIDs = new HashSet<ProcessID>();

#if NEW_JOIN_ANALYSIS
            // PerfView should do this, but checking their work ... looks like my version gets more!
            HistoryDictionary<ProcessID> myThreadIDToProcessID = new HistoryDictionary<ProcessID>(1<<12);
#endif

            eventNames = new Dictionary<string, uint>();
            source!.AllEvents += (TraceEvent te) =>
            {
                /*if (tt is CSwitchTraceData cs)
                {
                    //processNames.Add(cs.OldProcessID, )
                    //seenProcessIDs.Add(cs.OldProcessID);
                    //seenProcessIDs.Add(cs.NewProcessID);
                    processIDsAndTimes.Add((cs.OldProcessID, cs.TimeStampQPC));
                    processIDsAndTimes.Add((cs.NewProcessID, cs.TimeStampQPC));
                }*/

#if NEW_JOIN_ANALYSIS
                if (te.ThreadID != -1 && te.ProcessID != -1)
                {
                    //TODO: HistoryDictionary do this automatically in Add?
                    if (myThreadIDToProcessID.TryGetValue((ulong)te.ThreadID, GetTimeQpc(te), out ProcessID pid) && pid == te.ProcessID)
                    {
                        // do nothing, already handled
                    }
                    else
                    {
                        myThreadIDToProcessID.Add((ulong)te.ThreadID, GetTimeQpc(te), te.ProcessID);
                    }
                }
#endif

                firstEventTimeMSec = firstEventTimeMSec ?? te.TimeStampRelativeMSec;
                lastEventTimeMSec = te.TimeStampRelativeMSec;

                if (collectEventNames)
                {
                    eventNames.TryGetValue(te.EventName, out uint eventCount);
                    eventNames[te.EventName] = eventCount + 1;
                }
            };
            

            var kernelParser = new KernelTraceEventParser(source);

            TraceLoadedDotNetRuntimeExtensions.NeedLoadedDotNetRuntimes(source);
            source.Process();

            TraceProcesses processes = TraceProcessesExtensions.Processes(source);

            /*Dictionary<ProcessID, string> processNames = new Dictionary<ProcessID, string>();
            foreach (TraceProcess process in processes)
            {
                if (processNames.TryGetValue(process.ProcessID, out string name))
                {
                    Debug.Assert(name == process.Name,
                        $"process {process.ProcessID} name was {name}, but now is {process.Name}");
                }
                else
                {
                    processNames.Add(process.ProcessID, process.Name);
                }
            }*/

#if NEW_JOIN_ANALYSIS
            HistoryDictionary<string> processNames = new HistoryDictionary<string>(initialSize: 1024);
            foreach ((ProcessID pid, long timeQPC) in processIDsAndTimes)
            {
                //Console.WriteLine($"Seen process ID: {pid} at {timeQPC}");
                string name = source.ProcessName(pid, timeQPC);
                if (name == "")
                {
                    Console.WriteLine("Skipping, empty process name");
                }
                else
                {
                    processNames.Add(Util.IntToULong(pid), timeQPC, name);
                }
            }
#endif

            TimeSpan? events_time_span = firstEventTimeMSec == null || lastEventTimeMSec == null
                ? (TimeSpan?)null
                : TimeSpan.FromStartEndMSec(firstEventTimeMSec.Value, lastEventTimeMSec.Value);
            return new TracedProcesses(
                event_names: eventNames,
                per_heap_history_times: perHeapHistoryTimes,
                processes: processes,
#if NEW_JOIN_ANALYSIS
                my_thread_id_to_process_id: new MyThreadIDToProcessIDImpl(myThreadIDToProcessID),
                thread_id_to_process_id: kernelParser.ThreadIDToProcessIDGetter,
                process_id_to_process_name: new ProcessIDToProcessNameFromHistoryDictionary(processNames),
#else
                my_thread_id_to_process_id: null,
                thread_id_to_process_id: null,
                process_id_to_process_name: null,
#endif
                events_time_span: events_time_span);
        }

        private static long GetTimeQpc(TraceEvent te) =>
            (long) (te.TimeStampRelativeMSec * 10000);


		private static ServerGcThreadState GetNewGcThreadState(ServerGcThreadState previousState, GcJoin joinEvent)
		{
			// NOTE: Based on the way GC join events are fired, we can generally expect
			// some specific patterns of event interleaving, and the state machine below
			// takes advantage of this. I've used debug asserts to enumerate known expected
			// pattern interleavings -- these are not necessarily the *only* valid interleavings,
			// but an assert failure may be indicative of dropped/reordered events or some other
			// thread join sequence that I haven't considered.
			switch (joinEvent.Type)
			{
				case GcJoinType.Join:
					if (joinEvent.Time == GcJoinTime.Start)
					{
						switch (previousState)
						{
							// Standard join pattern
							case ServerGcThreadState.Ready:
							// Observed when thread was last to join (does not fire join end)
							case ServerGcThreadState.WaitingInRestart:
							case ServerGcThreadState.Unknown:
								break;

							case ServerGcThreadState.SingleThreaded:
							case ServerGcThreadState.WaitingInJoin: //TODO: anhans saw these happen
								break;
							default:
								throw new Exception($"Unexpected previous state of Join: {previousState}");
						}
						return ServerGcThreadState.WaitingInJoin;
					}
					else  // GcJoinTime.End
						return ServerGcThreadState.Ready;
				case GcJoinType.Restart:
					// TODO: This fails to correctly handle the transition case from
					// WaitingInRestart to Ready in the case where the thread is the last thread
					// to join. The state machine breaks down here because it only looks at the
					// previous state to determine the next state -- for this case, we would need
					// to look two states behind. This will require updating the state machine,
					// but unfortunately this was discovered during the
					// 2nd-to-last week of t-aase's internship, and there may not be enough time
					// to fix this properly. For now, users of this function should just be aware
					// of this case and handle it in their code accordingly.
					if (joinEvent.Time == GcJoinTime.Start)
					{
						switch (previousState)
						{
							// Standard join pattern
							case ServerGcThreadState.WaitingInJoin:
							// Observed when thread was first to join (see r_join in gc.cpp)
							case ServerGcThreadState.SingleThreaded:
							case ServerGcThreadState.Unknown:
								break;
							case ServerGcThreadState.Ready:
							case ServerGcThreadState.WaitingInRestart:
								break; //TODO: anhans saw these happen
							default:
								throw new Exception($"Unexpected previous state of Restart: {previousState}");
						}
						return ServerGcThreadState.WaitingInRestart;
					}
					else
						return previousState; //TODO: previously this happened just by falling out of the switch and failing to assign 'newState'.
				case GcJoinType.LastJoin:
					switch (previousState)
					{
						// Standard join pattern
						case ServerGcThreadState.Ready:
						// Observed when thread is last to join for consecutive joins
						case ServerGcThreadState.WaitingInRestart:
						case ServerGcThreadState.Unknown:
							break;
						case ServerGcThreadState.SingleThreaded:
						case ServerGcThreadState.WaitingInJoin: break; //TODO: anhans saw these
						default:
							throw new Exception($"Unexpected previous state of LastJoin: {previousState}");
					}
					return ServerGcThreadState.SingleThreaded;
				case GcJoinType.FirstJoin:
					switch (previousState)
					{
						case ServerGcThreadState.Ready:
						case ServerGcThreadState.Unknown:
							break;
						default:
							throw new Exception($"Unexpected previous state of FirstJoin: {previousState}");
					}
					return ServerGcThreadState.SingleThreaded;
				default:
					throw new Exception($"Unexpected GcJoinType {joinEvent.Type}");
			}
		}

        private static ServerGcThreadState UpdateCurrentGcThreadState(
            ServerGcHistory heap,
            GcJoin joinEvent,
            ServerGcThreadState previousState
            )
        {

			// For a single GC, we expect to receive a list of ServerGcHeapHistories for each heap.
			// Each ServerGcHeapHistory is only supposed to store the GC Join events fired by that
			// particular heap, with the exception of global restart events fired by the last thread
			// to join, which may originate from any heap.
			if (joinEvent.Heap != heap.HeapId)
			{
				if (joinEvent.Type != GcJoinType.Restart)
				{
					// NOTE: joinEvent.Heap is 'data.ProcessorNumber', which may be different from the heap id???
					// fails with 'JoinEvent.Heap: 5, heap.HeapId: 0, joinEvent.Type: Join'
					//throw new Exception($"joinEvent.Heap: {joinEvent.Heap}, heap.HeapId: {heap.HeapId}, joinEvent.Type: {joinEvent.Type}, previousState: {previousState}");
				}
			}

			//System.Diagnostics.Debug.Assert(joinEvent.Heap == heap.HeapId ||
			//        joinEvent.Type == GcJoinType.Restart);

			ServerGcThreadState newState = GetNewGcThreadState(previousState, joinEvent);
#if DEBUG
			//Console.WriteLine("Transitioned from {0} to {1} upon {2} {3}", previousState, newState,
            //    joinEvent.Type, joinEvent.Time);
#endif
            return newState;
        }

        public static List<Tuple<ServerGcThreadState, double>> GetGcThreadStateTransitionTimes(
            ServerGcHistory heap)
        {
            List<Tuple<ServerGcThreadState, double>> gcThreadStateTransitionTimestamps = new
                List<Tuple<ServerGcThreadState, double>>();
            ServerGcThreadState currJoinState = ServerGcThreadState.Unknown;
            ServerGcThreadState lastJoinState = currJoinState;
            gcThreadStateTransitionTimestamps.Add(new Tuple<ServerGcThreadState, double>(
                currJoinState, 0));
            for (int i = 0; i < heap.GcJoins.Count; i++)
            {
                var secondToLastJoinState = lastJoinState;
                lastJoinState = currJoinState;
                // See the TODO comment under the Restart join type case within the function
                // UpdateCurrentGcThreadState() to understand why I'm doing this.
                if (secondToLastJoinState == ServerGcThreadState.SingleThreaded &&
                    lastJoinState == ServerGcThreadState.WaitingInRestart)
                {
                    currJoinState = ServerGcThreadState.Ready;
                }
                else
                {
                    currJoinState = UpdateCurrentGcThreadState(heap, heap.GcJoins[i],
                                currJoinState);
                }
                if (currJoinState != lastJoinState)
                {
                    gcThreadStateTransitionTimestamps.Add(new Tuple<ServerGcThreadState, double>(
                        currJoinState, heap.GcJoins[i].RelativeTimestampMsc));
                }
            }

            return gcThreadStateTransitionTimestamps;
        }

        private static ServerGcThreadState FindFirstKnownGcThreadStateAfterTime(
            ServerGcHistory heap,
            double timestamp,
            ServerGcThreadState currentJoinState,
            ref int currentJoinEventIndex
            )
        {
            while (currentJoinEventIndex < heap.GcJoins.Count &&
                heap.GcJoins[currentJoinEventIndex].AbsoluteTimestampMsc < timestamp)
            {
                currentJoinState = UpdateCurrentGcThreadState(heap,
                            heap.GcJoins[currentJoinEventIndex], currentJoinState);
                currentJoinEventIndex++;
            }
            return currentJoinState;
        }

        // Many different types of questions a user might want to ask will involve identifying
        // points in time where CPU time was stolen from a GC thread. However, the questions we ask
        // may require different ways of processing stolen CPU time, so we set up this callback
        // which gives a stolen CPU processing callback as much information as we have and allows
        // it to do some arbitrary processing with that stolen CPU information.
        public delegate void ProcessStolenCpu(
            ThreadWorkSpan span,
            double currentStateDuration,
            ServerGcThreadState currentState,
            TraceGC gcInstance,
            ServerGcHistory heap
            );

        // In addition to registering callbacks for stolen CPU, we expose a callback for all
        // GC join state spans.
        public delegate void ProcessGcJoinActivity(
            double currentStateDuration,
            ServerGcThreadState currentState,
            TraceGC gcInstance,
            ServerGcHistory heap
            );

        public static void AddStolenCpuForThreadState(
            Dictionary<ServerGcThreadState, double> cpuPerState,
            double currentStateDuration,
            ServerGcThreadState currentState
            )
        {
            cpuPerState[currentState] += currentStateDuration;
        }

        public static void AddToListOfStolenCpuInstances(
            Dictionary<ServerGcThreadState, List<ThreadWorkSpan>> stolenCpuInstances,
            ServerGcThreadState currentState,
            ThreadWorkSpan span
            )
        {
            stolenCpuInstances[currentState].Add(span);
        }

        static object TryThis()
        {
            Type etlTrace = Assembly.GetAssembly(typeof(TraceGC)).GetType("ETLTrace");
            // won't work, it's not static. How to get ETLTrace instance?
            return etlTrace.InvokeMember("ComputeStacksRaw", BindingFlags.Public | BindingFlags.Static, binder: null, target: null, args: new object[0]);
            /*
             * 
            foreach (dynamic tree_node in (System.Collections.IEnumerable)stackResult.rollupStats)
            {
                string name = stackResult.atomsNodeNames.MakeString(tree_node.id);
                Console.WriteLine(name, tree_node.Inclusive);
            }*/
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gcInstance"></param>
        /// <param name="heap"></param>
        /// <param name="eventList"></param>
        /// <returns></returns>
        public static Dictionary<ServerGcThreadState, List<ThreadWorkSpan>>
            GetLostCpuInstancesForHeap(
                TraceGC gcInstance,
                ServerGcHistory heap,
                Microsoft.Diagnostics.Tracing.Etlx.TraceEvents eventList,
                bool markIdleStolen=false
            )
        {
            Dictionary<ServerGcThreadState, List<ThreadWorkSpan>> lostCpuInstances =
                new Dictionary<ServerGcThreadState, List<ThreadWorkSpan>>();
            foreach (ServerGcThreadState state in Enum.GetValues(typeof(ServerGcThreadState)))
            {
                lostCpuInstances[state] = new List<ThreadWorkSpan>();
            }

            ProcessStolenCpuInstances(gcInstance, heap, (ThreadWorkSpan span,
                double currentStateDuration, ServerGcThreadState currentState, TraceGC currGc,
                ServerGcHistory currHeap) =>
            {
                AddToListOfStolenCpuInstances(lostCpuInstances, currentState, span);
            }, markIdleStolen:markIdleStolen);

            return lostCpuInstances;
        }

        /// <summary>
        /// Compute total amount of CPU time stolen from a GC thread for a given heap, aggregated
        /// by GC thread join state
        /// </summary>
        /// <param name="gcInstance">The exact GC to compute stolen CPU time for</param>
        /// <param name="heap">The thread join history for the target GC thread to analyze during this GC</param>
        /// <returns>Dictionary of amount of CPU time stolen from GC thread in milliseconds for each
        /// GC thread join state</returns>
        public static Dictionary<ServerGcThreadState, Tuple<double, double>> GetLostCpuBreakdownForHeap(
            TraceGC gcInstance,
            ServerGcHistory heap,
            bool markIdleStolen=false
            )
        {
            Dictionary<ServerGcThreadState, double> stolenCpuPerState =
                new Dictionary<ServerGcThreadState, double>();
            Dictionary<ServerGcThreadState, double> totalCpuPerState =
                new Dictionary<ServerGcThreadState, double>();
            Dictionary<ServerGcThreadState, Tuple<double, double>> stolenCpuAsAbsoluteAndPercentage =
                new Dictionary<ServerGcThreadState, Tuple<double, double>>();
            foreach (ServerGcThreadState state in Enum.GetValues(typeof(ServerGcThreadState)))
            {
                stolenCpuPerState[state] = totalCpuPerState[state] = 0.0;
            }

            ProcessStolenCpuInstances(gcInstance, heap,
                (
                    ThreadWorkSpan span,
                    double stolenDuration,
                    ServerGcThreadState currentState,
                    TraceGC currGc,
                    ServerGcHistory currHeap
                ) =>
                {
                    stolenCpuPerState[currentState] += stolenDuration;
                },
                (
                    double stolenDuration,
                    ServerGcThreadState currentState,
                    TraceGC currentGc,
                    ServerGcHistory currentHeap
                ) =>
                {
                    totalCpuPerState[currentState] += stolenDuration;
                }, markIdleStolen);

            foreach (ServerGcThreadState state in Enum.GetValues(typeof(ServerGcThreadState)))
            {
                stolenCpuAsAbsoluteAndPercentage[state] = new Tuple<double, double>(
                    stolenCpuPerState[state],
                    totalCpuPerState[state]
                );
            }

            return stolenCpuAsAbsoluteAndPercentage;
        }

        private static TraceTimeRange switchSpanToTraceTimeRange(ThreadWorkSpan span)
        {
            return new TraceTimeRange(span.AbsoluteTimestampMsc,
                    span.AbsoluteTimestampMsc + span.DurationMsc);
        }

        private static bool TimeRangeOverlap(TraceTimeRange firstRange, TraceTimeRange secondRange)
        {
            bool firstStartsWithinSecond = firstRange.BeginMsec >= secondRange.BeginMsec &&
                    firstRange.BeginMsec <= secondRange.EndMsec;
            bool firstExtendsIntoSecond = firstRange.BeginMsec < secondRange.BeginMsec &&
                    firstRange.EndMsec >= secondRange.BeginMsec;
            return firstStartsWithinSecond || firstExtendsIntoSecond;
        }

        private static bool SpanBeforeThreadStatePeriod(ThreadWorkSpan span,
                TraceTimeRange gcThreadStatePeriod)
        {
            TraceTimeRange spanTimeRange = switchSpanToTraceTimeRange(span);

            return !TimeRangeOverlap(gcThreadStatePeriod, spanTimeRange) &&
                    spanTimeRange.BeginMsec < gcThreadStatePeriod.EndMsec;
        }

        private static void ProcessStolenCpuInstances(
            TraceGC gcInstance,
            ServerGcHistory heap,
            ProcessStolenCpu? stolenCpuHandler=null,
            ProcessGcJoinActivity? gcJoinHandler=null,
            bool markIdleStolen=false
            )
        {
            List<Tuple<ServerGcThreadState, double>> transitionTimes =
                GetGcThreadStateTransitionTimes(heap);

            /* Identifying moments where other processes are stealing CPU time from heap's GC thread
               requires the following data to be available:

               1) Knowing what the thread ID for heap's GC thread is (Some TraceGCs don't have this
                  for some reason)
               2) Having CSwitch data, i.e. knowing what was running on the CPU at certain times
               3) Having GC join data, i.e. knowing what state the GC thread was in at certain times

               If we don't have any one of these, we may as well stop because we will either end
               up failing to identify any instances of stolen CPU time or return incorrect data.
            */
            if (heap.GcWorkingThreadId < 1 || heap.SwitchSpans.Count < 1 || transitionTimes.Count < 1)
            {
                return;
            }

            //Console.WriteLine("Doing analysis for GC {0} Heap {1}", gcInstance.Number, heap.HeapId);


            double gcReadyTime, lastGcSpanEndTime;
            gcReadyTime = lastGcSpanEndTime = gcInstance.PauseStartRelativeMSec;

            int currentStateTransitionIndex = 0;
            int currentSpanIndex = 0;

            /* We want to identify moments of time where the CPU is being stolen from the target
               heap's GC thread. This can be tricky because while the GC thread is in a specific
               state, multiple different processes or threads might execute on the CPU. Conversely,
               while a single process is on the CPU, the GC thread might transition to different
               states. As a result, we want to look at a period of time where the GC thread is
               in a specific state, and find *all* CPU spans of work that interfere with the GC
               thread while it is in that state. Furthermore, we only want to measure the offending
               process' contribution to stolen CPU time during that period, since a process' time
               on the CPU might not have a clean overlap with the current GC thread state period.
            */
            while (currentStateTransitionIndex < transitionTimes.Count &&
                    currentSpanIndex < heap.SwitchSpans.Count)
            {
                // Pick a GC thread state period to analyze and get its start/end times
                Tuple<ServerGcThreadState, double> currentGcTransitionPeriod =
                    transitionTimes[currentStateTransitionIndex];
                ServerGcThreadState currentGcPeriodThreadState = currentGcTransitionPeriod.Item1;
                double currentGcPeriodStartTime = currentGcTransitionPeriod.Item2 +
                        gcInstance.PauseStartRelativeMSec;
                double currentGcPeriodEndTime;
                if (currentStateTransitionIndex < transitionTimes.Count - 1)
                {
                    currentGcPeriodEndTime = transitionTimes[currentStateTransitionIndex + 1].Item2 +
                            gcInstance.PauseStartRelativeMSec;
                } else
                {
                    currentGcPeriodEndTime = gcInstance.PauseStartRelativeMSec +
                            gcInstance.PauseDurationMSec;
                }

                // Start by skipping over CPU work spans that start/end entirely before this current
                // GC thread state period even began
                TraceTimeRange gcThreadStateTimeRange = new TraceTimeRange(currentGcPeriodStartTime,
                        currentGcPeriodEndTime);

                while (currentSpanIndex < heap.SwitchSpans.Count &&
                       SpanBeforeThreadStatePeriod(heap.SwitchSpans[currentSpanIndex],
                       gcThreadStateTimeRange))
                {
                    ThreadWorkSpan span = heap.SwitchSpans[currentSpanIndex];
                    TraceTimeRange range = switchSpanToTraceTimeRange(span);

                    currentSpanIndex++;
                }

                /* At this point, we know that we are in one of two cases:
                    1) We have found the first CPU span that actually occurred during this
                       GC thread state period or;
                    2) There never was a CPU span that occurred during this GC thread state period
                       and the next CPU span we ran into started after this period
                    In the event we've hit case 1), we mark each CPU span where the GC thread was
                    not running as stealing time from the GC thread, and the amount of time that
                    CPU span stole, but only with respect to the current GC thread state.
                */
                while (currentSpanIndex < heap.SwitchSpans.Count &&
                       TimeRangeOverlap(gcThreadStateTimeRange,
                       switchSpanToTraceTimeRange(heap.SwitchSpans[currentSpanIndex])))
                {
                    ThreadWorkSpan currentCpuSpan = heap.SwitchSpans[currentSpanIndex];
                    TraceTimeRange cpuSpanTimeRange = switchSpanToTraceTimeRange(currentCpuSpan);

                    // Since this CPU span overlaps with this GC thread state, we check if it
                    // is a non-GC thread running, and if so, mark its contribution to stolen CPU
                    // time.
                    if (currentCpuSpan.ProcessId != heap.ProcessId &&
                        currentCpuSpan.ThreadId != heap.GcWorkingThreadId &&
                        (currentCpuSpan.ProcessId != 0 || markIdleStolen))
                    {
                        double stolenTimeStart = Math.Max(cpuSpanTimeRange.BeginMsec,
                                currentGcPeriodStartTime);
                        double stolenTimeEnd = Math.Min(cpuSpanTimeRange.EndMsec,
                                currentGcPeriodEndTime);
                        stolenCpuHandler?.Invoke(currentCpuSpan, stolenTimeEnd - stolenTimeStart,
                            currentGcPeriodThreadState, gcInstance, heap);
                    }

                    currentSpanIndex++;
                }

                // In the event that a CPU span extends into the next GC thread state period,
                // we want to make sure that it is accounted for in both periods.
                // Incrementing in this case would cause us to fail to account for any stolen
                // time the span occupies in the beginning of the next state.
                if (currentSpanIndex > 0 && currentSpanIndex < heap.SwitchSpans.Count &&
                    switchSpanToTraceTimeRange(heap.SwitchSpans[currentSpanIndex]).EndMsec >= currentGcPeriodEndTime)                    
                {
                    currentSpanIndex--;
                }
                gcJoinHandler?.Invoke(currentGcPeriodEndTime - currentGcPeriodStartTime,
                    currentGcPeriodThreadState, gcInstance, heap);
                currentStateTransitionIndex++;
            }
        }

        // Helper function for removing illegal characters from filenames. This is here for convenience
        // since it's often desirable to include a function name in the name of a file, but function
        // names can contain characters that aren't allowed in filenames.
        public static string RemoveIllegalFilenameChars(string filename)
        {
            string cleanFilename = filename;

            int idx = cleanFilename.IndexOfAny(Path.GetInvalidFileNameChars());
            while (idx != -1)
            {
                cleanFilename = cleanFilename.Remove(idx, 1);
                idx = cleanFilename.IndexOfAny(Path.GetInvalidFileNameChars());
            }

            return cleanFilename;
        }

        private static bool IsTimeWithinTimeRanges(double timeMsec, List<TraceTimeRange> sortedTimeRanges)
        {
            // TODO: Make this faster.
            foreach (var timeRange in sortedTimeRanges)
            {
                // For now just stop looking at ranges when we get to one whose start time is later
                // than what we are looking for. Time ranges are sorted by begin time.
                if (timeRange.BeginMsec > timeMsec)
                {
                    return false;
                }

                if (timeMsec <= timeRange.EndMsec)
                {
                    return true;
                }
            }

            return false;
        }

        public static StackSource LoadTraceAndGetStacks2(
            EtlxNS.TraceLog traceLog,
            SymbolReader symReader,
            string processName)
        {
            foreach (var module in traceLog.ModuleFiles)
            {
                if (module.Name.ToLower().Contains("clr"))
                {
                    // Only resolve symbols for modules whose name includes "clr".
                    traceLog.CodeAddresses.LookupSymbolsForModule(symReader, module);
                }
            }

            EtlxNS.TraceProcess processToAnalyze = traceLog.Processes.FirstProcessWithName(processName);
            return traceLog.CPUStacks(processToAnalyze);
        }

        public static StackView GetStackViewForInfra(
            string tracePath,
            string symPath,
            string processName)
        {
            EtlxNS.TraceLog traceLog = EtlxNS.TraceLog.OpenOrConvert(tracePath);
            TextWriter symlogWriter  = File.CreateText("C:\\Git\\disposablelog.txt");
            SymbolReader symReader   = new SymbolReader(symlogWriter, symPath);
            StackSource stackSource  =  LoadTraceAndGetStacks2(traceLog, symReader, processName);
            return new StackView(traceLog, stackSource, symReader);
        }

        /* ************************************************************ */
        /*                         Drafts to Learn                      */
        /* ************************************************************ */

        public static void CPUSamplesAnalysis(string tracePath, string symPath)
        {
            // int numCPUStacks         = 0;
            EtlxNS.TraceLog traceLog = EtlxNS.TraceLog.OpenOrConvert(tracePath);
            TextWriter symlogWriter  = File.CreateText("C:\\Git\\disposablelog.txt");
            SymbolReader symReader   = new SymbolReader(symlogWriter, symPath);

            // foreach (var module in traceLog.ModuleFiles)
            // {
            //     if (module.Name.ToLower().Contains("clr"))
            //     {
            //         Console.WriteLine(module.Name);
            //     }
            // }

            // (stackSource, numCPUStacks) = LoadTraceAndGetStacks(traceLog, symReader);
            // Console.WriteLine(numCPUStacks);

            StackSource stackSource = LoadTraceAndGetStacks2(traceLog, symReader, "Corerun");
            StackView stackView = new StackView(traceLog, stackSource, symReader);
            CallTreeNodeBase node = stackView.FindNodeByName("gc_heap::plan_phase");
            // Console.WriteLine(node.ToString());

            Console.WriteLine($"Name: {node.Name}");
            Console.WriteLine($"Inclusive Metric %: {node.InclusiveMetricPercent}");
            Console.WriteLine($"Exclusive Metric %: {node.ExclusiveMetricPercent}");
            Console.WriteLine($"Inclusive Count: {node.InclusiveCount}");
            Console.WriteLine($"Exclusive Count: {node.ExclusiveCount}");
            Console.WriteLine($"Exclusive Folded: {node.ExclusiveFoldedCount}");
            Console.WriteLine($"First Time Relative MSec: {node.FirstTimeRelativeMSec}");
            Console.WriteLine($"Last Time Relative MSec: {node.LastTimeRelativeMSec}");
            Console.WriteLine($"\nHistogram:\n{node.InclusiveMetricByScenarioString}");

            // CallTreeNode node = stackView.GetCallees("gc_heap::plan_phase");
            // while (node.HasChildren)
            // {
            //     Console.WriteLine($"{node.ToString()}");
            //     node = (CallTreeNode) node.Callees[0];
            // }

            return ;
        }

        /* ************************************************************ */
        /*                         Unused Code                          */
        /* ************************************************************ */

        // static string GetFrameNameAtIndex(TraceEventStackSource stackSource, StackSourceSample sample)
        // {
        //     StackSourceCallStackIndex currIdx = sample.StackIndex;
        //     return stackSource.GetFrameName(stackSource.GetFrameIndex(currIdx), false);
        // }

        // /// <summary>
        // /// Filter a set of stacks and return a StackSource containing the filtered set.
        // /// </summary>
        // /// <param name="traceLog">The tracelog the stacks came from</param>
        // /// <param name="stackSource">The input set of stacks to filter</param>
        // /// <param name="timeRanges">The time ranges to filter to, sorted by begin time</param>
        // /// <param name="functionName">The functions to filter to (stacks that don't contain this function will be filtered out)</param>
        // /// <param name="functionMetrics">[out] The metrics for each function specified</param>
        // /// <returns></returns>
        // public static StackSource FilterStacks(
        //     Microsoft.Diagnostics.Tracing.Etlx.TraceLog traceLog,
        //     TraceEventStackSource stackSource,
        //     List<TraceTimeRange>? timeRanges,
        //     string functionName,
        //     out SampleMetrics functionMetrics
        //     )
        // {
        //     int localIncCount = 0;
        //     int localExcCount = 0;

        //     bool filterByFunction = !string.IsNullOrWhiteSpace(functionName);

        //     var filteredStackSource = new MutableTraceEventStackSource(traceLog);
        //     stackSource.ForEach(sample =>
        //     {
        //         bool includeSample = false;

        //         if (timeRanges != null && !IsTimeWithinTimeRanges(sample.TimeRelativeMSec, timeRanges))
        //             return;

        //         if (filterByFunction)
        //         {
        //             StackSampleContainsFunctionKind? f = DoesStackSampleContainFunction(stackSource, sample, functionName);
        //             if (f != null)
        //             {
        //                 localIncCount++;
        //                 if (f == StackSampleContainsFunctionKind.exclusive) localExcCount++;
        //                 includeSample = true;
        //             }
        //         }
        //         else
        //         {
        //             includeSample = true;
        //         }

        //         // DO THE ADDRESS METHOD MENTIONED ABOVE
        //         //var frameIdx = stackSource.GetFrameIndex(sample.StackIndex);
        //         //if (frameIdx != StackSourceFrameIndex.Invalid)
        //         //{
        //         //    var addrIdx = stackSource.GetFrameCodeAddress(frameIdx);
        //         //    if (addrIdx != Microsoft.Diagnostics.Tracing.Etlx.CodeAddressIndex.Invalid)
        //         //    {
        //         //        var addr = traceLog.CodeAddresses.Address(addrIdx);
        //         //    }
        //         //}

        //         if (includeSample)
        //             filteredStackSource.AddSample(sample);
        //     });

        //     functionMetrics.ExclusiveSamples = localExcCount;
        //     functionMetrics.InclusiveSamples = localIncCount;
        //     functionMetrics.Identifier = functionName;

        //     return filteredStackSource;
        // }

        // public enum StackSampleContainsFunctionKind
        // {
        //     inclusive,
        //     exclusive
        // }


        // public static StackSampleContainsFunctionKind? DoesStackSampleContainFunction(TraceEventStackSource stackSource, StackSourceSample sample, string functionName)
        // {
        //     StackSourceCallStackIndex currIdx = sample.StackIndex;

        //     // Check each frame in the stack for the desired function
        //     bool isLeafFrame = true;
        //     while (currIdx != StackSourceCallStackIndex.Invalid)
        //     {
        //         // TODO: This is a poor way of filtering stacks containing a function: we're using string comparison 
        //         // after symbol lookup. A better way is to find the VA range for the function with DIA and then 
        //         // filtering samples by address
        //         var frameName = stackSource.GetFrameName(stackSource.GetFrameIndex(currIdx), false);
        //         // Unfortunately it can't seem to find the names of functions ... just the module name

        //         if (frameName.Contains(functionName))
        //             return isLeafFrame ? StackSampleContainsFunctionKind.exclusive : StackSampleContainsFunctionKind.inclusive;

        //         isLeafFrame = false;
        //         currIdx = stackSource.GetCallerIndex(currIdx);
        //     }

        //     return null;
        // }

        // static readonly string[] modulesToLoad = new string[] { "clr", "kernel32", "ntdll", "ntoskrnl", "mscorlib" };
        // const string symbolPath = "SRV*C:\\symbols*http://symweb.corp.microsoft.com;SRV*C:\\symbols*http://msdl.microsoft.com/download/symbols;SRV*C:\\symbols*https://dotnet.myget.org/F/dotnet-core/symbols";
        // public static void SIMPLIFIED(Microsoft.Diagnostics.Tracing.Etlx.TraceLog traceLog)
        // {
        //     var sym_log_writer = File.CreateText("C:\\tmp.txt");
        //     var symbolReader = new Microsoft.Diagnostics.Symbols.SymbolReader(sym_log_writer, symbolPath);

        //     foreach (Microsoft.Diagnostics.Tracing.Etlx.TraceModuleFile module in traceLog.ModuleFiles)
        //     {
        //         if (modulesToLoad.Any(m => module.Name.ToLower().Contains(m)))
        //         {
        //             traceLog.CodeAddresses.LookupSymbolsForModule(symbolReader, module);
        //             Console.WriteLine($"Loaded symbols for module {module}");
        //         }
        //     }
        //     
        //     StackSource stacks = traceLog.CPUStacks(null);
        //     int n = 0;
        //     stacks.ForEach(sample =>
        //     {
        //         if (n++ > 100) throw new Exception("bai"); // Prevent excessive output
        //         StackSourceCallStackIndex callStackIndex = sample.StackIndex;
        //         StackSourceFrameIndex frameIndex = stacks.GetFrameIndex(callStackIndex);
        //         string frameName = stacks.GetFrameName(frameIndex, verboseName: true);
        //         Console.WriteLine(frameName);
        //     });
        // }

        // // Load a trace and get all the CPU stacks from it.
        // public static (TraceEventStackSource, int) LoadTraceAndGetStacks(Microsoft.Diagnostics.Tracing.Etlx.TraceLog traceLog, Microsoft.Diagnostics.Symbols.SymbolReader symReader)
        // {
        //     foreach (var module in traceLog.ModuleFiles)
        //     {
        //         // TODO: Make this configurable?
        //         if (module.Name.ToLower().Contains("clr"))
        //         {
        //             // Only resolve symbols for modules whose name includes "clr".
        //             traceLog.CodeAddresses.LookupSymbolsForModule(symReader, module);
        //         }
        //     }

        //     // Put all the CPU stacks into our own mutable StackSource.
        //     var stackSource = new MutableTraceEventStackSource(traceLog);
        //     int stackCountLocal = 0;
        //     traceLog.CPUStacks(null).ForEach(
        //         sample =>
        //         {
        //             stackCountLocal++;
        //             stackSource.AddSample(sample);
        //         });

        //     return (stackSource, stackCountLocal);
        // }

        // // TODO: Rename this?
        // /// <summary>
        // /// Process a set of traces for information about a set of functions over a set of time ranges.
        // /// </summary>
        // /// <param name="traces">The ETL traces to process</param>
        // /// <param name="functionsOfInterest">The functions for which to get information</param>
        // /// <param name="timeRangesOfInterest">The time ranges (specified in milliseconds relative to trace start) of interest</param>
        // /// <param name="symlogWriter">The TextWriter to use for logging symbol resolution information</param>
        // public static void ProcessTracesForFunction(EtlTrace trace, string functionOfInterest) //, List<TraceTimeRange> timeRangesOfInterest, TextWriter symlogWriter)
        // {
        //     var traceLog = Microsoft.Diagnostics.Tracing.Etlx.TraceLog.OpenOrConvert(trace.FilePath);

        //     if (trace.AllCpuStacks == null)
        //     {
        //         TextWriter symlogWriter = File.CreateText("C:\\killme.txt");
        //         var symReader = new Microsoft.Diagnostics.Symbols.SymbolReader(symlogWriter, trace.SymPath);
        //         (trace.AllCpuStacks, trace.TotalCpuSampleCount) = LoadTraceAndGetStacks(traceLog, symReader);
        //     }

        //     SampleMetrics counts;
        //     StackSource filteredStacks = FilterStacks(traceLog, Util.NonNull(trace.AllCpuStacks), /*timeRangesOfInterest*/
        //     null, functionOfInterest, out counts);

        //     trace.FunctionNameToStacks[functionOfInterest] = filteredStacks;
        //     trace.FunctionNameToMetrics[functionOfInterest] = counts;
        // }
    }
}