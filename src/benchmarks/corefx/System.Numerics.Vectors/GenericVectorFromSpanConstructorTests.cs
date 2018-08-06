// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// following benchmarks consume .NET Core 2.1 APIs and are disabled for other frameworks in .csproj file

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Numerics.Tests
{
    public partial class Constructor
    {
        [Benchmark]
        public void ConstructorBenchmark_Byte() => Construct(new Span<Byte>(_arrValues_Byte));

        [Benchmark]
        public void ConstructorBenchmark_SByte() => Construct(new Span<SByte>(_arrValues_SByte));

        [Benchmark]
        public void ConstructorBenchmark_UInt16() => Construct(new Span<UInt16>(_arrValues_UInt16));

        [Benchmark]
        public void ConstructorBenchmark_Int16() => Construct(new Span<Int16>(_arrValues_Int16));

        [Benchmark]
        public void ConstructorBenchmark_UInt32() => Construct(new Span<UInt32>(_arrValues_UInt32));

        [Benchmark]
        public void ConstructorBenchmark_Int32() => Construct(new Span<Int32>(_arrValues_Int32));

        [Benchmark]
        public void ConstructorBenchmark_UInt64() => Construct(new Span<UInt64>(_arrValues_UInt64));

        [Benchmark]
        public void ConstructorBenchmark_Int64() => Construct(new Span<Int64>(_arrValues_Int64));

        [Benchmark]
        public void ConstructorBenchmark_Single() => Construct(new Span<Single>(_arrValues_Single));

        [Benchmark]
        public void ConstructorBenchmark_Double() => Construct(new Span<Double>(_arrValues_Double));

        public static void Construct<T>(Span<T> values) where T : struct
        {
            for (var iteration = 0; iteration < DefaultInnerIterationsCount; iteration++)
            {
                Vector<T> vect = new Vector<T>(values);
            }
        }
    }
}