using GC.Infrastructure.Core.Analysis;
using Newtonsoft.Json;

namespace GC.Infrastructure.Core.Presentation.GCPerfSim
{
    public static class Json
    {
        public static void GenerateForCompareCommand(GCTraceMetricComparisonResults metricComparisonResult, string path)
        {
            string json = JsonConvert.SerializeObject(metricComparisonResult);
            File.WriteAllText(path, json);
        }

        public static void GenerateForAnalyzeCommand(IEnumerable<GCTraceMetricComparisonResults> metricComparisonResults, string path)
        {
            string json = JsonConvert.SerializeObject(metricComparisonResults);
            File.WriteAllText(path, json);
        }
    }
}
