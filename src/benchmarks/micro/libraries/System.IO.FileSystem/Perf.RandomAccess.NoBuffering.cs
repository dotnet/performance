// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    [OperatingSystemsFilter(allowed: true, platforms: OS.Windows)] // NO_BUFFERING is supported only on Windows: https://github.com/dotnet/runtime/issues/27408
    public class Perf_RandomAccess_NoBuffering
    {
        private const int OneKibibyte  = 1 << 10; // 1024
        private const int OneMibibyte = OneKibibyte  << 10;
        private const int HundredMibibytes = OneMibibyte * 100;
        private const FileOptions NoBuffering = (FileOptions)0x20000000;
        private const FileOptions AsyncNoBuffering  = FileOptions.Asynchronous | NoBuffering;

        static int PageSize = Environment.SystemPageSize;

        private Dictionary<long, string> _sourceFilePaths, _destinationFilePaths;
        private Dictionary<int, AlignedMemory[]> _countToAlignedMemory;
        private Dictionary<int, System.Memory<byte>[]> _countToMemory;
        private Dictionary<int, System.ReadOnlyMemory<byte>[]> _countToReadOnlyMemory;

        private void Setup(params long[] fileSizes)
        {
            _countToAlignedMemory = new Dictionary<int, AlignedMemory[]>()
            {
                { 4, Enumerable.Range(0, 4).Select(_ => AlignedMemory.Allocate((uint)PageSize, (uint)PageSize)).ToArray() },
                { 16, Enumerable.Range(0, 16).Select(_ => AlignedMemory.Allocate((uint)PageSize, (uint)PageSize)).ToArray() },
            };
            _countToMemory = _countToAlignedMemory.ToDictionary(pair => pair.Key, pair => pair.Value.Select(aligned => aligned.Memory).ToArray());
            _countToReadOnlyMemory = _countToAlignedMemory.ToDictionary(pair => pair.Key, pair => pair.Value.Select(aligned => (System.ReadOnlyMemory<byte>)aligned.Memory).ToArray());

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
            foreach (IDisposable alignedMemory in _countToAlignedMemory.Values.SelectMany(buffers => buffers))
            {
                alignedMemory.Dispose();
            }
        }

        [GlobalSetup(Targets = new[] { nameof(ReadScatterAsync), nameof(WriteGatherAsync) })]
        public void SetupBigFileBenchmarks() => Setup(OneMibibyte, HundredMibibytes);

        public IEnumerable<object[]> ReadWrite_MultipleBuffers_Arguments()
        {
            yield return new object[] { OneMibibyte, 4 }; // medium size file, 4xpage size user buffers
            yield return new object[] { OneMibibyte, 16 }; // medium size file, 16xpage size user buffers
            yield return new object[] { HundredMibibytes, 4 }; // big file, 4xpage size user buffers
            yield return new object[] { HundredMibibytes, 16 }; // big file, 16xpage size user buffers
        }

        [Benchmark]
        [ArgumentsSource(nameof(ReadWrite_MultipleBuffers_Arguments))]
        [MemoryRandomization]
        public async Task<long> ReadScatterAsync(long fileSize, int count)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            System.Memory<byte>[] buffers = _countToMemory[count];
            long bytesRead = 0;
            using (SafeFileHandle fileHandle = File.OpenHandle(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, AsyncNoBuffering))
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
        public async Task WriteGatherAsync(long fileSize, int count)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            System.ReadOnlyMemory<byte>[] buffers = _countToReadOnlyMemory[count];
            long buffersSize = count * PageSize;
            using (SafeFileHandle fileHandle = File.OpenHandle(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, AsyncNoBuffering))
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