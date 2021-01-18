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
        private const int OneKiloByte = 1_000;
        private const int HalfKiloByte = OneKiloByte / 2;
        private const int FourKiloBytes = OneKiloByte * 4; // default Stream buffer size
        private const int OneMegaByte = OneKiloByte * 1_000;
        private const int HundredMegaBytes = 1_000_000 * 100;

        private Dictionary<long, string> _sourceFilePaths, _destinationFilePaths;

        private Dictionary<int, byte[]> _userBuffers;

        private void Setup(params long[] fileSizes)
        {
            _userBuffers = new Dictionary<int, byte[]>()
            {
                { HalfKiloByte, ValuesGenerator.Array<byte>(HalfKiloByte) },
                { FourKiloBytes, ValuesGenerator.Array<byte>(FourKiloBytes) },
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
        public void SetuOneKiloByteBenchmarks() => Setup(OneKiloByte);

        [Benchmark]
        [Arguments(OneKiloByte, FileOptions.None)] // sync (default)
        [Arguments(OneKiloByte, FileOptions.Asynchronous)] // async
        public bool OpenClose(long fileSize, FileOptions options)
        {
            string filePath = _sourceFilePaths[fileSize];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, options))
            {
                return fileStream.IsAsync; // return something just to consume the reader
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte, FileOptions.None)]
        [Arguments(OneKiloByte, FileOptions.Asynchronous)]
        public void LockUnlock(long fileSize, FileOptions options)
        {
            string filePath = _sourceFilePaths[fileSize];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, options))
            {
                fileStream.Lock(0, fileStream.Length);

                fileStream.Unlock(0, fileStream.Length);
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte, FileOptions.None)]
        [Arguments(OneKiloByte, FileOptions.Asynchronous)]
        public void SeekForward(long fileSize, FileOptions options)
        {
            string filePath = _sourceFilePaths[fileSize];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, options))
            {
                for (long offset = 0; offset < fileSize; offset++)
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                }
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte, FileOptions.None)]
        [Arguments(OneKiloByte, FileOptions.Asynchronous)]
        public void SeekBackward(long fileSize, FileOptions options)
        {
            string filePath = _sourceFilePaths[fileSize];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, options))
            {
                for (long offset = -1; offset >= -fileSize; offset--)
                {
                    fileStream.Seek(offset, SeekOrigin.End);
                }
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte, FileOptions.None)]
        [Arguments(OneKiloByte, FileOptions.Asynchronous)] // calling ReadByte() on bigger files makes no sense, we so we don't have more test cases
        public int ReadByte(long fileSize, FileOptions options)
        {
            int result = default;
            using (FileStream fileStream = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, options))
            {
                for (long i = 0; i < fileSize; i++)
                {
                    result += fileStream.ReadByte();
                }
            }

            return result;
        }

        [Benchmark]
        [Arguments(OneKiloByte, FileOptions.None)]
        [Arguments(OneKiloByte, FileOptions.Asynchronous)]
        public void WriteByte(long fileSize, FileOptions options)
        {
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, options))
            {
                for (int i = 0; i < fileSize; i++)
                {
                    fileStream.WriteByte(default);
                }
            }
        }

        [GlobalSetup(Targets = new[] { nameof(Read), nameof(ReadAsync), nameof(CopyToFile), nameof(CopyToFileAsync), nameof(Write), nameof(WriteAsync) })]
        public void SetupBigFileBenchmarks() => Setup(OneKiloByte, OneMegaByte, HundredMegaBytes);

        [Benchmark]
        [Arguments(OneKiloByte, HalfKiloByte, FileOptions.None)] // userBufferSize is less than StreamBufferSize, buffering makes sense
        [Arguments(OneKiloByte, FourKiloBytes, FileOptions.None)] // the buffer provided by User and internal Stream buffer are of the same size, buffering makes NO sense
        [Arguments(OneMegaByte, HalfKiloByte, FileOptions.None)]
        [Arguments(OneMegaByte, FourKiloBytes, FileOptions.None)]
        [Arguments(HundredMegaBytes, HalfKiloByte, FileOptions.None)]
        [Arguments(HundredMegaBytes, FourKiloBytes, FileOptions.None)]
        public long Read(long fileSize, int userBufferSize, FileOptions options)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            long bytesRead = 0;
            using (FileStream fileStream = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += fileStream.Read(userBuffer, 0, userBuffer.Length);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [Arguments(OneKiloByte, HalfKiloByte, FileOptions.None)]
        [Arguments(OneKiloByte, FourKiloBytes, FileOptions.None)]
        [Arguments(OneMegaByte, HalfKiloByte, FileOptions.None)]
        [Arguments(OneMegaByte, FourKiloBytes, FileOptions.None)]
        [Arguments(HundredMegaBytes, HalfKiloByte, FileOptions.None)]
        [Arguments(HundredMegaBytes, FourKiloBytes, FileOptions.None)]
        public void Write(long fileSize, int userBufferSize, FileOptions options)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, options))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    fileStream.Write(userBuffer, 0, userBuffer.Length);
                }
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte, HalfKiloByte, FileOptions.Asynchronous)]
        [Arguments(OneKiloByte, FourKiloBytes, FileOptions.Asynchronous)]
        [Arguments(OneMegaByte, HalfKiloByte, FileOptions.Asynchronous)]
        [Arguments(OneMegaByte, FourKiloBytes, FileOptions.Asynchronous)]
        [Arguments(HundredMegaBytes, HalfKiloByte, FileOptions.Asynchronous)]
        [Arguments(HundredMegaBytes, FourKiloBytes, FileOptions.Asynchronous)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task<long> ReadAsync(long fileSize, int userBufferSize, FileOptions options)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            long bytesRead = 0;
            using (FileStream fileStream = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += await fileStream.ReadAsync(userBuffer, 0, userBuffer.Length);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [Arguments(OneKiloByte, HalfKiloByte, FileOptions.Asynchronous)]
        [Arguments(OneKiloByte, FourKiloBytes, FileOptions.Asynchronous)]
        [Arguments(OneMegaByte, HalfKiloByte, FileOptions.Asynchronous)]
        [Arguments(OneMegaByte, FourKiloBytes, FileOptions.Asynchronous)]
        [Arguments(HundredMegaBytes, HalfKiloByte, FileOptions.Asynchronous)]
        [Arguments(HundredMegaBytes, FourKiloBytes, FileOptions.Asynchronous)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task WriteAsync(long fileSize, int userBufferSize, FileOptions options)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, options))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    await fileStream.WriteAsync(userBuffer, 0, userBuffer.Length);
                }
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte, FileOptions.None)]
        [Arguments(OneMegaByte, FileOptions.None)]
        [Arguments(HundredMegaBytes, FileOptions.None)]
        public void CopyToFile(long fileSize, FileOptions options)
        {
            using (FileStream source = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, options))
            using (FileStream destination = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, options))
            {
                source.CopyTo(destination);
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte, FileOptions.None)]
        [Arguments(OneMegaByte, FileOptions.None)]
        [Arguments(HundredMegaBytes, FileOptions.None)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task CopyToFileAsync(long fileSize, FileOptions options)
        {
            using (FileStream source = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, options))
            using (FileStream destination = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, options))
            {
                await source.CopyToAsync(destination);
            }
        }

        [GlobalSetup(Targets = new[] { nameof(ReadWithCancellationTokenAsync), nameof(WriteWithCancellationTokenAsync) })]
        public void SetupCancellationTokenBenchmarks() => Setup(OneMegaByte);

        [Benchmark]
        [Arguments(OneMegaByte, HalfKiloByte, FileOptions.Asynchronous)] // only two test cases to compare the overhead of using CancellationToken
        [Arguments(OneMegaByte, FourKiloBytes, FileOptions.Asynchronous)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task<long> ReadWithCancellationTokenAsync(long fileSize, int userBufferSize, FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[] userBuffer = _userBuffers[userBufferSize];
            long bytesRead = 0;

            using (FileStream fileStream = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += await fileStream.ReadAsync(userBuffer, 0, userBuffer.Length, cancellationToken);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [Arguments(OneMegaByte, HalfKiloByte, FileOptions.Asynchronous)]
        [Arguments(OneMegaByte, FourKiloBytes, FileOptions.Asynchronous)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task WriteWithCancellationTokenAsync(long fileSize, int userBufferSize, FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[] userBuffer = _userBuffers[userBufferSize];
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, options))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    await fileStream.WriteAsync(userBuffer, 0, userBuffer.Length, cancellationToken);
                }
            }
        }
    }
}
