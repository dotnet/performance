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
                foreach (object[] testFile in UncompressedTestFiles())
                {
                    yield return new object[] { testFile[0], compressionLevel };
                }
            }
        }

        private IReadOnlyDictionary<string, byte[]> _uncompressedTestFiles;
        private MemoryStream _compressedDataStream;

        [GlobalSetup(Target = nameof(Compress_Canterbury))]
        public void SetupCompress_Canterbury()
        {
            _uncompressedTestFiles = UncompressedTestFileNames().ToDictionary(fileName => fileName, File.ReadAllBytes);
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

            using (Stream compressor = CreateStream(_compressedDataStream, compressLevel))
                compressor.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Benchmark tests to measure the performance of individually compressing each file in the
        /// Canterbury Corpus
        /// </summary>
        [Benchmark(InnerIterationCount=100)]
        [ArgumentsSource(nameof(UncompressedTestFiles))]
        public void Decompress_Canterbury(string uncompressedFilePath)
        {
            int innerIterations = (int)Benchmark.InnerIterationCount;
            string compressedFilePath = CompressedTestFile(uncompressedFilePath);
            byte[] outputRead = new byte[new FileInfo(uncompressedFilePath).Length];
            MemoryStream[] memories = new MemoryStream[innerIterations];
            foreach (var iteration in Benchmark.Iterations)
            {
                for (int i = 0; i < innerIterations; i++)
                    memories[i] = new MemoryStream(File.ReadAllBytes(compressedFilePath));

                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                        using (Stream decompressor = CreateStream(memories[i], CompressionMode.Decompress))
                            decompressor.Read(outputRead, 0, outputRead.Length);

                for (int i = 0; i < innerIterations; i++)
                    memories[i].Dispose();
            }
        }
    }
}
