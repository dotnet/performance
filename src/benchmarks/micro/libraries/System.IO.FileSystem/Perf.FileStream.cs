// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_FileStream
    {
        private const int DefaultBufferSize = 4096;
        
        [Params(DefaultBufferSize / 8, 200000)]
        public int BufferSize;

        [Params(200000)]
        public int TotalSize;

        private byte[] _buffer;
        private string _filePath, _filePath2;

        [GlobalSetup]
        public void Setup()
        {
            _buffer = CreateRandomBytes(BufferSize);
            _filePath = CreateFileWithRandomContent(TotalSize);
            _filePath2 = CreateFileWithRandomContent(TotalSize);
        }

        [GlobalCleanup]
        public void Cleanup() => File.Delete(_filePath);

        [Benchmark]
        public bool OpenClose()
        {
            using (FileStream reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.None))
            {
                return reader.IsAsync; // return something just to consume the reader
            }
        }

        [Benchmark]
        public bool OpenCloseAsync()
        {
            using (FileStream reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous))
            {
                return reader.IsAsync;
            }
        }

        [Benchmark]
        public int ReadByte()
        {
            int result = default;
            
            using (FileStream reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.None))
            {
                for (int i = 0; i < TotalSize; i++)
                {
                    result += reader.ReadByte();
                }
            }

            return result;
        }
        
        [Benchmark]
        public int Read()
        {
            byte[] buffer = _buffer;
            int bytesRead = 0;
            
            using (FileStream reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.None))
            {
                while (bytesRead < TotalSize)
                {
                    bytesRead += reader.Read(buffer, 0, buffer.Length);
                }
            }

            return bytesRead;
        }
        
        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task<int> ReadAsync()
        {
            byte[] buffer = _buffer;
            int bytesRead = 0;
            
            using (FileStream reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous))
            {
                while (bytesRead < TotalSize)
                {
                    bytesRead += await reader.ReadAsync(buffer, 0, buffer.Length);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task<int> ReadWithCancellationTokenAsync()
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[] buffer = _buffer;
            int bytesRead = 0;

            using (FileStream reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous))
            {
                while (bytesRead < TotalSize)
                {
                    bytesRead += await reader.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        public void CopyToFile()
        {
            using (var reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.None))
            using (var writer = new FileStream(_filePath2, FileMode.Create, FileAccess.Write, FileShare.Read, DefaultBufferSize, FileOptions.None))
            {
                reader.CopyTo(writer);
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task CopyToAsync()
        {
            using (var reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous))
            {
                await reader.CopyToAsync(Stream.Null);
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task CopyToFileAsync()
        {
            using (var reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous))
            using (var writer = new FileStream(_filePath2, FileMode.Create, FileAccess.Write, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous))
            {
                await reader.CopyToAsync(writer);
            }
        }

        [Benchmark]
        public void WriteByte()
        {
            using (FileStream writer = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read, DefaultBufferSize, FileOptions.None))
            {
                for (int i = 0; i < TotalSize; i++)
                {
                    writer.WriteByte(default);
                }
            }
        }
        
        [Benchmark]
        public void Write()
        {
            byte[] bytes = _buffer;

            using (FileStream writer = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read, DefaultBufferSize, FileOptions.None))
            {
                for (int i = 0; i < TotalSize / BufferSize; i++)
                {
                    writer.Write(bytes, 0, bytes.Length);
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task WriteAsync()
        {
            byte[] bytes = _buffer;
            
            using (FileStream writer = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous))
            {
                for (int i = 0; i < TotalSize / BufferSize; i++)
                {
                    await writer.WriteAsync(bytes, 0, bytes.Length);
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task WriteWithCancellationTokenAsync()
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[] bytes = _buffer;

            using (FileStream writer = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous))
            {
                for (int i = 0; i < TotalSize / BufferSize; i++)
                {
                    await writer.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                }
            }
        }

        private static byte[] CreateRandomBytes(int size)
        {
            byte[] bytes = new byte[size];
            new Random(531033).NextBytes(bytes);
            return bytes;
        }

        private static string CreateFileWithRandomContent(int size)
        {
            string filePath = FileUtils.GetTestFilePath();
            byte[] bytes = new byte[size];
            new Random(531033).NextBytes(bytes);
            File.WriteAllBytes(filePath, bytes);
            return filePath;
        }
    }
}
