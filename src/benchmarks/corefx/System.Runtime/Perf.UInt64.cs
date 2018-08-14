// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_UInt64
    {
        private char[] _destination = new char[ulong.MaxValue.ToString().Length];

        public IEnumerable<object> StringValues => UInt64Values.Select(value => value.ToString()).ToArray();
        
        public static object[] UInt64Values => new object[]
        {
            ulong.MinValue,
            (ulong)12345, // same value used by other tests to compare the perf
            ulong.MaxValue,
        };

        [Benchmark]
        [ArgumentsSource(nameof(UInt64Values))]
        public string ToString(ulong value) => value.ToString();

#if NETCOREAPP2_1
        [Benchmark]
        [ArgumentsSource(nameof(UInt64Values))]
        public bool TryFormat(ulong value) => value.TryFormat(new Span<char>(_destination), out _);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public ulong Parse(string value) => ulong.Parse(value.AsSpan());
#endif
    }
}
