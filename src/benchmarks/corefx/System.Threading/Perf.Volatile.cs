// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Volatile
    {
        private double _location = 0;
        private double _newValue = 1;
        
        [Benchmark]
        public double Read_double() => Volatile.Read(ref _location);

        [Benchmark]
        public void Write_double() => Volatile.Write(ref _location, _newValue);
    }
}