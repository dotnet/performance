// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET10_0_OR_GREATER

using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Compression
{
    public enum ZipArchiveSource
    {
        Memory,
        File
    }

    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class ZipArchiveRead
    {
        private const int PayloadEntryCount = 32;
        private const int PayloadLength = 4096;

        private byte[] _archiveData;
        private string _archivePath;

        [ParamsAllValues]
        public ZipArchiveSource source { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            using var archiveStream = new MemoryStream();
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                WritePackageMetadata(archive);

                byte[] payload = new byte[PayloadLength];
                new Random(42).NextBytes(payload);

                for (int i = 0; i < PayloadEntryCount; i++)
                {
                    ZipArchiveEntry entry = archive.CreateEntry($"content/{i:D2}.bin", CompressionLevel.Fastest);
                    using Stream entryStream = entry.Open();
                    entryStream.Write(payload);
                }
            }

            _archiveData = archiveStream.ToArray();
            _archivePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
            File.WriteAllBytes(_archivePath, _archiveData);
        }

        [GlobalCleanup]
        public void Cleanup() => File.Delete(_archivePath);

        [Benchmark(Baseline = true)]
        public int ReadPackageMetadata()
        {
            using Stream archiveStream = OpenArchive(useAsync: false);
            using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
            ZipArchiveEntry metadataEntry = GetMetadataEntry(archive);
            using Stream entryStream = metadataEntry.Open();
            byte[] metadata = new byte[checked((int)metadataEntry.Length)];
            entryStream.ReadExactly(metadata);
            using JsonDocument document = JsonDocument.Parse(metadata);
            return ConsumeMetadata(document);
        }

        [Benchmark]
        public int ReadPackageMetadataAsync() => ReadPackageMetadataCoreAsync().GetAwaiter().GetResult();

        private async Task<int> ReadPackageMetadataCoreAsync()
        {
            await using Stream archiveStream = OpenArchive(useAsync: true);
            await using ZipArchive archive = await ZipArchive.CreateAsync(
                archiveStream,
                ZipArchiveMode.Read,
                leaveOpen: true,
                entryNameEncoding: null,
                cancellationToken: CancellationToken.None);

            ZipArchiveEntry metadataEntry = GetMetadataEntry(archive);
            await using Stream entryStream = await metadataEntry.OpenAsync(CancellationToken.None);
            byte[] metadata = new byte[checked((int)metadataEntry.Length)];
            int totalBytesRead = 0;
            while (totalBytesRead < metadata.Length)
            {
                int bytesRead = await entryStream.ReadAsync(metadata.AsMemory(totalBytesRead), CancellationToken.None);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }

                totalBytesRead += bytesRead;
            }

            using JsonDocument document = JsonDocument.Parse(metadata);
            return ConsumeMetadata(document);
        }

        private Stream OpenArchive(bool useAsync) =>
            source switch
            {
                ZipArchiveSource.Memory => new MemoryStream(_archiveData, writable: false),
                ZipArchiveSource.File => new FileStream(
                    _archivePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: useAsync),
                _ => throw new InvalidOperationException()
            };

        private static ZipArchiveEntry GetMetadataEntry(ZipArchive archive) =>
            archive.Entries.First(entry => entry.FullName == "package-metadata.json");

        private static int ConsumeMetadata(JsonDocument document)
        {
            JsonElement metadata = document.RootElement.GetProperty("metadata");
            int dependencyCount = metadata.GetProperty("dependencies").GetArrayLength();
            string tags = metadata.GetProperty("tags").GetString();

            return dependencyCount + (tags.Contains("polyglot", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        }

        private static void WritePackageMetadata(ZipArchive archive)
        {
            const string metadata = """
                {
                  "metadata": {
                    "id": "Aspire.Hosting.Redis",
                    "version": "13.0.0-preview.1",
                    "authors": ["Microsoft"],
                    "description": "Redis hosting integration for Aspire.",
                    "tags": "aspire integration hosting redis cache caching polyglot",
                    "dependencies": [
                      {
                        "id": "Aspire.Hosting",
                        "version": "[13.0.0-preview.1]"
                      },
                      {
                        "id": "Aspire.Hosting.Redis.Client",
                        "version": "[13.0.0-preview.1]"
                      },
                      {
                        "id": "Microsoft.Extensions.Hosting.Abstractions",
                        "version": "10.0.0"
                      }
                    ]
                  }
                }
                """;

            ZipArchiveEntry metadataEntry = archive.CreateEntry("package-metadata.json", CompressionLevel.Fastest);
            using Stream stream = metadataEntry.Open();
            using var writer = new StreamWriter(stream);
            writer.Write(metadata);
        }
    }
}

#endif
