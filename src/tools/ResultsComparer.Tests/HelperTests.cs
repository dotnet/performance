using System.IO;
using System.Text;
using DataTransferContracts;
using Xunit;

namespace ResultsComparer.Tests;

public class HelperTests
{
    [Fact]
    public void GetFilesToParseReturnsAllFullJsonFilesFromDirectory()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var nestedDirectory = Directory.CreateDirectory(Path.Combine(tempDir.FullName, "nested"));
            var expectedA = Path.Combine(tempDir.FullName, "first.full.json");
            var expectedB = Path.Combine(nestedDirectory.FullName, "second.full.json");

            File.WriteAllText(expectedA, "{}");
            File.WriteAllText(expectedB, "{}");
            File.WriteAllText(Path.Combine(tempDir.FullName, "ignored.json"), "{}");

            var result = global::ResultsComparer.Helper.GetFilesToParse(tempDir.FullName);

            Assert.Equal(2, result.Length);
            Assert.Contains(expectedA, result);
            Assert.Contains(expectedB, result);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetFilesToParseThrowsForMissingFullJsonPath()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var missingPath = Path.Combine(tempDir.FullName, "missing.full.json");

            Assert.Throws<FileNotFoundException>(() => global::ResultsComparer.Helper.GetFilesToParse(missingPath));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ReadFromStreamDeserializesBenchmarkResults()
    {
        var json = ResultsComparerTestData.CreateBdnJson(originalValues: [1.0, 1.1, 1.2], median: 1.1);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        BdnResult result = global::ResultsComparer.Helper.ReadFromStream(stream);

        Assert.Equal("SampleBenchmark-20240504-182513", result.Title);
        Assert.Equal("X64", result.HostEnvironmentInfo.Architecture);
        Assert.Single(result.Benchmarks);
        Assert.Equal("Demo.Namespace.SampleBenchmark", result.Benchmarks[0].FullName);
    }

    [Fact]
    public void ReadFromFileDeserializesBenchmarkResults()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var filePath = Path.Combine(tempDir.FullName, "sample.full.json");
            File.WriteAllText(filePath, ResultsComparerTestData.CreateBdnJson());

            BdnResult result = global::ResultsComparer.Helper.ReadFromFile(filePath);

            Assert.Equal("SampleBenchmark-20240504-182513", result.Title);
            Assert.Single(result.Benchmarks);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetFilesToParseReturnsExplicitFilePath()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var filePath = Path.Combine(tempDir.FullName, "custom-name.json");
            File.WriteAllText(filePath, ResultsComparerTestData.CreateBdnJson());

            var result = global::ResultsComparer.Helper.GetFilesToParse(filePath);

            Assert.Equal([filePath], result);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetModalInfoReturnsNullForSmallSampleSets()
    {
        var benchmark = new Benchmark
        {
            Statistics = new Statistics
            {
                N = 3,
                OriginalValues = [1.0, 1.1, 1.2]
            }
        };

        Assert.Null(global::ResultsComparer.Helper.GetModalInfo(benchmark));
    }

    [Fact]
    public void GetModalInfoDetectsMultiClusterData()
    {
        var benchmark = new Benchmark
        {
            Statistics = new Statistics
            {
                N = 16,
                OriginalValues = [1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 50.0, 50.0, 50.0, 50.0, 50.0, 50.0, 50.0, 50.0]
            }
        };

        var modality = global::ResultsComparer.Helper.GetModalInfo(benchmark);

        Assert.Equal("bimodal", modality);
    }
}
