// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Version
    {
        private Version _v2 = new Version(1, 2);
        private Version _v3 = new Version(1, 2, 3);
        private Version _v4 = new Version(1, 2, 3, 4);
        private Version _vL = new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);

        [Benchmark]
        public Version Ctor2() => new Version(1, 2);

        [Benchmark]
        public Version Ctor3() => new Version(1, 2, 3);

        [Benchmark]
        public Version Ctor4() => new Version(1, 2, 3, 4);

        [Benchmark]
        [MemoryRandomization]
        public Version Parse2() => Version.Parse("1.2");

        [Benchmark]
        [MemoryRandomization]
        public Version Parse3() => Version.Parse("1.2.3");

        [Benchmark]
        [MemoryRandomization]
        public Version Parse4() => Version.Parse("1.2.3.4");

        [Benchmark]
        [MemoryRandomization]
        public bool TryParse2() => Version.TryParse("1.2", out _);

        [Benchmark]
        [MemoryRandomization]
        public bool TryParse3() => Version.TryParse("1.2.3", out _);

        [Benchmark]
        [MemoryRandomization]
        public bool TryParse4() => Version.TryParse("1.2.3.4", out _);

        [Benchmark]
        public string ToString2() => _v2.ToString();
        
        [Benchmark]
        public string ToString3() => _v3.ToString();
        
        [Benchmark]
        public string ToString4() => _v4.ToString();
        
        [Benchmark]
        public string ToStringL() => _vL.ToString();

#if !NETFRAMEWORK // API added in .NET Core 2.1
        private char[] _buffer = new char[100];

        [Benchmark]
        public bool TryFormat2() => _v2.TryFormat(_buffer, out _);
        
        [Benchmark]
        public bool TryFormat3() => _v3.TryFormat(_buffer, out _);
        
        [Benchmark]
        public bool TryFormat4() => _v4.TryFormat(_buffer, out _);
        
        [Benchmark]
        public bool TryFormatL() => _vL.TryFormat(_buffer, out _);
#endif
    }
}
