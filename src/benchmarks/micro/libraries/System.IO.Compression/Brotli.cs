// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Compression
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Brotli : CompressionStreamPerfTestBase
    {
        private const int Window = 22;

        public override Stream CreateStream(Stream stream, CompressionMode mode) => new BrotliStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new BrotliStream(stream, level);

        [Benchmark]
        public Span<byte> Compress_WithState()
        {
            using (BrotliEncoder encoder = new BrotliEncoder(GetQuality(level), Window))
            {
                Span<byte> output = new Span<byte>(CompressedFile.CompressedData);
                ReadOnlySpan<byte> input = CompressedFile.UncompressedData;
                while (!input.IsEmpty && !output.IsEmpty)
                {
                    encoder.Compress(input, output, out int bytesConsumed, out int written, isFinalBlock:false);
                    input = input.Slice(bytesConsumed);
                    output = output.Slice(written);
                }
                encoder.Compress(input, output, out int bytesConsumed2, out int written2, isFinalBlock: true);

                return output;
            }
        }

        [Benchmark]
        public Span<byte> Decompress_WithState() // the level argument is not used here, but it describes how the data was compressed (in the benchmark id)
        {
            using (BrotliDecoder decoder = new BrotliDecoder())
            {
                Span<byte> output = new Span<byte>(CompressedFile.UncompressedData);
                ReadOnlySpan<byte> input = CompressedFile.CompressedData;
                while (!input.IsEmpty && !output.IsEmpty)
                {
                    decoder.Decompress(input, output, out int bytesConsumed, out int written);
                    input = input.Slice(bytesConsumed);
                    output = output.Slice(written);
                }

                return output;
            }
        }

        [Benchmark]
        [MemoryRandomization]
        public bool Compress_WithoutState()
            => BrotliEncoder.TryCompress(CompressedFile.UncompressedData, CompressedFile.CompressedData, out int bytesWritten, GetQuality(level), Window);

        /// <summary>
        /// The perf tests for the instant decompression aren't exactly indicative of real-world scenarios since they require you to know 
        /// either the exact figure or the upper bound of the uncompressed size of your given compressed data.
        /// </summary>
        [Benchmark]
        public bool Decompress_WithoutState() // the level argument is not used here, but it describes how the data was compressed (in the benchmark id)
            => BrotliDecoder.TryDecompress(CompressedFile.CompressedData, CompressedFile.UncompressedData, out int bytesWritten);
        
        private static int GetQuality(CompressionLevel compressLevel)
            => compressLevel == CompressionLevel.Optimal ? 11 : compressLevel == CompressionLevel.Fastest ? 1 : 0;
    }
}
