// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.CoreCLR)]
    public class Perf_Interlocked
    {
        private int _int = 0;
        private long _long = 0;
        private string _location, _newValue, _comparand;

        [Benchmark]
        public int Increment_int() => Interlocked.Increment(ref _int);

        [Benchmark]
        public int Decrement_int() => Interlocked.Decrement(ref _int);

        [Benchmark]
        public long Increment_long() => Interlocked.Increment(ref _long);

        [Benchmark]
        public long Decrement_long() => Interlocked.Decrement(ref _long);

        [Benchmark]
        public int Add_int() => Interlocked.Add(ref _int, 2);

        [Benchmark]
        public long Add_long() => Interlocked.Add(ref _long, 2);

        [Benchmark]
        public int Exchange_int() => Interlocked.Exchange(ref _int, 1);

        [Benchmark]
        public long Exchange_long() => Interlocked.Exchange(ref _long, 1);

        [Benchmark]
        public int CompareExchange_int() => Interlocked.CompareExchange(ref _int, 1, 0);

        [Benchmark]
        public long CompareExchange_long() => Interlocked.CompareExchange(ref _long, 1, 0);

        [GlobalSetup(Target = nameof(CompareExchange_object_Match))]
        public void Setup_CompareExchange_object_Match()
        {
            _location = "What?";
            _newValue = "World";
            _comparand = "What?";
        }
        
        [Benchmark]
        public string CompareExchange_object_Match() => Interlocked.CompareExchange(ref _location, _newValue, _comparand);

        [GlobalSetup(Target = nameof(CompareExchange_object_NoMatch))]
        public void Setup_CompareExchange_object_NoMatch()
        {
            _location = "Hello";
            _newValue = "World";
            _comparand = "What?";
        }
        
        [Benchmark]
        public string CompareExchange_object_NoMatch() => Interlocked.CompareExchange(ref _location, _newValue, _comparand);
    }
}
