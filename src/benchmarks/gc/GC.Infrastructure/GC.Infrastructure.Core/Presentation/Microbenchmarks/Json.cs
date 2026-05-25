using GC.Infrastructure.Core.Analysis.Microbenchmarks;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using Newtonsoft.Json;

namespace GC.Infrastructure.Core.Presentation.Microbenchmarks
{
    public static class Json
    {
        public static void Generate(MicrobenchmarkConfiguration configuration, List<MicrobenchmarkComparisonResults> comparisonResultsGroupedByName, string path)
        {
            string json = JsonConvert.SerializeObject(comparisonResultsGroupedByName);
            File.WriteAllText(path, json);
        }
    }
}
