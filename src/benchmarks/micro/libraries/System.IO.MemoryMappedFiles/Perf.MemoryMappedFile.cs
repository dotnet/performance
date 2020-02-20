// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.MemoryMappedFiles.Tests
{
    /// <summary>
    /// Performance tests for the construction and disposal of MemoryMappedFiles of varying sizes
    /// </summary>
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_MemoryMappedFile
    {
        private TempFile _file;
        
        [Params(10000, 
                100000, 
                1000000,
                10000000)]
        public int capacity; 
        
        [Benchmark]
        public void CreateNew() => MemoryMappedFile.CreateNew(null, capacity).Dispose();

        [GlobalSetup(Target = nameof(CreateFromFile))]
        public void SetupCreateFromFile() => _file = new TempFile(GetTestFilePath(), capacity);
        
        [Benchmark]
        public void CreateFromFile()
        {
            // Note that the test results will include the disposal overhead of both the MemoryMappedFile
            // as well as the Accessor for it
            using (MemoryMappedFile mmfile = MemoryMappedFile.CreateFromFile(_file.Path))
            using (mmfile.CreateViewAccessor(capacity / 4, capacity / 2))
            { }
        }
        
        [GlobalCleanup(Target = nameof(CreateFromFile))]
        public void CleanupCreateFromFile() => _file.Dispose();
        
        private string GetTestFilePath() => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    }
}
