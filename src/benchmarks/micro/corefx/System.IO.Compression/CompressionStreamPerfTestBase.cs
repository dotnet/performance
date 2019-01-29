// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Compression
{
    public class Gzip : CompressionStreamPerfTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new GZipStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new GZipStream(stream, level);
    }
    
    public class Deflate : CompressionStreamPerfTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new DeflateStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new DeflateStream(stream, level);
    }
    
    // Brotli has a dedicated file with more benchmarks

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class CompressionStreamPerfTestBase
    {
        public abstract Stream CreateStream(Stream stream, CompressionMode mode);
        public abstract Stream CreateStream(Stream stream, CompressionLevel level);

        [ParamsSource(nameof(UncompressedTestFileNames))]
        public string file { get; set; } // this public property is called "file" to keep the benchmark ID in BenchView, do NOT rename it

        [Params(CompressionLevel.Optimal, CompressionLevel.Fastest)] // we don't test the performance of CompressionLevel.NoCompression on purpose
        public CompressionLevel level { get; set; } // this public property is called "file" to keep the benchmark ID in BenchView, do NOT rename it

        protected CompressedFile CompressedFile;

        [GlobalSetup]
        public void Setup() => CompressedFile = new CompressedFile(file, level, CreateStream); // this logic is quite expensive, needs to be a part of Setup

        public IEnumerable<string> UncompressedTestFileNames()
        {
            yield return "TestDocument.pdf"; // 199 KB small test document with repeated paragraph, PDF are common
            yield return "alice29.txt"; // 145 KB, copy of "ALICE'S ADVENTURES IN WONDERLAND" book, an example of text file
            yield return "sum"; // 37.3 KB, some binary content, an example of binary file
        }

        [Benchmark]
        public void Compress()
        {
            CompressedFile.CompressedDataStream.Position = 0; // all benchmarks invocation reuse the same stream, we set Postion to 0 to start at the beginning

            var compressor = CreateStream(CompressedFile.CompressedDataStream, level);
            compressor.Write(CompressedFile.UncompressedData, 0, CompressedFile.UncompressedData.Length);
        }

        [Benchmark]
        public int Decompress()
        {
            CompressedFile.CompressedDataStream.Position = 0;

            var compressor = CreateStream(CompressedFile.CompressedDataStream, CompressionMode.Decompress);
            return compressor.Read(CompressedFile.UncompressedData, 0, CompressedFile.UncompressedData.Length);
        }
    }
}
