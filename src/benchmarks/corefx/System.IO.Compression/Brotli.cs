// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.IO.Compression
{
    public class Brotli : CompressionStreamPerfTestBase
    {
        private const int Window = 22;

        public override Stream CreateStream(Stream stream, CompressionMode mode) => new BrotliStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new BrotliStream(stream, level);

        [Benchmark]
        [ArgumentsSource(nameof(Arguments))]
        public Span<byte> Compress_WithState(CompressedFile file, CompressionLevel level)
        {
            using (BrotliEncoder encoder = new BrotliEncoder(GetQuality(level), Window))
            {
                Span<byte> output = new Span<byte>(file.UncompressedData);
                ReadOnlySpan<byte> input = file.CompressedData;
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
        [ArgumentsSource(nameof(Arguments))]
        public Span<byte> Decompress_WithState(CompressedFile file, CompressionLevel level) // the level argument is not used here, but it describes how the data was compressed
        {
            using (BrotliDecoder decoder = new BrotliDecoder())
            {
                Span<byte> output = new Span<byte>(file.UncompressedData);
                ReadOnlySpan<byte> input = file.CompressedData;
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
        [ArgumentsSource(nameof(Arguments))]
        public bool Compress_WithoutState(CompressedFile file, CompressionLevel level)
            => BrotliEncoder.TryCompress(file.UncompressedData, file.UncompressedData, out int bytesWritten, GetQuality(level), Window);

        /// <summary>
        /// The perf tests for the instant decompression aren't exactly indicative of real-world scenarios since they require you to know 
        /// either the exact figure or the upper bound of the uncompressed size of your given compressed data.
        /// </summary>
        [Benchmark]
        [ArgumentsSource(nameof(Arguments))]
        public bool Decompress_WithoutState(CompressedFile file, CompressionLevel level) // the level argument is not used here, but it describes how the data was compressed
            => BrotliDecoder.TryDecompress(file.CompressedData, file.UncompressedData, out int bytesWritten);
        
        private static int GetQuality(CompressionLevel compressLevel)
            => compressLevel == CompressionLevel.Optimal ? 11 : compressLevel == CompressionLevel.Fastest ? 1 : 0;
    }
}
