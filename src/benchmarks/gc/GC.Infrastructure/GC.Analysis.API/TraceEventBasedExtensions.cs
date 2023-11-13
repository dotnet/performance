using Microsoft.Diagnostics.Tracing.Analysis;
using Etlx = Microsoft.Diagnostics.Tracing.Etlx;

namespace GC.Analysis.API
{
    public static class TraceEventBasedExtensions
    {
        public static IEnumerable<TraceProcess> GetAllProcesses(this Etlx.TraceLog traceLog)
        {
            var eventSource = traceLog.Events.GetSource();
            eventSource.NeedLoadedDotNetRuntimes();
            eventSource.Process();
            return eventSource.Processes();
        }

        public static IEnumerable<TraceLoadedDotNetRuntime> GetValidGCProcesses(this Etlx.TraceLog traceLog)
        {
            var eventSource = traceLog.Events.GetSource();
            eventSource.NeedLoadedDotNetRuntimes();
            eventSource.Process();
            return eventSource.Processes()
                .EagerSelect(p => p.LoadedDotNetRuntime())
                .EagerWhere(p =>
                {
                    return p != null &&
                           p.GC != null &&
                           p.GC.GCs != null &&
                           p.GC.GCs.Count > 0;
                });
        }
    }
}
