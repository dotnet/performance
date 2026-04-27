// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Path
    {
        private readonly string _testPath = FileUtils.GetTestFilePath();
        private readonly string _testPath10 = PerfUtils.CreateString(10);
        private readonly string _testPath200 = PerfUtils.CreateString(200);
        private readonly string _testPath500 = PerfUtils.CreateString(500);
        private readonly string _testPath1000 = PerfUtils.CreateString(1000);
        private string _testPathNoRedundantSegments;
        private string _testPathWithRedundantSegments;

        [GlobalSetup]
        public void SetupPaths()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // This fully qualified path will be normalized by the Windows P/Invoke
                _testPathNoRedundantSegments = @"C:\repos\runtime\src\coreclr\runtime\src\libraries\System.Private.CoreLib\src\System\IO\Path.cs";
                // This unqualified path will be analyzed by our RedundantSegments.Windows code
                _testPathWithRedundantSegments = @"runtime\src\coreclr\runtime\src\libraries\System.Private.CoreLib\src\System\IO\Extra\..\Path.cs";
            }
            else
            {
                // Both paths will be analyzed by our RedundantSegments.Unix code
                _testPathNoRedundantSegments = "/home/user/runtime/src/coreclr/runtime/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs";
                _testPathWithRedundantSegments = "runtime/src/coreclr/runtime/src/libraries/System.Private.CoreLib/src/System/IO/Extra/../Path.cs";
            }
        }

        [Benchmark]
        public string Combine() => Path.Combine(_testPath, _testPath10);

        [Benchmark]
        [MemoryRandomization]
        public string GetFileName() => Path.GetFileName(_testPath);

        [Benchmark]
        public string GetDirectoryName() => Path.GetDirectoryName(_testPath);

        [Benchmark]
        [MemoryRandomization]
        public string ChangeExtension() => Path.ChangeExtension(_testPath, ".new");

        [Benchmark]
        [MemoryRandomization]
        public string GetExtension() => Path.GetExtension(_testPath);

        [Benchmark]
        [MemoryRandomization]
        public string GetFileNameWithoutExtension() => Path.GetFileNameWithoutExtension(_testPath);

        [Benchmark]
        public string GetFullPathForLegacyLength() => Path.GetFullPath(_testPath200);

#if !NETFRAMEWORK // long paths are always supported on .NET Core
        [Benchmark]
        public string GetFullPathForTypicalLongPath() => Path.GetFullPath(_testPath500);

        [Benchmark]
        public void GetFullPathForReallyLongPath() => Path.GetFullPath(_testPath1000);
#endif

        [Benchmark]
        public void GetFullPathNoRedundantSegments() => Path.GetFullPath(_testPathNoRedundantSegments);

        [Benchmark]
        public void GetFullPathWithRedundantSegments() => Path.GetFullPath(_testPathWithRedundantSegments);

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
