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
        private const int SixteenKibibytes = FourKibibytes << 2; // default Stream buffer size * 4
        private const int OneMibibyte = OneKibibyte  << 10;
        private const int HundredMibibytes = OneMibibyte * 100;

        private Dictionary<long, string> _sourceFilePaths, _destinationFilePaths;
        private Dictionary<int, byte[]> _userBuffers;

        private void Setup(params long[] fileSizes)
        {
            _userBuffers = new Dictionary<int, byte[]>()
            {
                { HalfKibibyte, ValuesGenerator.Array<byte>(HalfKibibyte) },
                { OneKibibyte, ValuesGenerator.Array<byte>(OneKibibyte) },
                { FourKibibytes, ValuesGenerator.Array<byte>(FourKibibytes) },
                { SixteenKibibytes, ValuesGenerator.Array<byte>(SixteenKibibytes) },
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

        [GlobalSetup(Targets = new[] { nameof(OpenClose), nameof(LockUnlock), nameof(SeekForward), nameof(SeekBackward), 
            nameof(GetLength), nameof(ReadByte), nameof(WriteByte), nameof(Flush), nameof(FlushAsync) })]
        public void SetuOneKibibyteBenchmarks() => Setup(OneKibibyte );

        [Benchmark]
        [Arguments(OneKibibyte, FileOptions.None)] // sync (default)
        [Arguments(OneKibibyte, FileOptions.Asynchronous)] // async
        public bool OpenClose(long fileSize, FileOptions options)
        {
            string filePath = _sourceFilePaths[fileSize];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                return fileStream.IsAsync; // return something just to consume the stream
            }
        }

        [Benchmark]
        [Arguments(OneKibibyte , FileOptions.None)]
        [Arguments(OneKibibyte , FileOptions.Asynchronous)]
        [OperatingSystemsFilter(allowed: true, OS.Windows, OS.Linux)] // "Lock and Unlock are supported only on Windows and Linux"
#if NET6_0_OR_GREATER // the method was marked as unsupported on macOS in .NET 6.0
        [System.Runtime.Versioning.UnsupportedOSPlatform("macos")]
#endif
        public void LockUnlock(long fileSize, FileOptions options)
        {
            string filePath = _sourceFilePaths[fileSize];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, FourKibibytes, options))
            {
                fileStream.Lock(0, fileStream.Length);

                fileStream.Unlock(0, fileStream.Length);
            }
        }

        [Benchmark]
        [Arguments(OneKibibyte, FileOptions.None)]
        [Arguments(OneKibibyte, FileOptions.Asynchronous)]
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
        [Arguments(OneKibibyte, FileOptions.None)]
        [Arguments(OneKibibyte, FileOptions.Asynchronous)]
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

        [Benchmark(OperationsPerInvoke = OneKibibyte)]
        [Arguments(FileShare.Read)]
        [Arguments(FileShare.Write)]
        public long GetLength(FileShare options) // since other benchmarks use "options" name, we use it as well to avoid introducing new column in the results
        {
            string filePath = _sourceFilePaths[OneKibibyte];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, options, FourKibibytes, FileOptions.None))
            {
                long length = 0;
                for (long i = 0; i < OneKibibyte; i++)
                {
                    length = fileStream.Length;
                }
                return length;
            }
        }

        [MemoryRandomization(true)]
        [IterationCount(50)]
        [Outliers(Perfolizer.Mathematics.OutlierDetection.OutlierMode.DontRemove)]
        [Benchmark]
        [Arguments(OneKibibyte, FileOptions.None)]
        [Arguments(OneKibibyte, FileOptions.Asynchronous)] // calling ReadByte() on bigger files makes no sense, so we don't have more test cases
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
        [Arguments(OneKibibyte, FileOptions.None)]
        [Arguments(OneKibibyte, FileOptions.Asynchronous)]
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

        [GlobalSetup(Targets = new[] { nameof(Read), nameof(Read_NoBuffering), "ReadAsync", "ReadAsync_NoBuffering", 
            nameof(Write), nameof(Write_NoBuffering), "WriteAsync", "WriteAsync_NoBuffering", nameof(CopyToFile), nameof(CopyToFileAsync), nameof(Append), "AppendAsync" })]
        public void SetupBigFileBenchmarks() => Setup(OneKibibyte, OneMibibyte, HundredMibibytes);
        
        public IEnumerable<object[]> SyncArguments()
        {
            // long fileSize, int userBufferSize, FileOptions options
            foreach (FileOptions options in new FileOptions[] { FileOptions.None, FileOptions.Asynchronous })
            {
                yield return new object[] { OneKibibyte, OneKibibyte, options }; // small file size, user buffer size == file size
                yield return new object[] { OneMibibyte, HalfKibibyte, options }; // medium size file, user buffer size * 8 == default stream buffer size (buffering is beneficial)
                yield return new object[] { OneMibibyte, FourKibibytes, options }; // medium size file, user buffer size == default stream buffer size (buffering is not beneficial)
                yield return new object[] { HundredMibibytes, FourKibibytes, options }; // big file, user buffer size == default stream buffer size (buffering is not beneficial)
            }
        }
        
        public IEnumerable<object[]> SyncArguments_NoBuffering()
        {
            // long fileSize, int userBufferSize, FileOptions options
            foreach (FileOptions options in new FileOptions[] { FileOptions.None, FileOptions.Asynchronous })
            {
                yield return new object[] { OneMibibyte, SixteenKibibytes, options }; // medium size file, user buffer size == 4 * default stream buffer size
                yield return new object[] { HundredMibibytes, SixteenKibibytes, options }; // big file, user buffer size == 4 * default stream buffer size
            }
        }
        
        [Benchmark]
        [ArgumentsSource(nameof(SyncArguments))]
        public long Read(long fileSize, int userBufferSize, FileOptions options)
            => Read(fileSize, userBufferSize, options, streamBufferSize: FourKibibytes);

        [Benchmark]
        [ArgumentsSource(nameof(SyncArguments_NoBuffering))]
        public long Read_NoBuffering(long fileSize, int userBufferSize, FileOptions options)
            => Read(fileSize, userBufferSize, options, streamBufferSize: 1);

        private long Read(long fileSize, int userBufferSize, FileOptions options, int streamBufferSize)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            long bytesRead = 0;
            using (FileStream fileStream = new FileStream(
                _sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, streamBufferSize, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += fileStream.Read(userBuffer, 0, userBuffer.Length);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [ArgumentsSource(nameof(SyncArguments))]
        public void Write(long fileSize, int userBufferSize, FileOptions options)
            => Write(FileMode.Create, fileSize, userBufferSize, options, streamBufferSize: FourKibibytes);

        [Benchmark]
        [ArgumentsSource(nameof(SyncArguments_NoBuffering))]
        public void Write_NoBuffering(long fileSize, int userBufferSize, FileOptions options)
            => Write(FileMode.Create, fileSize, userBufferSize, options, streamBufferSize: 1);

        [Benchmark]
        [Arguments(OneMibibyte, FourKibibytes, FileOptions.DeleteOnClose)]
        public void Append(long fileSize, int userBufferSize, FileOptions options)
            => Write(FileMode.Append, fileSize, userBufferSize, options, streamBufferSize: FourKibibytes);

        private void Write(FileMode mode, long fileSize, int userBufferSize, FileOptions options, int streamBufferSize)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], mode, FileAccess.Write, FileShare.Read, streamBufferSize, options))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    fileStream.Write(userBuffer, 0, userBuffer.Length);
                }
            }
        }

#if !NETFRAMEWORK // APIs added in .NET Core 2.0

        public IEnumerable<object[]> AsyncArguments()
        {
            // long fileSize, int userBufferSize, FileOptions options
            yield return new object[] { OneKibibyte, OneKibibyte, FileOptions.Asynchronous }; // small file size, user buffer size == file size
            yield return new object[] { OneKibibyte, OneKibibyte, FileOptions.None }; // same as above, but sync open, later async usage (common use case)
            
            yield return new object[] { OneMibibyte, HalfKibibyte, FileOptions.Asynchronous }; // medium size file, user buffer size * 8 == default stream buffer size (buffering is beneficial)
            yield return new object[] { OneMibibyte, HalfKibibyte, FileOptions.None }; // same as above, but sync open, later async usage (common use case)
            
            yield return new object[] { OneMibibyte, FourKibibytes, FileOptions.Asynchronous }; // medium size file, user buffer size == default stream buffer size (buffering is not beneficial)
            yield return new object[] { OneMibibyte, FourKibibytes, FileOptions.None }; // same as above, but sync open, later async usage (common use case)
            
            yield return new object[] { HundredMibibytes, FourKibibytes, FileOptions.Asynchronous }; //  big file, user buffer size == default stream buffer size (buffering is not beneficial)
            yield return new object[] { HundredMibibytes, FourKibibytes, FileOptions.None }; // same as above, but sync open, later async usage (common use case)
        }
        
        public IEnumerable<object[]> AsyncArguments_NoBuffering()
        {
            // long fileSize, int userBufferSize, FileOptions options
            yield return new object[] { OneMibibyte, SixteenKibibytes, FileOptions.Asynchronous }; // medium size file, user buffer size == 4 * default stream buffer size
            yield return new object[] { OneMibibyte, SixteenKibibytes, FileOptions.None }; // same as above, but sync open, later async usage
            
            yield return new object[] { HundredMibibytes, SixteenKibibytes, FileOptions.Asynchronous }; // big file, user buffer size == 4 * default stream buffer size
            yield return new object[] { HundredMibibytes, SixteenKibibytes, FileOptions.None }; // same as above, but sync open, later async usage
        }

        [Benchmark]
        [ArgumentsSource(nameof(AsyncArguments))]
        [BenchmarkCategory(Categories.NoWASM)]
        public Task<long> ReadAsync(long fileSize, int userBufferSize, FileOptions options)
            => ReadAsync(fileSize, userBufferSize, options, streamBufferSize: FourKibibytes);

        [Benchmark]
        [ArgumentsSource(nameof(AsyncArguments_NoBuffering))]
        [BenchmarkCategory(Categories.NoWASM)]
        public Task<long> ReadAsync_NoBuffering(long fileSize, int userBufferSize, FileOptions options)
            => ReadAsync(fileSize, userBufferSize, options, streamBufferSize: 1);

        private async Task<long> ReadAsync(long fileSize, int userBufferSize, FileOptions options, int streamBufferSize)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            Memory<byte> userBuffer = new Memory<byte>(_userBuffers[userBufferSize]);
            long bytesRead = 0;
            using (FileStream fileStream = new FileStream(_sourceFilePaths[fileSize], FileMode.Open, FileAccess.Read, FileShare.Read, streamBufferSize, options))
            {
                while (bytesRead < fileSize)
                {
                    bytesRead += await fileStream.ReadAsync(userBuffer, cancellationToken);
                }
            }

            return bytesRead;
        }

        [Benchmark]
        [ArgumentsSource(nameof(AsyncArguments))]
        [BenchmarkCategory(Categories.NoWASM)]
        public Task WriteAsync(long fileSize, int userBufferSize, FileOptions options)
            => WriteAsync(FileMode.Create, fileSize, userBufferSize, options, streamBufferSize: FourKibibytes);

        [Benchmark]
        [ArgumentsSource(nameof(AsyncArguments_NoBuffering))]
        [BenchmarkCategory(Categories.NoWASM)]
        public Task WriteAsync_NoBuffering(long fileSize, int userBufferSize, FileOptions options)
            => WriteAsync(FileMode.Create, fileSize, userBufferSize, options, streamBufferSize: 1);

        [Benchmark]
        [Arguments(OneMibibyte, FourKibibytes, FileOptions.DeleteOnClose | FileOptions.Asynchronous)]
        [BenchmarkCategory(Categories.NoWASM)]
        public Task AppendAsync(long fileSize, int userBufferSize, FileOptions options)
            => WriteAsync(FileMode.Append, fileSize, userBufferSize, options, streamBufferSize: FourKibibytes);

        private async Task WriteAsync(FileMode mode, long fileSize, int userBufferSize, FileOptions options, int streamBufferSize)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            Memory<byte> userBuffer = new Memory<byte>(_userBuffers[userBufferSize]);
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], mode, FileAccess.Write, FileShare.Read, streamBufferSize, options))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    await fileStream.WriteAsync(userBuffer, cancellationToken);
                }
            }
        }
#endif

#if NET6_0_OR_GREATER // APIs added in .NET 6
        private string _nonExistingFile;

        [GlobalSetup(Targets = new[] { nameof(Write_NoBuffering_PreallocationSize), nameof(WriteAsync_NoBuffering_PreallocationSize) })]
        public void SetupNonExistingFile()
        {
            _userBuffers = new Dictionary<int, byte[]>()
            {
                { SixteenKibibytes, ValuesGenerator.Array<byte>(SixteenKibibytes) },
            };

            string filePath = FileUtils.GetTestFilePath();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            _nonExistingFile = filePath;
        }

        [Benchmark]
        [Arguments(OneMibibyte, SixteenKibibytes, FileOptions.None)]
        [Arguments(HundredMibibytes, SixteenKibibytes, FileOptions.None)]
        public void Write_NoBuffering_PreallocationSize(long fileSize, int userBufferSize, FileOptions options)
        {
            byte[] userBuffer = _userBuffers[userBufferSize];
            FileStreamOptions fsOptions = new () { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None,
                BufferSize = 0, Options = options | FileOptions.DeleteOnClose, PreallocationSize = fileSize };
            using (FileStream fileStream = new FileStream(_nonExistingFile, fsOptions))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    fileStream.Write(userBuffer, 0, userBuffer.Length);
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        [Arguments(OneMibibyte, SixteenKibibytes, FileOptions.Asynchronous)]
        [Arguments(HundredMibibytes, SixteenKibibytes, FileOptions.Asynchronous)]
        public async Task WriteAsync_NoBuffering_PreallocationSize(long fileSize, int userBufferSize, FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            Memory<byte> userBuffer = new Memory<byte>(_userBuffers[userBufferSize]);
            FileStreamOptions fsOptions = new () { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None,
                BufferSize = 0, Options = options | FileOptions.DeleteOnClose, PreallocationSize = fileSize };
            using (FileStream fileStream = new FileStream(_nonExistingFile, fsOptions))
            {
                for (int i = 0; i < fileSize / userBufferSize; i++)
                {
                    await fileStream.WriteAsync(userBuffer, cancellationToken);
                }
            }
        }
#endif

        [Benchmark]
        [Arguments(OneKibibyte, FileOptions.None)]
        [Arguments(OneKibibyte, FileOptions.Asynchronous)]
        public void Flush(long fileSize, FileOptions options)
        {
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKibibytes, options))
            {
                for (int i = 0; i < fileSize; i++)
                {
                    fileStream.WriteByte(default); // make sure that Flush has something to actualy flush to disk

                    fileStream.Flush();
                }
            }
        }

        [BenchmarkCategory(Categories.NoWASM)]
        [Benchmark]
        [Arguments(OneKibibyte, FileOptions.None)]
        [Arguments(OneKibibyte, FileOptions.Asynchronous)]
        public async Task FlushAsync(long fileSize, FileOptions options)
        {
            using (FileStream fileStream = new FileStream(_destinationFilePaths[fileSize], FileMode.Create, FileAccess.Write, FileShare.Read, FourKibibytes, options))
            {
                for (int i = 0; i < fileSize; i++)
                {
                    fileStream.WriteByte(default);

                    await fileStream.FlushAsync();
                }
            }
        }

        [Benchmark]
        [Arguments(OneKibibyte, FileOptions.None)]
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
        [Arguments(OneKibibyte, FileOptions.Asynchronous)]
        [Arguments(OneKibibyte, FileOptions.None)]
        [Arguments(OneMibibyte, FileOptions.Asynchronous)]
        [Arguments(OneMibibyte, FileOptions.None)]
        [Arguments(HundredMibibytes, FileOptions.Asynchronous)]
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
    }
}
