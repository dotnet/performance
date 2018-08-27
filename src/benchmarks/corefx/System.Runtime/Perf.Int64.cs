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
    public class Perf_Int64
    {
        private char[] _destination = new char[long.MinValue.ToString().Length];

        public IEnumerable<object> StringValues => Int64Values.Select(value => value.ToString()).ToArray();
        
        public IEnumerable<object> Int64Values => new object[]
        {
            long.MinValue,
            (long)12345, // same value used by other tests to compare the perf
            long.MaxValue,
        };

        [Benchmark]
        [ArgumentsSource(nameof(Int64Values))]
        public string ToString(long value) => value.ToString();

#if NETCOREAPP2_1
        [Benchmark]
        [ArgumentsSource(nameof(Int64Values))]
        public bool TryFormat(long value) => value.TryFormat(new Span<char>(_destination), out _);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public long Parse(string value) => long.Parse(value.AsSpan());
#endif
    }
}
