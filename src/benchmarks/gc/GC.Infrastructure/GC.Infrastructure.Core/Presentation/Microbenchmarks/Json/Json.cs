using GC.Infrastructure.Core.Analysis.Microbenchmarks;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using Newtonsoft.Json;

namespace GC.Infrastructure.Core.Presentation.Microbenchmarks.Json
{
    public static class Json
    {
        public static void Generate(MicrobenchmarkConfiguration configuration, IReadOnlyList<MicrobenchmarkComparisonResults> comparisonResults, string path)
        {
            string json = JsonConvert.SerializeObject(comparisonResults);
            File.WriteAllText(path, json);
        }
    }
}
