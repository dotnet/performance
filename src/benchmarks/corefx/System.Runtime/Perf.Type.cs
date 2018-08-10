// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Type
    {
        RuntimeTypeHandle TypeHandle = typeof(int).TypeHandle;
        Type Type1 = typeof(int);
        Type Type2 = typeof(string);
        
        [Benchmark]
        public Type GetTypeFromHandle() => Type.GetTypeFromHandle(TypeHandle);

        [Benchmark]
        public bool op_Equality() => Type1 == Type2;
    }
}
