using GC.Analysis.API;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;

namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    public sealed class MicrobenchmarkResult
    {
        public Statistics? Statistics { get; set; }
        public GCProcessData? GCData { get; set; }
        public GCTraceMetrics? GCTraceMetrics { get; set; }
        public CPUProcessData? CPUData { get; set; }
        public Run? Parent { get; set; }
        public string? MicrobenchmarkName { get; set; }
    }

}