using GC.Analysis.API;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using Newtonsoft.Json;

namespace GC.Infrastructure.Core.Analysis.Microbenchmarks
{
    public sealed class MicrobenchmarkResult
    {
        public Statistics? Statistics { get; set; }

        [JsonIgnore]
        public GCProcessData? GCData { get; set; }

        public GCTraceMetrics? GCTraceMetrics { get; set; }

        [JsonIgnore]
        public CPUProcessData? CPUData { get; set; }
        public Run? Parent { get; set; }
        public string? MicrobenchmarkName { get; set; }
        public Dictionary<string, double?> OtherMetrics { get; set; } = new();

        private static readonly IReadOnlyDictionary<string, Func<Statistics, double?>> _customStatisticsCalculationMap = new Dictionary<string, Func<Statistics, double?>>(StringComparer.OrdinalIgnoreCase)
        {
            { "number of iterations", (Statistics stats) => stats.N },
            { "min", (Statistics stats) => stats.Min },
            { "max", (Statistics stats) => stats.Max },
            { "median", (Statistics stats) => stats.Median },
            { "q1", (Statistics stats) => stats.Q1 },
            { "q3", (Statistics stats) => stats.Q3 },
            { "variance", (Statistics stats) => stats.Variance },
            { "standard deviation", (Statistics stats) => stats.StandardDeviation },
            { "skewness", (Statistics stats) => stats.Skewness },
            { "kurtosis", (Statistics stats) => stats.Kurtosis },
            { "standard error", (Statistics stats) => stats.StandardError },
            { "standard error / mean", (Statistics stats) => stats.StandardError / stats.Mean },
        };

        public static double? LookupStatisticsCalculation(string columnName, MicrobenchmarkResult result)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return null;
            }

            if (!_customStatisticsCalculationMap.TryGetValue(columnName, out var val))
            {
                return null;
            }

            else
            {
                return val.Invoke(result.Statistics);
            }
        }
    }

}