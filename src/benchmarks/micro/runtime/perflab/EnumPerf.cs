// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace PerfLabTests
{
    public enum Color
    {
        Black,
        White,
        Red,
        Brown,
        Yellow,
        Purple,
        Orange
    }

    [BenchmarkCategory(Categories.Runtime, Categories.Perflab)]
    public class EnumPerf
    {
        public object blackObject = Color.Black;
        
        [Benchmark]
        [Arguments(Color.Red)]
        public int EnumCompareTo(Color color) => color.CompareTo(Color.White);

        [Benchmark]
        [MemoryRandomization]
        public Type ObjectGetType() => Color.Black.GetType();

        [Benchmark]
        public Type ObjectGetTypeNoBoxing() => blackObject.GetType();

        [Benchmark]
        public bool EnumEquals() => Color.Black.Equals(Color.White);
    }

}
