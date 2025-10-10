// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Reflection
{
    [BenchmarkCategory(Categories.Runtime, Categories.Reflection)]
    public class RuntimeMethodInfo
    {
        private static readonly MethodInfo s_methodInfo = typeof(RuntimeMethodInfoTestClass).GetMethod(nameof(RuntimeMethodInfoTestClass.Method1));

        [Benchmark]
        public int GetHashCodeBenchmark()
        {
            return s_methodInfo.GetHashCode();
        }
    }

    public class RuntimeMethodInfoTestClass
    {
        public int Method1() => throw new NotImplementedException();
    }
}