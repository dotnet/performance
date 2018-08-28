// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Path
    {
        private readonly string _testPath = FileUtils.GetTestFilePath();
        private readonly string _testPath10 = PerfUtils.CreateString(10);
        private readonly string _testPath200 = PerfUtils.CreateString(200);
        private readonly string _testPath500 = PerfUtils.CreateString(500);
        private readonly string _testPath1000 = PerfUtils.CreateString(1000);

        [Benchmark]
        public string Combine() => Path.Combine(_testPath, _testPath10);

        [Benchmark]
        public string GetFileName() => Path.GetFileName(_testPath);

        [Benchmark]
        public string GetDirectoryName() => Path.GetDirectoryName(_testPath);

        [Benchmark]
        public string ChangeExtension() => Path.ChangeExtension(_testPath, ".new");

        [Benchmark]
        public string GetExtension() => Path.GetExtension(_testPath);

        [Benchmark]
        public string GetFileNameWithoutExtension() => Path.GetFileNameWithoutExtension(_testPath);

        [Benchmark]
        public string GetFullPathForLegacyLength() => Path.GetFullPath(_testPath200);

        [Benchmark]
        public string GetFullPathForTypicalLongPath() => Path.GetFullPath(_testPath500);

        [Benchmark]
        public void GetFullPathForReallyLongPath() => Path.GetFullPath(_testPath1000);

        [Benchmark]
        public string GetPathRoot() => Path.GetPathRoot(_testPath);

        [Benchmark]
        public string GetRandomFileName() => Path.GetRandomFileName();

        [Benchmark]
        public string GetTempPath() => Path.GetTempPath();

        [Benchmark]
        public bool HasExtension() => Path.HasExtension(_testPath);

        [Benchmark]
        public bool IsPathRooted() => Path.IsPathRooted(_testPath);
    }
}
