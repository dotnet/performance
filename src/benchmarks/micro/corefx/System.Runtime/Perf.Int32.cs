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
    public class Perf_Int32
    {
        private char[] _destination = new char[int.MinValue.ToString().Length];
        
        public IEnumerable<object> Int32Values => new object[]
        {
            int.MinValue,
            (int)12345, // same value used by other tests to compare the perf
            int.MaxValue
        };

        public IEnumerable<object> StringValues => Int32Values.Select(value => value.ToString()).ToArray();

        [Benchmark]
        [ArgumentsSource(nameof(Int32Values))]
        public string ToString(int value) => value.ToString();
        
#if !NETFRAMEWORK && !NETCOREAPP2_0 // API added in .NET Core 2.1
        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public int Parse(string value) => int.Parse(value.AsSpan());

        [Benchmark]
        [ArgumentsSource(nameof(Int32Values))]
        public bool TryFormat(int value) => value.TryFormat(new Span<char>(_destination), out _);
#endif
    }
}
