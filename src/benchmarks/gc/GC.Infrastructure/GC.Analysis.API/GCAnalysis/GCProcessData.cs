using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;

namespace GC.Analysis.API
{
    public sealed class GCProcessData
    {
        private static readonly Dictionary<string, Func<GCProcessData, double>> _customAggregateCalculationMap = new Dictionary<string, Func<GCProcessData, double>>(StringComparer.OrdinalIgnoreCase)
        {
            {  "gc count", (gc) => gc.Stats.Count },
            {  "non induced gc count", (gc) => gc.Stats.Count - gc.GCs.Count(g => g.Reason == GCReason.Induced)},
            {  "induced gc count", (gc) => gc.GCs.Count(g => g.Reason == GCReason.Induced)},
            {  "total allocated (mb)", (gc) => gc.Stats.TotalAllocatedMB },
            {  "max size peak (mb)", (gc) => gc.Stats.MaxSizePeakMB },
            {  "total pause time (msec)", (gc) => gc.Stats.TotalPauseTimeMSec },
            {  "gc pause time %", (gc) => gc.Stats.GetGCPauseTimePercentage() },
            {  "avg. heap size (mb)", (gc) => gc.GCs.Average(g => g.HeapSizeBeforeMB) },
            {  "avg. heap size after (mb)", (gc) => gc.GCs.Average(g => g.HeapSizeAfterMB) },
        };

        private readonly Lazy<JoinAnalysis> _joinAnalysis;

        public GCProcessData(TraceProcess process, TraceLoadedDotNetRuntime managedProcess, Dictionary<int, int> gcThreadsToHeapNumber, Analyzer parent, double durationMSec)
        {
            ProcessName = process.Name;
            ProcessID = process.ProcessID;
            CommandLine = process.CommandLine;
            GCs = managedProcess.GC.GCs;
            DurationMSec = durationMSec;
            Stats = managedProcess.GC.Stats();
            Generations = managedProcess.GC.Generations();
            Gen2Blocking = GCs.Where(gc => gc.Generation == 2 && gc.Type != GCType.BackgroundGC);
            BGCs = GCs.Where(gc => gc.Generation == 2 && gc.Type == GCType.BackgroundGC);
            Parent = parent;

            // Check if the process was running as SRV. 
            if (Stats.IsServerGCUsed == 1)
            {
                GCThreadIDsToHeapNumbers = gcThreadsToHeapNumber;
            }

            // If somehow the GlobalHeapHistory event wasn't fired, check the number of heaps.
            // For any cases with > 1 heaps, we know it's not WKS. 
            // For all other cases, ignore storing these.
            else
            {
                if (Stats.HeapCount > 1)
                {
                    GCThreadIDsToHeapNumbers = gcThreadsToHeapNumber;
                }

                else
                {
                    GCThreadIDsToHeapNumbers = new Dictionary<int, int>();
                }
            }

            _joinAnalysis = new Lazy<JoinAnalysis>(() => new JoinAnalysis(this));
        }

        public string ProcessName { get; }
        public string CommandLine { get; }
        public int ProcessID { get; }
        public List<TraceGC> GCs { get; }
        public GCStats Stats { get; }
        public GCStats[] Generations { get; }
        public List<TraceGC> Gen2Blocking { get; }
        public List<TraceGC> BGCs { get; }
        public Dictionary<int, int> GCThreadIDsToHeapNumbers { get; }
        public double DurationMSec { get; }
        public Analyzer Parent { get; }

        public static double? LookupAggregateCalculation(string calculation, GCProcessData processData)
        {
            if (processData == null)
            {
                return null;
            }

            if (!_customAggregateCalculationMap.TryGetValue(calculation, out var val))
            {
                return null;
            }

            else
            {
                return val.Invoke(processData);
            }
        }

        public JoinAnalysis GetJoinAnalysis() => _joinAnalysis.Value;
    }
}
