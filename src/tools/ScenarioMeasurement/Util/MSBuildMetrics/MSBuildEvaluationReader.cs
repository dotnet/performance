#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Build.PerformanceTests.Measurements;
using Reporting;

namespace ScenarioMeasurement;

internal static class MSBuildEvaluationReader
{
    private static readonly string[] s_passes = { "pass0", "pass1", "pass2", "pass3", "pass3dot1", "pass4", "pass5" };

    internal static IEnumerable<Counter> Read(JsonElement requests, string measurementsPath)
    {
        Dictionary<string, List<double>> values = new();
        HashSet<string> incompleteCounters = new();
        List<object> extrema = new();
        Dictionary<string, bool> availability = new();
        foreach (JsonElement request in requests.EnumerateArray())
        {
            string variant = request.GetProperty("variant").GetString() ?? "";
            string prefix = variant.Length == 0 ? "" : variant + "-";
            string directory = Path.GetFullPath(request.GetProperty("directory").GetString()!,
                Path.GetDirectoryName(Path.GetFullPath(measurementsPath))!);
            string sessionId = request.GetProperty("session").GetString()!;
            using EvaluationMetricsSession session = new(
                directory,
                forceProcessExit: false,
                sessionId: sessionId,
                preserveFiles: true);
            EvaluationMetricsSnapshot? snapshot;
            if (request.TryGetProperty("window", out JsonElement window))
            {
                long frequency = request.GetProperty("clock_frequency").GetInt64();
                if (frequency <= 0)
                {
                    throw new InvalidDataException("Invalid evaluation timestamp frequency.");
                }
                long ToTicks(long nanoseconds) =>
                    (long)((Int128)nanoseconds * frequency / 1_000_000_000);
                JsonElement priming = request.GetProperty("priming_window");
                EvaluationMetricsWindowReadResult result = session.ReadByTimeWindows(
                    new[]
                    {
                        new EvaluationMetricsTimeWindow(
                            ToTicks(priming.GetProperty("start").GetInt64()),
                            ToTicks(priming.GetProperty("end").GetInt64())),
                        new EvaluationMetricsTimeWindow(
                            ToTicks(window.GetProperty("start").GetInt64()),
                            ToTicks(window.GetProperty("end").GetInt64()))
                    },
                    waitForFirstFile: true);
                if (result.IsAvailable && result.Windows.Any(item => item is null))
                {
                    throw new InvalidDataException("A warm evaluation window lacks complete collector evidence.");
                }

                snapshot = result.Windows.Last();
            }
            else
            {
                snapshot = request.GetProperty("require_server").GetBoolean()
                    ? session.ReadFromServer(TimeSpan.FromSeconds(30))
                    : session.Read(TimeSpan.FromSeconds(30));
            }
            if (availability.TryGetValue(prefix, out bool previous) && previous != (snapshot is not null))
            {
                throw new InvalidDataException("Evaluation metric availability changed between samples.");
            }
            availability[prefix] = snapshot is not null;
            string metricsDirectory = Path.Combine(directory, ".perfstar-evaluation-metrics");
            Add(prefix + "evaluation-collector-lifecycle-retries-count",
                Directory.GetFiles(metricsDirectory, sessionId + "-*.retries.txt")
                    .Sum(path => File.ReadAllLines(path).Length));
            Add(prefix + "evaluation-metrics-available", snapshot is null ? 0 : 1);
            if (snapshot is null)
            {
                if (request.GetProperty("require_server").GetBoolean())
                {
                    throw new InvalidDataException("Required server evaluation metrics are unavailable.");
                }
                Console.Error.WriteLine("MSBuild evaluation metrics are unavailable for this invocation.");
                continue;
            }

            Add(prefix + "evaluation-time-metrics", snapshot.TotalDurationMilliseconds);
            Add(prefix + "evaluation-metrics-count", snapshot.TotalCount);
            Add(prefix + "outside-build-submission-evaluation-time-metrics", snapshot.OutsideBuildSubmissionDurationMilliseconds);
            Add(prefix + "outside-build-submission-evaluation-metrics-count", snapshot.OutsideBuildSubmissionCount);
            AddPasses(prefix + "evaluation-time-", snapshot.PassDurationsMilliseconds, snapshot.TotalCount);
            AddPasses(prefix + "outside-build-submission-evaluation-time-",
                snapshot.OutsideBuildSubmissionPassDurationsMilliseconds, snapshot.OutsideBuildSubmissionCount);
            extrema.Add(new
            {
                iteration = request.GetProperty("iteration").GetInt32(),
                variant,
                minimum = snapshot.MinimumEvaluation,
                maximum = snapshot.MaximumEvaluation
            });
        }
        File.WriteAllText(
            Path.Combine(Path.GetDirectoryName(Path.GetFullPath(measurementsPath))!, "evaluation-extrema.json"),
            JsonSerializer.Serialize(extrema, new JsonSerializerOptions { WriteIndented = true }));
        return values.Where(item => !incompleteCounters.Contains(item.Key))
            .Select(item => new Counter
            {
                Name = item.Key,
                MetricName = item.Key.EndsWith("count", StringComparison.Ordinal) ||
                    item.Key.EndsWith("available", StringComparison.Ordinal) ? "count" : "ms",
                TopCounter = true,
                Results = item.Value
            }).ToArray();

        void Add(string name, double value)
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new InvalidDataException($"Invalid evaluation metric '{name}'.");
            }
            if (!values.TryGetValue(name, out List<double>? samples))
            {
                samples = new List<double>();
                values.Add(name, samples);
            }
            samples.Add(value);
        }

        void AddPasses(string family, IReadOnlyDictionary<string, double> durations, long count)
        {
            if (count != 0 && durations.Count == 0)
            {
                foreach (string pass in s_passes)
                {
                    incompleteCounters.Add(family + pass + "-metrics");
                }
                Console.Error.WriteLine($"Evaluation pass family '{family}' is unavailable; omitting it.");
                return;
            }
            foreach (string pass in s_passes)
            {
                Add(family + pass + "-metrics", durations.TryGetValue(pass, out double duration) ? duration : 0);
            }
        }
    }
}
