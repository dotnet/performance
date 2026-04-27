using Microsoft.Diagnostics.Tracing.Analysis.GC;

namespace GC.Analysis.API
{
    public sealed class CPUInfo
    {
        public CPUInfo(TraceGC gc, float count)
        {
            GC = gc;
            Count = count;
        }

        public TraceGC GC { get; }
        public float Count { get; }
    }
}
