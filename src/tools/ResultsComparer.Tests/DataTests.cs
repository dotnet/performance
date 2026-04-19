using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace ResultsComparer.Tests;

public class DataTests
{
    [Fact]
    public void DecompressExtractsJsonFilesFromNestedZipArchives()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var outerZipPath = Path.Combine(tempDir.FullName, "results.zip");
            var outputDirectory = new DirectoryInfo(Path.Combine(tempDir.FullName, "output"));
            outputDirectory.Create();

            var innerZipBytes = CreateInnerZip(("net10.0/SampleBenchmark.full.json", ResultsComparerTestData.CreateBdnJson()));

            using (var fileStream = File.Create(outerZipPath))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("Performance-Runs/net10.0/testuser/results.zip");
                using var entryStream = entry.Open();
                entryStream.Write(innerZipBytes, 0, innerZipBytes.Length);
            }

            global::ResultsComparer.Data.Decompress(new FileInfo(outerZipPath), outputDirectory);

            var extractedFiles = Directory.GetFiles(outputDirectory.FullName, "*.full.json", SearchOption.AllDirectories);
            var extractedFile = Assert.Single(extractedFiles);
            var directoryName = Path.GetFileName(Path.GetDirectoryName(extractedFile));

            Assert.Contains("testuser", directoryName, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains("net10.0", directoryName, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SampleBenchmark", File.ReadAllText(extractedFile));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void DecompressExtractsJsonFilesFromTarGzArchives()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var outerZipPath = Path.Combine(tempDir.FullName, "results.zip");
            var outputDirectory = new DirectoryInfo(Path.Combine(tempDir.FullName, "output"));
            outputDirectory.Create();

            var tarGzBytes = CreateTarGzArchive(
                ("payload/SampleBenchmark.full.json", ResultsComparerTestData.CreateBdnJson()),
                ("payload/README.md", "ignored"));

            using (var fileStream = File.Create(outerZipPath))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("Performance-Runs/nativeaot10.0/testuser/arm64_win10-nativeaot10.0.tar.gz");
                using var entryStream = entry.Open();
                entryStream.Write(tarGzBytes, 0, tarGzBytes.Length);
            }

            global::ResultsComparer.Data.Decompress(new FileInfo(outerZipPath), outputDirectory);

            var extractedFiles = Directory.GetFiles(outputDirectory.FullName, "*.full.json", SearchOption.AllDirectories);
            var extractedFile = Assert.Single(extractedFiles);
            var directoryName = Path.GetFileName(Path.GetDirectoryName(extractedFile));

            Assert.Contains("nativeaot10.0", directoryName, System.StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void DecompressPrefersNewestBenchmarkDotNetVersionWhenDuplicatesExist()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var outerZipPath = Path.Combine(tempDir.FullName, "results.zip");
            var outputDirectory = new DirectoryInfo(Path.Combine(tempDir.FullName, "output"));
            outputDirectory.Create();

            var innerZipBytes = CreateInnerZip(
                ("net10.0/SampleBenchmark-a.full.json", ResultsComparerTestData.CreateBdnJson(benchmarkDotNetVersion: "0.13.9")),
                ("net10.0/SampleBenchmark-b.full.json", ResultsComparerTestData.CreateBdnJson(benchmarkDotNetVersion: "0.13.10")));

            using (var fileStream = File.Create(outerZipPath))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("Performance-Runs/net10.0/testuser/results.zip");
                using var entryStream = entry.Open();
                entryStream.Write(innerZipBytes, 0, innerZipBytes.Length);
            }

            global::ResultsComparer.Data.Decompress(new FileInfo(outerZipPath), outputDirectory);

            var extractedFile = Assert.Single(Directory.GetFiles(outputDirectory.FullName, "*.full.json", SearchOption.AllDirectories));
            var json = File.ReadAllText(extractedFile);

            Assert.Contains("\"BenchmarkDotNetVersion\": \"0.13.10\"", json);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    private static byte[] CreateInnerZip(params (string EntryName, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (entryName, content) in entries)
            {
                var entry = archive.CreateEntry(entryName);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }

        return stream.ToArray();
    }

    private static byte[] CreateTarGzArchive(params (string EntryName, string Content)[] entries)
    {
        using var tarStream = new MemoryStream();
        using (var tarWriter = new TarWriter(tarStream, leaveOpen: true))
        {
            foreach (var (entryName, content) in entries)
            {
                var tarEntry = new UstarTarEntry(TarEntryType.RegularFile, entryName)
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content))
                };

                tarWriter.WriteEntry(tarEntry);
            }
        }

        tarStream.Position = 0;

        using var gzipStream = new MemoryStream();
        using (var compressor = new GZipStream(gzipStream, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            tarStream.CopyTo(compressor);
        }

        return gzipStream.ToArray();
    }
}
