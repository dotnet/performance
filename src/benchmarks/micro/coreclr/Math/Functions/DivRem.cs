// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class MathTests
    {
        private static int _aInt32 = 99, _bInt32 = 10, _resultInt32;
        private static long _aInt64 = 99, _bInt64 = 10, _resultInt64;

        [Benchmark]
        public int DivRemInt32() => Math.DivRem(_aInt32, _bInt32, out _resultInt32);

        [Benchmark]
        public long DivRemInt64() => Math.DivRem(_aInt64, _bInt64, out _resultInt64);
    }
}
