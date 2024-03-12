// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_File
    {
        private const int OneKibibyte = 1 << 10; // 1024
        private const int HalfKibibyte = OneKibibyte >> 1;
        private const int FourKibibytes = OneKibibyte << 2; // default Stream buffer size
        private const int SixteenKibibytes = FourKibibytes << 2; // default Stream buffer size * 4
        private const int OneMibibyte = OneKibibyte << 10;
        private const int HundredMibibytes = OneMibibyte * 100;

        private const int DeleteteInnerIterations = 10;

        private string _testFilePath;
        private string[] _filesToRemove;
        private Dictionary<int, byte[]> _userBuffers;
        private Dictionary<int, string> _filesToRead;
        private string[] _linesToAppend;
        private Dictionary<int, string> _textToAppend;

        [GlobalSetup(Target = nameof(Exists))]
        public void SetupExists()
        {
            _testFilePath = FileUtils.GetTestFilePath();
            File.Create(_testFilePath).Dispose();
        }

        [Benchmark]
        public void Exists() => File.Exists(_testFilePath);

        [GlobalCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [IterationSetup(Target = nameof(Delete))]
        public void SetupDeleteIteration()
        {
            var testFile = FileUtils.GetTestFilePath();
            _filesToRemove = Enumerable.Range(1, DeleteteInnerIterations).Select(index => testFile + index).ToArray();
            foreach (var file in _filesToRemove)
                File.Create(file).Dispose();
        }

        [Benchmark(OperationsPerInvoke = DeleteteInnerIterations)]
        public void Delete()
        {
            var filesToRemove = _filesToRemove;

            foreach (var file in filesToRemove)
                File.Delete(file);
        }

        [GlobalSetup(Targets = new[] { nameof(WriteAllBytes), "WriteAllBytesAsync" })]
        public void SetupWriteAllBytes()
        {
            _testFilePath = FileUtils.GetTestFilePath();
            _userBuffers = new Dictionary<int, byte[]>()
            {
                { HalfKibibyte, ValuesGenerator.Array<byte>(HalfKibibyte) },
                { FourKibibytes, ValuesGenerator.Array<byte>(FourKibibytes) },
                { SixteenKibibytes, ValuesGenerator.Array<byte>(SixteenKibibytes) },
                { OneMibibyte, ValuesGenerator.Array<byte>(OneMibibyte) },
                { HundredMibibytes, ValuesGenerator.Array<byte>(HundredMibibytes) },
            };
        }

        [Benchmark]
        [Arguments(HalfKibibyte)]
        [Arguments(FourKibibytes)]
        [Arguments(SixteenKibibytes)]
        [Arguments(OneMibibyte)]
        [Arguments(HundredMibibytes)]
        public void WriteAllBytes(int size) => File.WriteAllBytes(_testFilePath, _userBuffers[size]);

        [GlobalSetup(Targets = new[] { nameof(ReadAllBytes), "ReadAllBytesAsync", nameof(CopyTo), nameof(CopyToOverwrite) })]
        public void SetupReadAllBytes()
        {
            // use non-temp file path to ensure that we don't test some unusal File System on Unix
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.Create);
            File.WriteAllBytes(_testFilePath = Path.Combine(baseDir, Path.GetRandomFileName()), Array.Empty<byte>());
            _filesToRead = new Dictionary<int, string>()
            {
                { HalfKibibyte, WriteBytes(HalfKibibyte) },
                { FourKibibytes, WriteBytes(FourKibibytes) },
                { SixteenKibibytes, WriteBytes(SixteenKibibytes) },
                { OneMibibyte, WriteBytes(OneMibibyte) },
                { HundredMibibytes, WriteBytes(HundredMibibytes) },
            };

            string WriteBytes(int fileSize)
            {
                string filePath = Path.Combine(baseDir, Path.GetRandomFileName());
                File.WriteAllBytes(filePath, ValuesGenerator.Array<byte>(fileSize));
                return filePath;
            }
        }

        [GlobalCleanup(Targets = new[] { nameof(ReadAllBytes), "ReadAllBytesAsync", nameof(CopyTo), nameof(CopyToOverwrite) })]
        public void CleanupReadAllBytes()
        {
            foreach (string filePath in _filesToRead.Values)
                File.Delete(filePath);

            File.Delete(_testFilePath);
        }

        [Benchmark]
        [Arguments(HalfKibibyte)]
        [Arguments(FourKibibytes)]
        [Arguments(SixteenKibibytes)]
        [Arguments(OneMibibyte)]
        [Arguments(HundredMibibytes)]
        [MemoryRandomization]
        public byte[] ReadAllBytes(int size) => File.ReadAllBytes(_filesToRead[size]);

#if !NETFRAMEWORK
        [BenchmarkCategory(Categories.NoWASM)]
        [Benchmark]
        [Arguments(HalfKibibyte)]
        [Arguments(FourKibibytes)]
        [Arguments(SixteenKibibytes)]
        [Arguments(OneMibibyte)]
        [Arguments(HundredMibibytes)]
        public Task WriteAllBytesAsync(int size) => File.WriteAllBytesAsync(_testFilePath, _userBuffers[size]);

        [BenchmarkCategory(Categories.NoWASM)]
        [Benchmark]
        [Arguments(HalfKibibyte)]
        [Arguments(FourKibibytes)]
        [Arguments(SixteenKibibytes)]
        [Arguments(OneMibibyte)]
        [Arguments(HundredMibibytes)]
        [MemoryRandomization]
        public Task<byte[]> ReadAllBytesAsync(int size) => File.ReadAllBytesAsync(_filesToRead[size]);
#endif

        [GlobalSetup(Targets = new[] { nameof(ReadAllLines), "ReadAllLinesAsync" })]
        public void SetupReadAllLines()
            => File.WriteAllLines(_testFilePath = FileUtils.GetTestFilePath(), ValuesGenerator.ArrayOfStrings(count: 100, minLength: 20, maxLength: 80));

        [Benchmark]
        public string[] ReadAllLines() => File.ReadAllLines(_testFilePath);

#if !NETFRAMEWORK
        [BenchmarkCategory(Categories.NoWASM)]
        [Benchmark]
        public Task<string[]> ReadAllLinesAsync() => File.ReadAllLinesAsync(_testFilePath);
#endif

        [GlobalSetup(Targets = new[] { nameof(AppendAllLines), "AppendAllLinesAsync" })]
        public void SetupAppendAllLines()
        {
            _testFilePath = FileUtils.GetTestFilePath();
            _linesToAppend = ValuesGenerator.ArrayOfStrings(count: 20, minLength: 20, maxLength: 80);
        }

        [Benchmark(OperationsPerInvoke = 1000)]
        public void AppendAllLines()
        {
            for (int i = 0; i < 1000; i++)
            {
                File.AppendAllLines(_testFilePath, _linesToAppend);
            }

            // We can't use:
            // - [GlobalCleanup] because it could be invoked after millions of invocations of this benchmark, which could consume entire disk space
            // - [IterationSetup] because it would invoke the benchmark once per iteration: https://github.com/dotnet/performance/blob/main/docs/microbenchmark-design-guidelines.md#IterationSetup
            // This is why we Delete the file from the benchmark itself, but add plenty of OperationsPerInvoke so it's amortized
            File.Delete(_testFilePath);
        }

        [GlobalSetup(Targets = new[] { nameof(AppendAllText), "AppendAllTextAsync", nameof(WriteAllText), "WriteAllTextAsync" })]
        public void SetupAppendAllText()
        {
            _testFilePath = FileUtils.GetTestFilePath();
            _textToAppend = new Dictionary<int, string>()
            {
                { 10, new string('a', 10) },
                { 100, new string('a', 100) },
                { 10_000, new string('a', 10_000) },
                { 100_000, new string('a', 100_000) },
            };
        }

        [Benchmark(OperationsPerInvoke = 1000)]
        [Arguments(100)]
        [Arguments(10_000)]
        public void AppendAllText(int size)
        {
            string content = _textToAppend[size];
            for (int i = 0; i < 1000; i++)
            {
                File.AppendAllText(_testFilePath, content);
            }

            File.Delete(_testFilePath); // see the comment in AppendAllLines
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(10_000)]
        [Arguments(100_000)]
        public void WriteAllText(int size) => File.WriteAllText(_testFilePath, _textToAppend[size]);

#if !NETFRAMEWORK
        [BenchmarkCategory(Categories.NoWASM)]
        [Benchmark(OperationsPerInvoke = 1000)]
        public async Task AppendAllLinesAsync()
        {
            for (int i = 0; i < 1000; i++)
            {
                await File.AppendAllLinesAsync(_testFilePath, _linesToAppend);
            }

            File.Delete(_testFilePath); // see the comment in AppendAllLines
        }

        [BenchmarkCategory(Categories.NoWASM)]
        [Benchmark(OperationsPerInvoke = 1000)]
        [Arguments(100)]
        [Arguments(10_000)]
        public async Task AppendAllTextAsync(int size)
        {
            string content = _textToAppend[size];
            for (int i = 0; i < 1000; i++)
            {
                await File.AppendAllTextAsync(_testFilePath, content);
            }

            File.Delete(_testFilePath); // see the comment in AppendAllLines
        }

        [BenchmarkCategory(Categories.NoWASM)]
        [Benchmark]
        [Arguments(100)]
        [Arguments(10_000)]
        [Arguments(100_000)]
        public Task WriteAllTextAsync(int size) => File.WriteAllTextAsync(_testFilePath, _textToAppend[size]);
#endif

        [Benchmark]
        [Arguments(HalfKibibyte)]
        [Arguments(FourKibibytes)]
        [Arguments(OneMibibyte)]
        [Arguments(HundredMibibytes)]
        public void CopyTo(int size)
        {
            File.Delete(_testFilePath);
            File.Copy(_filesToRead[size], _testFilePath); // overwrite defaults to false
        }

        [Benchmark]
        [Arguments(HalfKibibyte)]
        [Arguments(FourKibibytes)]
        [Arguments(OneMibibyte)]
        [Arguments(HundredMibibytes)]
        public void CopyToOverwrite(int size) => File.Copy(_filesToRead[size], _testFilePath, overwrite: true);
    }
}
