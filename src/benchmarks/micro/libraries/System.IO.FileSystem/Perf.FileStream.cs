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

        [GlobalSetup]
        public void Setup()
        {
            _userBuffers = new Dictionary<int, byte[]>()
            {
                { HalfKiloByte, ValuesGenerator.Array<byte>(HalfKiloByte) },
                { FourKiloBytes, ValuesGenerator.Array<byte>(HalfKiloByte) },
            };
            _sourceFilePaths = new Dictionary<long, string>()
            {
                {  OneKiloByte, CreateFileWithRandomContent(OneKiloByte) },
                {  OneMegaByte, CreateFileWithRandomContent(OneMegaByte) },
                {  HundredMegaBytes, CreateFileWithRandomContent(HundredMegaBytes) }
            };
            _destinationFilePaths = new Dictionary<long, string>()
            {
                {  OneKiloByte, CreateFileWithRandomContent(OneKiloByte) },
                {  OneMegaByte, CreateFileWithRandomContent(OneMegaByte) },
                {  HundredMegaBytes, CreateFileWithRandomContent(HundredMegaBytes) }
            };

            static string CreateFileWithRandomContent(int size)
            {
                string filePath = FileUtils.GetTestFilePath();
                File.WriteAllBytes(filePath, ValuesGenerator.Array<byte>(size));
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

        [Benchmark]
        public bool OpenClose()
        {
            string filePath = _sourceFilePaths[OneKiloByte]; // size does not matter in this benchmark
            using (FileStream reader = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, FileOptions.None))
            {
                return reader.IsAsync; // return something just to consume the reader
            }
        }

        [Benchmark]
        public bool OpenCloseAsync()
        {
            string filePath = _sourceFilePaths[OneKiloByte];
            using (FileStream reader = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, FileOptions.Asynchronous))
            {
                return reader.IsAsync;
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte)]
        [Arguments(OneMegaByte)] // calling ReadByte() on bigger files makes no sense, we so we don't have more test cases
        public int ReadByte(long fileSize)
        {
            int result = default;
            
            using (FileStream reader = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, FileOptions.None))
            {
                for (long i = 0; i < fileSize; i++)
                {
                    result += reader.ReadByte();
                }
            }

            return result;
        }
        
        [Benchmark]
        [Arguments(OneKiloByte, HalfKiloByte)] // userBufferSize is less than StreamBufferSize, buffering makes sense
        [Arguments(OneKiloByte, FourKiloBytes)] // the buffer provided by User and internal Stream buffer are of the same size, buffering makes NO sense
        [Arguments(OneMegaByte, HalfKiloByte)]
        [Arguments(OneMegaByte, FourKiloBytes)]
        [Arguments(HundredMegaBytes, HalfKiloByte)]
        [Arguments(HundredMegaBytes, FourKiloBytes)]
        public long Read(long fileSize, int userBufferSize)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            long bytesRead = 0;
            
            using (FileStream reader = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, FileOptions.None))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += reader.Read(userBuffer, 0, userBuffer.Length);
                }
            }

            return bytesRead;
        }
        
        [Benchmark]
        [Arguments(OneKiloByte, HalfKiloByte)] // userBufferSize is less than StreamBufferSize, buffering makes sense
        [Arguments(OneKiloByte, FourKiloBytes)] // the buffer provided by User and internal Stream buffer are of the same size, buffering makes NO sense
        [Arguments(OneMegaByte, HalfKiloByte)]
        [Arguments(OneMegaByte, FourKiloBytes)]
        [Arguments(HundredMegaBytes, HalfKiloByte)]
        [Arguments(HundredMegaBytes, FourKiloBytes)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task<long> ReadAsync(long fileSize, int userBufferSize)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            long bytesRead = 0;
            
            using (FileStream reader = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, FileOptions.Asynchronous))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += await reader.ReadAsync(userBuffer, 0, userBuffer.Length);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [Arguments(OneMegaByte, HalfKiloByte)] // only two test cases to compare the overhead of using CancellationToken
        [Arguments(OneMegaByte, FourKiloBytes)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task<long> ReadWithCancellationTokenAsync(long fileSize, int userBufferSize)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[] userBuffer = _userBuffers[userBufferSize];
            long bytesRead = 0;

            using (FileStream reader = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, FileOptions.Asynchronous))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += await reader.ReadAsync(userBuffer, 0, userBuffer.Length, cancellationToken);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [Arguments(OneKiloByte)]
        [Arguments(OneMegaByte)]
        [Arguments(HundredMegaBytes)]
        public void CopyToFile(long fileSize)
        {
            using (var reader = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, FileOptions.None))
            using (var writer = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, FileOptions.None))
            {
                reader.CopyTo(writer);
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte)]
        [Arguments(OneMegaByte)]
        [Arguments(HundredMegaBytes)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task CopyToFileAsync(long fileSize)
        {
            using (var reader = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, FourKiloBytes, FileOptions.Asynchronous))
            using (var writer = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, FileOptions.Asynchronous))
            {
                await reader.CopyToAsync(writer);
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte)]
        [Arguments(OneMegaByte)]
        public void WriteByte(long fileSize)
        {
            using (FileStream writer = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, FileOptions.None))
            {
                for (int i = 0; i < fileSize; i++)
                {
                    writer.WriteByte(default);
                }
            }
        }
        
        [Benchmark]
        [Arguments(OneKiloByte, HalfKiloByte)] // userBufferSize is less than StreamBufferSize, buffering makes sense
        [Arguments(OneKiloByte, FourKiloBytes)] // the buffer provided by User and internal Stream buffer are of the same size, buffering makes NO sense
        [Arguments(OneMegaByte, HalfKiloByte)]
        [Arguments(OneMegaByte, FourKiloBytes)]
        [Arguments(HundredMegaBytes, HalfKiloByte)]
        [Arguments(HundredMegaBytes, FourKiloBytes)]
        public void Write(long fileSize, int userBufferSize)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];

            using (FileStream writer = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, FileOptions.None))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    writer.Write(userBuffer, 0, userBuffer.Length);
                }
            }
        }

        [Benchmark]
        [Arguments(OneKiloByte, HalfKiloByte)] // userBufferSize is less than StreamBufferSize, buffering makes sense
        [Arguments(OneKiloByte, FourKiloBytes)] // the buffer provided by User and internal Stream buffer are of the same size, buffering makes NO sense
        [Arguments(OneMegaByte, HalfKiloByte)]
        [Arguments(OneMegaByte, FourKiloBytes)]
        [Arguments(HundredMegaBytes, HalfKiloByte)]
        [Arguments(HundredMegaBytes, FourKiloBytes)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task WriteAsync(long fileSize, int userBufferSize)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];

            using (FileStream writer = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, FileOptions.Asynchronous))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    await writer.WriteAsync(userBuffer, 0, userBuffer.Length);
                }
            }
        }

        [Benchmark]
        [Arguments(OneMegaByte, HalfKiloByte)] // only two test cases to compare the overhead of using CancellationToken
        [Arguments(OneMegaByte, FourKiloBytes)]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task WriteWithCancellationTokenAsync(long fileSize, int userBufferSize)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[] userBuffer = _userBuffers[userBufferSize];

            using (FileStream writer = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKiloBytes, FileOptions.Asynchronous))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    await writer.WriteAsync(userBuffer, 0, userBuffer.Length, cancellationToken);
                }
            }
        }
    }
}
