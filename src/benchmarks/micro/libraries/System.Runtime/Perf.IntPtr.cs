// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_IntPtr
    {
        IntPtr ptr1 = new IntPtr(0);
        IntPtr ptr2 = new IntPtr(0);

        [Benchmark]
        public IntPtr ctor_int32() => new IntPtr(0);

        [Benchmark]
        public bool op_Equality_IntPtr_IntPtr() => ptr1 == ptr2;
    }
}