using System.Globalization;
using System.Linq;

namespace ResultsComparer.Tests;

internal static class ResultsComparerTestData
{
    internal static string CreateBdnJson(
        string title = "SampleBenchmark-20240504-182513",
        string fullName = "Demo.Namespace.SampleBenchmark",
        string? benchmarkNamespace = "Demo.Namespace",
        double[]? originalValues = null,
        double? median = null,
        long? allocatedBytes = 42,
        string benchmarkDotNetVersion = "0.13.10",
        string osVersion = "Windows 11 (10.0.26100)",
        string processorName = "Intel Core i7-8700",
        string architecture = "X64")
    {
        originalValues ??= [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111];
        median ??= originalValues.OrderBy(v => v).ElementAt(originalValues.Length / 2);
        string values = string.Join(", ", originalValues.Select(v => v.ToString(CultureInfo.InvariantCulture)));
        string namespaceField = benchmarkNamespace is null ? "null" : $"\"{benchmarkNamespace}\"";
        string memoryField = allocatedBytes is null
            ? "\"Memory\": {}"
            : $"\"Memory\": {{ \"BytesAllocatedPerOperation\": {allocatedBytes.Value.ToString(CultureInfo.InvariantCulture)} }}";

        return $$"""
            {
              "Title": "{{title}}",
              "HostEnvironmentInfo": {
                "BenchmarkDotNetVersion": "{{benchmarkDotNetVersion}}",
                "OsVersion": "{{osVersion}}",
                "ProcessorName": "{{processorName}}",
                "Architecture": "{{architecture}}"
              },
              "Benchmarks": [
                {
                  "Namespace": {{namespaceField}},
                  "FullName": "{{fullName}}",
                  "Statistics": {
                    "OriginalValues": [{{values}}],
                    "N": {{originalValues.Length}},
                    "Median": {{median.Value.ToString(CultureInfo.InvariantCulture)}}
                  },
                  {{memoryField}}
                }
              ]
            }
            """;
    }
}
