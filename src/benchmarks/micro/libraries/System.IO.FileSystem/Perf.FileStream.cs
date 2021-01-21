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

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_FileStream
    {
        private const int OneKibibyte  = 1 << 10; // 1024
        private const int HalfKibibyte = OneKibibyte >> 1;
        private const int FourKibibytes = OneKibibyte << 2; // default Stream buffer size
        private const int OneMibibyte = OneKibibyte  << 10;
        private const int HundredMibibytes = OneMibibyte * 100;

        private Dictionary<long, string> _sourceFilePaths, _destinationFilePaths;

        private Dictionary<int, byte[]> _userBuffers;

        private void Setup(params long[] fileSizes)
        {
            _userBuffers = new Dictionary<int, byte[]>()
            {
                { HalfKibibyte, ValuesGenerator.Array<byte>(HalfKibibyte) },
                { FourKibibytes, ValuesGenerator.Array<byte>(FourKibibytes) },
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

        [GlobalSetup(Targets = new[] { nameof(OpenClose), nameof(LockUnlock), nameof(SeekForward), nameof(SeekBackward), nameof(ReadByte), nameof(WriteByte) })]
        public void SetuOneKibibyteBenchmarks() => Setup(OneKibibyte );

        [Benchmark]
        [Arguments(OneKibibyte , FileOptions.None)] // sync (default)
        [Arguments(OneKibibyte , FileOptions.Asynchronous)] // async
        public bool OpenClose(long fileSize, FileOptions options)
        {
            string filePath = _sourceFilePaths[fileSize];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                return fileStream.IsAsync; // return something just to consume the reader
            }
        }

        [Benchmark]
        [Arguments(OneKibibyte , FileOptions.None)]
        [Arguments(OneKibibyte , FileOptions.Asynchronous)]
        public void LockUnlock(long fileSize, FileOptions options)
        {
            string filePath = _sourceFilePaths[fileSize];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                fileStream.Lock(0, fileStream.Length);

                fileStream.Unlock(0, fileStream.Length);
            }
        }

        [Benchmark]
        [Arguments(OneKibibyte , FileOptions.None)]
        [Arguments(OneKibibyte , FileOptions.Asynchronous)]
        public void SeekForward(long fileSize, FileOptions options)
        {
            string filePath = _sourceFilePaths[fileSize];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                for (long offset = 0; offset < fileSize; offset++)
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                }
            }
        }

        [Benchmark]
        [Arguments(OneKibibyte , FileOptions.None)]
        [Arguments(OneKibibyte , FileOptions.Asynchronous)]
        public void SeekBackward(long fileSize, FileOptions options)
        {
            string filePath = _sourceFilePaths[fileSize];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                for (long offset = -1; offset >= -fileSize; offset--)
                {
                    fileStream.Seek(offset, SeekOrigin.End);
                }
            }
        }

        [Benchmark]
        [Arguments(OneKibibyte , FileOptions.None)]
        [Arguments(OneKibibyte , FileOptions.Asynchronous)] // calling ReadByte() on bigger files makes no sense, so we don't have more test cases
        public int ReadByte(long fileSize, FileOptions options)
        {
            int result = default;
            using (FileStream fileStream = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                for (long i = 0; i < fileSize; i++)
                {
                    result += fileStream.ReadByte();
                }
            }

            return result;
        }

        [Benchmark]
        [Arguments(OneKibibyte , FileOptions.None)]
        [Arguments(OneKibibyte , FileOptions.Asynchronous)]
        public void WriteByte(long fileSize, FileOptions options)
        {
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKibibytes, options))
            {
                for (int i = 0; i < fileSize; i++)
                {
                    fileStream.WriteByte(default);
                }
            }
        }

        [GlobalSetup(Targets = new[] { nameof(Read), nameof(ReadAsync), nameof(CopyToFile), nameof(CopyToFileAsync), nameof(Write), nameof(WriteAsync) })]
        public void SetupBigFileBenchmarks() => Setup(OneKibibyte , OneMibibyte, HundredMibibytes);

        [Benchmark]
        [Arguments(OneKibibyte , HalfKibibyte, FileOptions.None)] // userBufferSize is less than StreamBufferSize, buffering makes sense
        [Arguments(OneKibibyte , FourKibibytes, FileOptions.None)] // the buffer provided by User and internal Stream buffer are of the same size, buffering makes NO sense
        [Arguments(OneMibibyte, HalfKibibyte, FileOptions.None)]
        [Arguments(OneMibibyte, FourKibibytes, FileOptions.None)]
        [Arguments(HundredMibibytes, HalfKibibyte, FileOptions.None)]
        [Arguments(HundredMibibytes, FourKibibytes, FileOptions.None)]
        public long Read(long fileSize, int userBufferSize, FileOptions options)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            long bytesRead = 0;
            using (FileStream fileStream = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += fileStream.Read(userBuffer, 0, userBuffer.Length);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [Arguments(OneKibibyte , HalfKibibyte, FileOptions.None)]
        [Arguments(OneKibibyte , FourKibibytes, FileOptions.None)]
        [Arguments(OneMibibyte, HalfKibibyte, FileOptions.None)]
        [Arguments(OneMibibyte, FourKibibytes, FileOptions.None)]
        [Arguments(HundredMibibytes, HalfKibibyte, FileOptions.None)]
        [Arguments(HundredMibibytes, FourKibibytes, FileOptions.None)]
        public void Write(long fileSize, int userBufferSize, FileOptions options)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKibibytes, options))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    fileStream.Write(userBuffer, 0, userBuffer.Length);
                }
            }
        }

        [Benchmark]
        [Arguments(OneKibibyte , HalfKibibyte, FileOptions.Asynchronous)]
        [Arguments(OneKibibyte , FourKibibytes, FileOptions.Asynchronous)]
        [Arguments(OneMibibyte, HalfKibibyte, FileOptions.Asynchronous)]
        [Arguments(OneMibibyte, FourKibibytes, FileOptions.Asynchronous)]
        [Arguments(HundredMibibytes, HalfKibibyte, FileOptions.Asynchronous)]
        [Arguments(HundredMibibytes, FourKibibytes, FileOptions.Asynchronous)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task<long> ReadAsync(long fileSize, int userBufferSize, FileOptions options)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            long bytesRead = 0;
            using (FileStream fileStream = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += await fileStream.ReadAsync(userBuffer, 0, userBuffer.Length);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [Arguments(OneKibibyte , HalfKibibyte, FileOptions.Asynchronous)]
        [Arguments(OneKibibyte , FourKibibytes, FileOptions.Asynchronous)]
        [Arguments(OneMibibyte, HalfKibibyte, FileOptions.Asynchronous)]
        [Arguments(OneMibibyte, FourKibibytes, FileOptions.Asynchronous)]
        [Arguments(HundredMibibytes, HalfKibibyte, FileOptions.Asynchronous)]
        [Arguments(HundredMibibytes, FourKibibytes, FileOptions.Asynchronous)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task WriteAsync(long fileSize, int userBufferSize, FileOptions options)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKibibytes, options))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    await fileStream.WriteAsync(userBuffer, 0, userBuffer.Length);
                }
            }
        }

        [Benchmark]
        [Arguments(OneKibibyte , FileOptions.None)]
        [Arguments(OneMibibyte, FileOptions.None)]
        [Arguments(HundredMibibytes, FileOptions.None)]
        public void CopyToFile(long fileSize, FileOptions options)
        {
            using (FileStream source = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            using (FileStream destination = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKibibytes, options))
            {
                source.CopyTo(destination);
            }
        }

        [Benchmark]
        [Arguments(OneKibibyte , FileOptions.None)]
        [Arguments(OneMibibyte, FileOptions.None)]
        [Arguments(HundredMibibytes, FileOptions.None)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task CopyToFileAsync(long fileSize, FileOptions options)
        {
            using (FileStream source = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            using (FileStream destination = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKibibytes, options))
            {
                await source.CopyToAsync(destination);
            }
        }

        [GlobalSetup(Targets = new[] { nameof(ReadWithCancellationTokenAsync), nameof(WriteWithCancellationTokenAsync) })]
        public void SetupCancellationTokenBenchmarks() => Setup(OneMibibyte);

        [Benchmark]
        [Arguments(OneMibibyte, HalfKibibyte, FileOptions.Asynchronous)] // only two test cases to compare the overhead of using CancellationToken
        [Arguments(OneMibibyte, FourKibibytes, FileOptions.Asynchronous)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task<long> ReadWithCancellationTokenAsync(long fileSize, int userBufferSize, FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[] userBuffer = _userBuffers[userBufferSize];
            long bytesRead = 0;

            using (FileStream fileStream = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += await fileStream.ReadAsync(userBuffer, 0, userBuffer.Length, cancellationToken);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [Arguments(OneMibibyte, HalfKibibyte, FileOptions.Asynchronous)]
        [Arguments(OneMibibyte, FourKibibytes, FileOptions.Asynchronous)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task WriteWithCancellationTokenAsync(long fileSize, int userBufferSize, FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[] userBuffer = _userBuffers[userBufferSize];
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKibibytes, options))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    await fileStream.WriteAsync(userBuffer, 0, userBuffer.Length, cancellationToken);
                }
            }
        }
    }
}
