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
            214748364L,
            2L,
            21474836L,
            21474L,
            214L,
            2147L,
            214748L,
            21L,
            2147483L,
            922337203685477580L,
            92233720368547758L,
            9223372036854775L,
            922337203685477L,
            92233720368547L,
            9223372036854L,
            922337203685L,
            92233720368L,
            -214748364L,
            -2L,
            -21474836L,
            -21474L,
            -214L,
            -2147L,
            -214748L,
            -21L,
            -2147483L,
            -922337203685477580L,
            -92233720368547758L,
            -9223372036854775L,
            -922337203685477L,
            -92233720368547L,
            -9223372036854L,
            -922337203685L,
            -92233720368L,
            0L,
            -9223372036854775808L, // min value
            9223372036854775807L, // max value
            -2147483648L, // int32 min value
            2147483647L, // int32 max value
            -4294967295000000000L, // -(uint.MaxValue * Billion)
            4294967295000000000L, // uint.MaxValue * Billion
            -4294967295000000001L, // -(uint.MaxValue * Billion + 1)
            4294967295000000001L // uint.MaxValue * Billion + 1
        };

        [Benchmark]
        [ArgumentsSource(nameof(Int64Values))]
        public string ToString(long value) => value.ToString();

#if NETCOREAPP2_1
        [Benchmark]
        [ArgumentsSource(nameof(Int64Values))]
        public bool TryFormat(long value) => value.TryFormat(new Span<char>(_destination), out _);

        [Benchmark]
        [ArgumentsSource(nameof(Int64Values))]
        public long Parse(string value) => long.Parse(value.AsSpan());
#endif
    }
}
