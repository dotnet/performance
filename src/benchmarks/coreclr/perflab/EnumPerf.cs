// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using BenchmarkDotNet.Attributes;

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

    public class EnumPerf
    {
        [Benchmark]
        [Arguments(Color.Red, Color.White)]
        public int EnumCompareTo(Color red, Color white) => red.CompareTo(white);

        [Benchmark]
        [Arguments(Color.Black)]
        public Type ObjectGetType(Color @enum) => @enum.GetType();

        [Benchmark]
        [Arguments(Color.Black)]
        public Type ObjectGetTypeNoBoxing(Object @object) => @object.GetType();

        [Benchmark]
        [Arguments(Color.Black, Color.White)]
        public bool EnumEquals(Color black, Color white) => black.Equals(white);
    }
}
