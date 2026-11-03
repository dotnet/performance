using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ScenarioMeasurement;
using Xunit;

namespace Startup.Tests;

public class MSBuildEvaluationParserTests
{
    [Theory]
    [InlineData(300, 400, 25, 1)]
    [InlineData(260, 280, 0, 0)]
    public void WarmWindowsExcludePrimingAndOnlyReportProvenZero(
        long start, long end, double expectedTime, double expectedCount)
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            string input = CreateInput(root, start, end);
            List<Reporting.Counter> counters = new BuildWallClockParser()
                .Parse(input, "dotnet", new List<int>(), "").ToList();
            Assert.Equal(expectedTime, counters.Single(counter => counter.Name == "evaluation-time-metrics").Results[0]);
            Assert.Equal(expectedCount, counters.Single(counter => counter.Name == "evaluation-metrics-count").Results[0]);
            Assert.True(File.Exists(Path.Combine(root, "evaluation-extrema.json")));
            Assert.True(Directory.GetFiles(Path.Combine(root, ".perfstar-evaluation-metrics"), "*.metrics.tsv").Length == 1);
            Assert.Equal(counters.Count, new BuildWallClockParser().Parse(input, "dotnet", new List<int>(), "").Count());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void MissingPrimingEvidenceCannotPublishMeasuredWarmMetrics()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            string input = CreateInput(root, 300, 400);
            string json = File.ReadAllText(input).Replace(
                "\"priming_window\":{\"start\":150,\"end\":250}",
                "\"priming_window\":{\"start\":120,\"end\":150}");
            File.WriteAllText(input, json);
            Assert.Throws<InvalidDataException>(() =>
                new BuildWallClockParser().Parse(input, "dotnet", new List<int>(), "").ToList());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RequiredServerMetricsCannotFallBackToClientMetrics()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            string input = CreateInput(root, null, null, requireServer: true);
            InvalidOperationException error = Assert.ThrowsAny<InvalidOperationException>(() =>
                new BuildWallClockParser().Parse(input, "dotnet", new List<int>(), "").ToList());
            Assert.Contains("MSBuild Server", error.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PartialLifecycleCohortsCannotPublishPartialMetrics()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            string input = CreateInput(root, 300, 400);
            string first = Directory.GetFiles(
                Path.Combine(root, ".perfstar-evaluation-metrics"), "*.metrics.tsv").Single();
            File.Copy(first, first.Replace("-17.metrics.tsv", "-18.metrics.tsv"));
            IOException error = Assert.ThrowsAny<IOException>(() =>
                new BuildWallClockParser().Parse(input, "dotnet", new List<int>(), "").ToList());
            Assert.Contains("cohort", error.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void MissingLifecycleCannotEstablishZeroForAnEmptyWarmWindow()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            string input = CreateInput(root, 300, 400);
            foreach (string file in Directory.GetFiles(
                Path.Combine(root, ".perfstar-evaluation-metrics"), "*.lifecycle.tsv"))
            {
                File.Delete(file);
            }
            // Legacy files without lifecycle cannot establish a zero-evaluation warm window.
            string json = File.ReadAllText(input).Replace("\"start\":300", "\"start\":260").Replace("\"end\":400", "\"end\":280");
            File.WriteAllText(input, json);
            InvalidDataException error = Assert.Throws<InvalidDataException>(() =>
                new BuildWallClockParser().Parse(input, "dotnet", new List<int>(), "").ToList());
            Assert.Contains("warm evaluation window", error.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateInput(string root, long? start, long? end, bool requireServer = false)
    {
        Directory.CreateDirectory(root);
        string metrics = Path.Combine(root, ".perfstar-evaluation-metrics");
        Directory.CreateDirectory(metrics);
        string token = Guid.NewGuid().ToString("N");
        string project = Convert.ToBase64String(Encoding.UTF8.GetBytes("app.csproj"));
        File.WriteAllText(Path.Combine(metrics, token + "-17.metrics.tsv"),
            "timestamp\tinstrument\tstage\tpass\torigin\tsucceeded\tdurationSeconds\tprojectFileBase64\tevaluationId\n" +
            $"200\tmsbuild.project.evaluation.duration\tfull\t\tbuild_submission\tTrue\t0.7\t{project}\t1\n" +
            $"350\tmsbuild.project.evaluation.duration\tfull\t\tbuild_submission\tTrue\t0.025\t{project}\t2\n");
        File.WriteAllText(Path.Combine(metrics, token + "-17.lifecycle.tsv"),
            "version\tsession\tprocessId\thookStartTimestamp\tlistenerStartTimestamp\tcompletionTimestamp\tcapabilityTimestamp\n" +
            $"2\t{token}\t17\t100\t110\t500\t200\n");
        Dictionary<string, object> request = new()
        {
            ["directory"] = ".",
            ["session"] = token,
            ["variant"] = "",
            ["iteration"] = 0,
            ["require_server"] = requireServer,
            ["clock_frequency"] = 1_000_000_000L
        };
        if (start.HasValue)
        {
            request["window"] = new { start = start.Value, end = end.Value };
            request["priming_window"] = new { start = 150L, end = 250L };
        }
        string input = Path.Combine(root, "build-wall-clock.json");
        File.WriteAllText(input, JsonSerializer.Serialize(new { samples = new[] { 10d }, sessions = new[] { request } }));
        return input;
    }
}
