using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Reporting;

namespace ScenarioMeasurement;

public sealed class BuildWallClockParser : IParser
{
    public void EnableKernelProvider(ITraceSession kernel) => throw new NotSupportedException();

    public void EnableUserProviders(ITraceSession user) => throw new NotSupportedException();

    public IEnumerable<Counter> Parse(string measurementsFile, string processName, IList<int> pids, string commandLine)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(measurementsFile));
        JsonElement root = document.RootElement;
        JsonElement samples = root.ValueKind == JsonValueKind.Object ? root.GetProperty("samples") : root;
        double[] results = samples.Deserialize<double[]>()
            ?? throw new InvalidDataException("Build measurements cannot be null.");
        if (results.Length == 0 || results.Any(value => !double.IsFinite(value) || value <= 0))
        {
            throw new InvalidDataException("Build measurements must contain positive finite durations.");
        }

        List<Counter> counters = new()
        {
            new Counter
            {
                Name = "Build Wall Clock Time",
                MetricName = "ms",
                DefaultCounter = true,
                TopCounter = true,
                Results = results
            }
        };
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("counters", out JsonElement extraCounters))
            {
                foreach (JsonElement extra in extraCounters.EnumerateArray())
                {
                    double[] values = extra.GetProperty("samples").Deserialize<double[]>()
                        ?? throw new InvalidDataException("Counter samples cannot be null.");
                    if (values.Length != results.Length || values.Any(value => !double.IsFinite(value) || value <= 0))
                    {
                        throw new InvalidDataException("Counter samples must align with measured iterations.");
                    }
                    counters.Add(new Counter
                    {
                        Name = extra.GetProperty("name").GetString()!,
                        MetricName = "ms",
                        TopCounter = true,
                        Results = values
                    });
                }
            }
            if (root.TryGetProperty("sessions", out JsonElement requests))
            {
                counters.AddRange(MSBuildEvaluationReader.Read(requests, measurementsFile));
            }
        }
        return counters;
    }
}
