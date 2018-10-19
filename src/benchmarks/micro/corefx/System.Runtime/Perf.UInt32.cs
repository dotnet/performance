// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_UInt32
    {
        private char[] _destination = new char[uint.MaxValue.ToString().Length];

        public IEnumerable<object> StringValues => UInt32Values.Select(value => value.ToString()).ToArray();
        
        public IEnumerable<object> UInt32Values => new object[]
        {
            uint.MinValue,
            (uint)12345, // same value used by other tests to compare the perf
            uint.MaxValue,
        };

        [Benchmark]
        [ArgumentsSource(nameof(UInt32Values))]
        public string ToString(uint value) => value.ToString();

#if NETCOREAPP2_1
        [Benchmark]
        [ArgumentsSource(nameof(UInt32Values))]
        public bool TryFormat(uint value) => value.TryFormat(new Span<char>(_destination), out _);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public void Parse(string value) => uint.Parse(value.AsSpan());
#endif
    }
}
