// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Xunit;

namespace ArtifactsUploader.Tests
{
    public class CompressorTests
    {
        [Fact]
        public void CompressedArchiveCanBeDecompressedAndContainsExactSameData()
        {
            const string content = "aVeryNiceString";

            var loggerMock = LoggerMockHelpers.CreateLoggerMock();
            var tempFileWithContent = new FileInfo(Path.GetTempFileName());
            var archiveFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip"));
            var decompressed = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}"));

            try
            {
                File.WriteAllText(tempFileWithContent.FullName, content);
                
                Compressor.Compress(archiveFile, new []{ tempFileWithContent }, loggerMock.Object);
                
                Decompress(archiveFile, decompressed);

                var decompressedFile = decompressed.EnumerateFiles().Single();
                var decompressedContent = File.ReadAllText(decompressedFile.FullName);
                
                Assert.Equal(content, decompressedContent);
            }
            finally
            {
                tempFileWithContent.Delete();
                archiveFile.Delete();
                decompressed.Delete(recursive: true);
            }
        }

        private static void Decompress(FileInfo fileToDecompress, DirectoryInfo decompressed)
        {
            using (var zip = ZipFile.OpenRead(fileToDecompress.FullName))
            {
                zip.ExtractToDirectory(decompressed.FullName);
            }
        }
    }
}