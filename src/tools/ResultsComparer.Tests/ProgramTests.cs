using System;
using System.IO;
using Xunit;

namespace ResultsComparer.Tests;

[Collection("Console output")]
public class ProgramTests
{
    [Fact]
    public void MainReportsInvalidThreshold()
    {
        var output = InvokeProgram(["--base", "base.json", "--diff", "diff.json", "--threshold", "not-a-threshold"]);

        Assert.Contains("Invalid Threshold", output);
    }

    [Fact]
    public void MainReportsMissingMatrixInputDirectory()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var output = InvokeProgram(["matrix", "--input", missingDirectory, "--base", "base", "--diff", "diff", "--threshold", "5%"]);

        Assert.Contains("does NOT exist", output);
    }

    [Fact]
    public void MainPrintsNoDifferencesForEquivalentInputs()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var baseFile = Path.Combine(tempDir.FullName, "base.full.json");
            var diffFile = Path.Combine(tempDir.FullName, "diff.full.json");
            var json = ResultsComparerTestData.CreateBdnJson();

            File.WriteAllText(baseFile, json);
            File.WriteAllText(diffFile, json);

            var output = InvokeProgram(["--base", baseFile, "--diff", diffFile, "--threshold", "5%"]);

            Assert.Contains("No differences found", output);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void MatrixCommandPrintsLegendAndBenchmarkTable()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var inputDirectory = Directory.CreateDirectory(Path.Combine(tempDir.FullName, "input"));
            var baseDirectory = Directory.CreateDirectory(Path.Combine(inputDirectory.FullName, "run-base"));
            var diffDirectory = Directory.CreateDirectory(Path.Combine(inputDirectory.FullName, "run-diff"));

            File.WriteAllText(
                Path.Combine(baseDirectory.FullName, "SampleBenchmark.full.json"),
                ResultsComparerTestData.CreateBdnJson(
                    originalValues: [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111],
                    median: 106,
                    allocatedBytes: 10));
            File.WriteAllText(
                Path.Combine(diffDirectory.FullName, "SampleBenchmark.full.json"),
                ResultsComparerTestData.CreateBdnJson(
                    originalValues: [200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211],
                    median: 206,
                    allocatedBytes: 30));

            var output = InvokeProgram(["matrix", "--input", inputDirectory.FullName, "--base", "base", "--diff", "diff", "--threshold", "5%", "--ratio-only"]);

            Assert.Contains("# Legend", output);
            Assert.Contains("Demo.Namespace.SampleBenchmark", output);
            Assert.Contains("Slower", output);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    private static string InvokeProgram(string[] args)
    {
        using var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);
        try
        {
            Program.Main(args);
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
