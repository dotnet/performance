// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace System.IO.Tests
{
    public class Perf_FileStream
    {
        private const int DefaultBufferSize = 4096;
        
        [Params(DefaultBufferSize / 8, 200000)]
        public int BufferSize;

        [Params(200000)]
        public int TotalSize;

        private byte[] _buffer;
        private string _filePath;

        [GlobalSetup]
        public void Setup()
        {
            _buffer = CreateRandomBytes(BufferSize);
            _filePath = CreateFileWithRandomContent(TotalSize);
        }

        [GlobalCleanup]
        public void Cleanup() => File.Delete(_filePath);

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
                for (int i = 0; i < TotalSize / BufferSize; i++)
                {
                    bytesRead += reader.Read(buffer, 0, buffer.Length);
                }
            }

            return bytesRead;
        }
        
        [Benchmark]
        public async Task<int> ReadAsync()
        {
            byte[] buffer = _buffer;
            int bytesRead = 0;
            
            using (FileStream reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous))
            {
                for (int i = 0; i < TotalSize / BufferSize; i++)
                {
                    bytesRead += await reader.ReadAsync(buffer, 0, buffer.Length);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        public async Task CopyToAsync()
        {
            using (var reader = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous))
            {
                await reader.CopyToAsync(Stream.Null);
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
