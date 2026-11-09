using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Reporting;

namespace ScenarioMeasurement;

public sealed class BuildWallClockParser : IParser
{
    public void EnableKernelProvider(ITraceSession kernel)
    {
        throw new NotSupportedException();
    }

    public void EnableUserProviders(ITraceSession user)
    {
        throw new NotSupportedException();
    }

    public IEnumerable<Counter> Parse(string measurementsFile, string processName, IList<int> pids, string commandLine)
    {
        using (var document = JsonDocument.Parse(File.ReadAllText(measurementsFile)))
        {
            var root = document.RootElement;
            var samples = root.ValueKind == JsonValueKind.Object ? root.GetProperty("samples") : root;
            var results = samples.Deserialize<double[]>()
                ?? throw new InvalidDataException("Build measurements cannot be null.");
            if (results.Length == 0 || results.Any(value => !double.IsFinite(value) || value <= 0))
            {
                throw new InvalidDataException("Build measurements must contain positive finite durations.");
            }

            var counters = new List<Counter>
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
                if (root.TryGetProperty("counters", out var extraCounters))
                {
                    foreach (var extra in extraCounters.EnumerateArray())
                    {
                        var values = extra.GetProperty("samples").Deserialize<double[]>()
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

                if (root.TryGetProperty("sessions", out var requests))
                {
                    counters.AddRange(MSBuildEvaluationReader.Read(requests, measurementsFile));
                }
            }

            return counters;
        }
    }
}
