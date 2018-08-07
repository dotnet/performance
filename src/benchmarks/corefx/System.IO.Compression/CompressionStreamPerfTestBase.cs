// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace System.IO.Compression
{
    public abstract class CompressionStreamPerfTestBase : CompressionStreamTestBase
    {
        public static IEnumerable<object[]> UncompressedTestFiles_WithCompressionLevel()
        {
            foreach (CompressionLevel compressionLevel in Enum.GetValues(typeof(CompressionLevel)))
            {
                foreach (string testFile in UncompressedTestFileNames())
                {
                    yield return new object[] { testFile, compressionLevel };
                }
            }
        }

        private IReadOnlyDictionary<string, byte[]> _uncompressedTestFiles;
        private IReadOnlyDictionary<string, (MemoryStream compressedStream, byte[] bytes)> _compressedTestFiles;
        private MemoryStream _compressedDataStream;

        [GlobalSetup(Target = nameof(Compress_Canterbury))]
        public void SetupCompress_Canterbury()
        {
            _uncompressedTestFiles = UncompressedTestFileNames().ToDictionary(fileName => fileName, fileName => File.ReadAllBytes(GetFilePath(fileName)));
            _compressedDataStream = new MemoryStream(_uncompressedTestFiles.Values.Max(content => content.Length));
        }

        /// <summary>
        /// Benchmark tests to measure the performance of individually compressing each file in the
        /// Canterbury Corpus
        /// </summary>
        [Benchmark]
        [ArgumentsSource(nameof(UncompressedTestFiles_WithCompressionLevel))]
        public void Compress_Canterbury(string uncompressedFileName, CompressionLevel compressLevel)
        {
            byte[] bytes = _uncompressedTestFiles[uncompressedFileName];

            _compressedDataStream.Position = 0;

            Stream compressor = CreateStream(_compressedDataStream, compressLevel);
            compressor.Write(bytes, 0, bytes.Length);
        }

        [GlobalCleanup(Target = nameof(Compress_Canterbury))]
        public void CleanupCompress_Canterbury() => _compressedDataStream.Dispose();

        [GlobalSetup(Target = nameof(Decompress_Canterbury))]
        public void SetupDecompress_Canterbury()
        {
            _compressedTestFiles = UncompressedTestFileNames()
                .ToDictionary(fileName => fileName, fileName =>
                {
                    var bytes = File.ReadAllBytes(GetFilePath(fileName));
                    var compressedStream = new MemoryStream(bytes.Length);
                    
                    var compressor = CreateStream(compressedStream, CompressionMode.Compress, leaveOpen: true);
                    compressor.Write(bytes, 0, bytes.Length);

                    return (compressedStream, bytes);
                });
        }

        /// <summary>
        /// Benchmark tests to measure the performance of individually compressing each file in the
        /// Canterbury Corpus
        /// </summary>
        [Benchmark]
        [ArgumentsSource(nameof(UncompressedTestFiles))]
        public void Decompress_Canterbury(string uncompressedFilePath)
        {
            var (compressedStream, bytes) = _compressedTestFiles[uncompressedFilePath];
            compressedStream.Position = 0;
            
            Stream decompressor = CreateStream(compressedStream, CompressionMode.Decompress);
            decompressor.Read(bytes, 0, bytes.Length);
        }

        [GlobalCleanup(Target = nameof(Decompress_Canterbury))]
        public void CleanupDecompress_Canterbury()
        {
            foreach (var item in _compressedTestFiles.Values)
                item.compressedStream.Dispose();
        }
    }
}
