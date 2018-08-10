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
            214748364LU,
            2LU,
            21474836LU,
            21474LU,
            214LU,
            2147LU,
            214748LU,
            21LU,
            2147483LU,
            922337203685477580LU,
            92233720368547758LU,
            9223372036854775LU,
            922337203685477LU,
            92233720368547LU,
            9223372036854LU,
            922337203685LU,
            92233720368LU,
            0LU, // min value
            18446744073709551615LU, // max value
            2147483647LU, // int32 max value
            9223372036854775807LU, // int64 max value
            1000000000000000000LU, // quintillion
            4294967295000000000LU, // uint.MaxValue * Billion
            4294967295000000001LU // uint.MaxValue * Billion + 1
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
