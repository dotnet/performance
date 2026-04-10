using System;
using System.IO;
using DataTransferContracts;
using Perfolizer.Mathematics.SignificanceTesting;
using Xunit;

namespace ResultsComparer.Tests;

[Collection("Console output")]
public class StatsTests
{
    [Fact]
    public void GetSimplifiedOSNameRemovesParentheticalSuffix()
    {
        Assert.Equal("Windows 11", global::ResultsComparer.Stats.GetSimplifiedOSName("Windows 11 (10.0.26100)"));
    }

    [Fact]
    public void PrintAggregatesTotalsAndEmitsSectionsOnce()
    {
        var stats = new global::ResultsComparer.Stats();
        var environment = new HostEnvironmentInfo
        {
            Architecture = "X64",
            OsVersion = "Windows 11 (10.0.26100)",
            ProcessorName = "Intel Core i7-8700"
        };
        var benchmark = new Benchmark
        {
            Namespace = "Demo.Namespace",
            Statistics = new Statistics
            {
                OriginalValues = new[] { 1.0, 1.1, 1.2 },
                N = 3,
                Median = 1.1
            },
            Memory = new Memory()
        };

        stats.Record(EquivalenceTestConclusion.Same, environment, benchmark);
        stats.Record(EquivalenceTestConclusion.Faster, environment, benchmark);
        stats.Record(EquivalenceTestConclusion.Slower, environment, benchmark);
        stats.Record(EquivalenceTestConclusion.Unknown, environment, benchmark);
        stats.Record(global::ResultsComparer.Stats.Noise, environment, benchmark);

        using var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);
        try
        {
            stats.Print();
            var firstOutput = writer.ToString();

            Assert.Contains("## Statistics", firstOutput);
            Assert.Contains("Total:   5", firstOutput);
            Assert.Contains("## Statistics per Architecture", firstOutput);
            Assert.Contains("## Statistics per Operating System", firstOutput);
            Assert.Contains("## Statistics per Namespace", firstOutput);
            Assert.Contains("Demo.Namespace", firstOutput);

            writer.GetStringBuilder().Clear();
            stats.Print();

            Assert.Equal(string.Empty, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void RecordThrowsForUnsupportedConclusion()
    {
        var stats = new global::ResultsComparer.Stats();
        var environment = new HostEnvironmentInfo { Architecture = "X64", OsVersion = "Windows 11 (10.0.26100)" };
        var benchmark = new Benchmark { Statistics = new Statistics { OriginalValues = [1.0], N = 1, Median = 1.0 }, Memory = new Memory() };

        Assert.Throws<NotSupportedException>(() => stats.Record((EquivalenceTestConclusion)999, environment, benchmark));
    }
}
