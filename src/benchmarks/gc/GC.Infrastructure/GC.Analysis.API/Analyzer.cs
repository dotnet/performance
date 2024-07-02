using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Etlx;
using Etlx = Microsoft.Diagnostics.Tracing.Etlx;

namespace GC.Analysis.API
{
    public sealed class Analyzer : IDisposable
    {
        public Analyzer(string tracePath, HashSet<string> processNames)
        {
            TraceLogPath = tracePath;
            if (Path.GetExtension(tracePath) == ".zip")
            {
                var zippedReader = new ZippedETLReader(tracePath);
                zippedReader.UnpackArchive();
                tracePath = zippedReader.EtlFileName;
            }

            TraceLog = Etlx.TraceLog.OpenOrConvert(tracePath);
            Dictionary<int, Dictionary<int, int>> processIdToGCThreads = GetAllGCThreads(TraceLog.Events.GetSource(), processNames);

            foreach (var p in TraceLog.GetAllProcesses())
            {
                if (!processNames.Contains(p.Name))
                {
                    continue;
                }

                var managedProcess = p.LoadedDotNetRuntime();
                if (!IsInterestingGCProcess(managedProcess))
                {
                    continue;
                }

                if (!AllGCProcessData.TryGetValue(p.Name, out var values))
                {
                    AllGCProcessData[p.Name] = values = new();
                }

                values.Add(new GCProcessData(p, managedProcess, processIdToGCThreads[p.ProcessID], this, p.EndTimeRelativeMsec - p.StartTimeRelativeMsec));
            }
        }

        public Analyzer(string tracePath)
        {
            TraceLogPath = tracePath;

            if (tracePath.EndsWith(".nettrace"))
            {
                string pathToNettraceEtlx = Etlx.TraceLog.CreateFromEventTraceLogFile(tracePath);
                var tracelog = Etlx.TraceLog.OpenOrConvert(pathToNettraceEtlx);
                var trace = tracelog.Events.GetSource();
                trace.NeedLoadedDotNetRuntimes();
                trace.Process();

                // Nettrace only has 1 process.
                var process = trace.Processes().First();
                var managed = process.LoadedDotNetRuntime();

                if (!AllGCProcessData.TryGetValue(process.Name, out var gcprocesses))
                {
                    AllGCProcessData[process.Name] = gcprocesses = new();
                }

                // TODO: Clean up the Linux traces.
                gcprocesses.Add(new GCProcessData(process, managed, new(), this, process.EndTimeRelativeMsec - process.StartTimeRelativeMsec));
            }

            else
            {
                if (Path.GetExtension(tracePath) == ".zip")
                {
                    var zippedReader = new ZippedETLReader(tracePath);
                    zippedReader.UnpackArchive();
                    tracePath = zippedReader.EtlFileName;
                }

                TraceLog = Etlx.TraceLog.OpenOrConvert(tracePath);
                Dictionary<int, Dictionary<int, int>> processIdToGCThreads = GetAllGCThreads(TraceLog.Events.GetSource());

                foreach (var p in TraceLog.GetAllProcesses())
                {
                    TraceLoadedDotNetRuntime managedProcess = p.LoadedDotNetRuntime();
                    if (!IsInterestingGCProcess(managedProcess))
                    {
                        continue;
                    }

                    if (!AllGCProcessData.TryGetValue(p.Name, out var values))
                    {
                        AllGCProcessData[p.Name] = values = new();
                    }

                    if (!processIdToGCThreads.ContainsKey(p.ProcessID))
                    {
                        continue;
                    }

                    values.Add(new GCProcessData(p, managedProcess, processIdToGCThreads[p.ProcessID], this, p.EndTimeRelativeMsec - p.StartTimeRelativeMsec));
                }
            }
        }

        internal static Dictionary<int, Dictionary<int, int>> GetAllGCThreads(TraceLogEventSource eventSource, HashSet<string>? processNames = null)
        {
            Dictionary<int, Dictionary<int, int>> gcThreadsForAllProcesses = new();

            eventSource.Clr.GCMarkWithType += (markData) =>
            {
                // If we passed in the process names, check if the process name exists in the hash set.
                if (processNames != null && !processNames.Contains(markData.ProcessName))
                {
                    return;
                }

                if (!gcThreadsForAllProcesses.TryGetValue(markData.ProcessID, out var gcThreads))
                {
                    gcThreadsForAllProcesses[markData.ProcessID] = gcThreads = new Dictionary<int, int>();
                }

                gcThreads[markData.ThreadID] = markData.HeapNum;
            };

            eventSource.Process();
            return gcThreadsForAllProcesses;
        }

        internal static Predicate<TraceLoadedDotNetRuntime> IsInterestingGCProcess = (managedProcess) =>
                (managedProcess != null &&  // If the process in question is a managed process.
                 managedProcess.GC != null &&  // If the managed process has GCs.
                 managedProcess.GC.GCs != null &&  // "
                 managedProcess.GC.GCs.Count > 0); // "
        private bool disposedValue;

        public Etlx.TraceLog TraceLog { get; }
        public string TraceLogPath { get; }
        public Dictionary<string, List<GCProcessData>> AllGCProcessData { get; } = new(StringComparer.OrdinalIgnoreCase);
        public CPUAnalyzer CPUAnalyzer { get; set; }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    TraceLog?.Dispose();
                    AllGCProcessData.Clear();
                    CPUAnalyzer?.SymbolReader?.Dispose();
                    CPUAnalyzer = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}
