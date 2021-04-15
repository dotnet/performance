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

        [GlobalSetup(Target = nameof(Exists))]
        public void SetupExists()
        {
            _testFilePath = FileUtils.GetTestFilePath();
            File.Create(_testFilePath).Dispose();
        }
        
        [Benchmark]
        public void Exists() => File.Exists(_testFilePath); 
        
        [GlobalCleanup(Target = nameof(Exists))]
        public void CleanupExists() => File.Delete(_testFilePath);

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
        public void WriteAllBytes(int userBuffer) => File.WriteAllBytes(_testFilePath, _userBuffers[userBuffer]);

#if !NETFRAMEWORK
        [Benchmark]
        [Arguments(HalfKibibyte)]
        [Arguments(FourKibibytes)]
        [Arguments(SixteenKibibytes)]
        [Arguments(OneMibibyte)]
        [Arguments(HundredMibibytes)]
        public Task WriteAllBytesAsync(int userBuffer) => File.WriteAllBytesAsync(_testFilePath, _userBuffers[userBuffer]);
#endif

        [GlobalCleanup(Targets = new[] { nameof(WriteAllBytes), "WriteAllBytesAsync" })]
        public void CleanupWriteAllBytes() => File.Delete(_testFilePath);
    }
}
