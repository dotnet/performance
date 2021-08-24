// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_RandomAccess
    {
        private const int OneKibibyte  = 1 << 10; // 1024
        private const int FourKibibytes = OneKibibyte << 2; // default Stream buffer size
        private const int SixteenKibibytes = FourKibibytes << 2; // default Stream buffer size * 4
        private const int SixtyFourKibibytes = SixteenKibibytes << 2; // default Stream buffer size * 16
        private const int OneMibibyte = OneKibibyte  << 10;
        private const int HundredMibibytes = OneMibibyte * 100;

        private Dictionary<long, string> _sourceFilePaths, _destinationFilePaths;
        private Dictionary<int, byte[]> _sizeToBuffer;
        private Dictionary<int, byte[][]> _sizeToBuffers;

        private void Setup(params long[] fileSizes)
        {
            _sizeToBuffer = new Dictionary<int, byte[]>()
            {
                { FourKibibytes, ValuesGenerator.Array<byte>(FourKibibytes) },
                { SixteenKibibytes, ValuesGenerator.Array<byte>(SixteenKibibytes) },
            };
            _sizeToBuffers = new Dictionary<int, byte[][]>()
            {
                { SixteenKibibytes, Enumerable.Range(0, 4).Select(_ => ValuesGenerator.Array<byte>(FourKibibytes)).ToArray() },
                { SixtyFourKibibytes, Enumerable.Range(0, 4).Select(_ => ValuesGenerator.Array<byte>(SixteenKibibytes)).ToArray() },
            };
            _sourceFilePaths = fileSizes.ToDictionary(size => size, size => CreateFileWithRandomContent(size));
            _destinationFilePaths = fileSizes.ToDictionary(size => size, size => CreateFileWithRandomContent(size));

            static string CreateFileWithRandomContent(long fileSize)
            {
                string filePath = FileUtils.GetTestFilePath();
                File.WriteAllBytes(filePath, ValuesGenerator.Array<byte>((int)fileSize));
                return filePath;
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            foreach (string filePath in _sourceFilePaths.Values.Concat(_destinationFilePaths.Values))
            {
                File.Delete(filePath);
            }
        }

        [GlobalSetup(Targets = new[] { nameof(Read), nameof(ReadScatter), nameof(ReadAsync), nameof(ReadScatterAsync),
            nameof(Write), nameof(WriteGather), nameof(WriteAsync), nameof(WriteGatherAsync) })]
        public void SetupBigFileBenchmarks() => Setup(OneMibibyte, HundredMibibytes);
        
        public IEnumerable<object[]> ReadWrite_SingleBuffer_Arguments()
        {
            // long fileSize, int bufferSize, FileOptions options
            foreach (FileOptions options in new FileOptions[] { FileOptions.None, FileOptions.Asynchronous })
            {
                yield return new object[] { OneMibibyte, FourKibibytes, options }; // medium size file, user buffer size == default stream buffer size
                yield return new object[] { HundredMibibytes, SixteenKibibytes, options }; // big file, user buffer size == 4 * default stream buffer size
            }
        }
        
        [Benchmark]
        [ArgumentsSource(nameof(ReadWrite_SingleBuffer_Arguments))]
        public long Read(long fileSize, int bufferSize, FileOptions options)
        {
            byte[] userBuffer = _sizeToBuffer[bufferSize];
            long bytesRead = 0;

            using (SafeFileHandle fileHandle = File.OpenHandle(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += RandomAccess.Read(fileHandle, userBuffer, bytesRead);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [ArgumentsSource(nameof(ReadWrite_SingleBuffer_Arguments))]
        public void Write(long fileSize, int bufferSize, FileOptions options)
        {
            byte[] userBuffer = _sizeToBuffer[bufferSize];
            using (SafeFileHandle fileHandle = File.OpenHandle(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, options))
            {
                long bytesWritten = 0;
                for (int i = 0; i < fileSize / bufferSize; i++)
                {
                    RandomAccess.Write(fileHandle, userBuffer, bytesWritten);
                    bytesWritten += userBuffer.Length;
                }
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(ReadWrite_SingleBuffer_Arguments))]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task<long> ReadAsync(long fileSize, int bufferSize, FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            Memory<byte> userBuffer = new Memory<byte>(_sizeToBuffer[bufferSize]);
            long bytesRead = 0;
            using (SafeFileHandle fileHandle = File.OpenHandle(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += await RandomAccess.ReadAsync(fileHandle, userBuffer, bytesRead, cancellationToken);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [ArgumentsSource(nameof(ReadWrite_SingleBuffer_Arguments))]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task WriteAsync(long fileSize, int bufferSize, FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            Memory<byte> userBuffer = new Memory<byte>(_sizeToBuffer[bufferSize]);
            using (SafeFileHandle fileHandle = File.OpenHandle(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, options))
            {
                long bytesWritten = 0;
                for (int i = 0; i < fileSize / bufferSize; i++)
                {
                    await RandomAccess.WriteAsync(fileHandle, userBuffer, bytesWritten, cancellationToken);
                    bytesWritten += userBuffer.Length;
                }
            }
        }

        public IEnumerable<object[]> ReadWrite_MultipleBuffers_Arguments()
        {
            // long fileSize, int buffersSize, FileOptions options
            foreach (FileOptions options in new FileOptions[] { FileOptions.None, FileOptions.Asynchronous })
            {
                yield return new object[] { OneMibibyte, SixteenKibibytes, options }; // medium size file, 4x4Mib user buffers
                yield return new object[] { HundredMibibytes, SixtyFourKibibytes, options }; // big file, 4x16Mib user buffers
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(ReadWrite_MultipleBuffers_Arguments))]
        public long ReadScatter(long fileSize, int buffersSize, FileOptions options)
        {
            byte[][] b = _sizeToBuffers[buffersSize];
            IReadOnlyList<Memory<byte>> buffers = new Memory<byte>[] { b[0], b[1], b[2], b[3], };
            long bytesRead = 0;

            using (SafeFileHandle fileHandle = File.OpenHandle(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += RandomAccess.Read(fileHandle, buffers, bytesRead);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [ArgumentsSource(nameof(ReadWrite_MultipleBuffers_Arguments))]
        public void WriteGather(long fileSize, int buffersSize, FileOptions options)
        {
            byte[][] b = _sizeToBuffers[buffersSize];
            IReadOnlyList<ReadOnlyMemory<byte>> buffers = new ReadOnlyMemory<byte>[] { b[0], b[1], b[2], b[3], };
            using (SafeFileHandle fileHandle = File.OpenHandle(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, options))
            {
                long bytesWritten = 0;
                for (int i = 0; i < fileSize / buffersSize; i++)
                {
                    RandomAccess.Write(fileHandle, buffers, bytesWritten);
                    bytesWritten += buffersSize;
                }
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(ReadWrite_MultipleBuffers_Arguments))]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task<long> ReadScatterAsync(long fileSize, int buffersSize, FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[][] b = _sizeToBuffers[buffersSize];
            IReadOnlyList<Memory<byte>> buffers = new Memory<byte>[] { b[0], b[1], b[2], b[3], };
            long bytesRead = 0;
            using (SafeFileHandle fileHandle = File.OpenHandle(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += await RandomAccess.ReadAsync(fileHandle, buffers, bytesRead, cancellationToken);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [ArgumentsSource(nameof(ReadWrite_MultipleBuffers_Arguments))]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task WriteGatherAsync(long fileSize, int buffersSize, FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[][] b = _sizeToBuffers[buffersSize];
            IReadOnlyList<ReadOnlyMemory<byte>> buffers = new ReadOnlyMemory<byte>[] { b[0], b[1], b[2], b[3], };
            using (SafeFileHandle fileHandle = File.OpenHandle(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, options))
            {
                long bytesWritten = 0;
                for (int i = 0; i < fileSize / buffersSize; i++)
                {
                    await RandomAccess.WriteAsync(fileHandle, buffers, bytesWritten, cancellationToken);
                    bytesWritten += buffersSize;
                }
            }
        }
    }
}