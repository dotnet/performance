// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// following benchmarks consume .NET Core 2.1 APIs and are disabled for other frameworks in .csproj file

using BenchmarkDotNet.Attributes;

namespace System.Numerics.Tests
{
    public partial class Constructor
    {
        [Benchmark]
        public Vector<Byte> ConstructorBenchmark_Byte() => new Vector<byte>(new Span<Byte>(_arrValues_Byte));

        [Benchmark]
        public Vector<SByte> ConstructorBenchmark_SByte() => new Vector<sbyte>(new Span<SByte>(_arrValues_SByte));

        [Benchmark]
        public Vector<UInt16> ConstructorBenchmark_UInt16() => new Vector<ushort>(new Span<UInt16>(_arrValues_UInt16));

        [Benchmark]
        public Vector<Int16> ConstructorBenchmark_Int16() => new Vector<short>(new Span<Int16>(_arrValues_Int16));

        [Benchmark]
        public Vector<UInt32> ConstructorBenchmark_UInt32() => new Vector<uint>(new Span<UInt32>(_arrValues_UInt32));

        [Benchmark]
        public Vector<Int32> ConstructorBenchmark_Int32() => new Vector<int>(new Span<Int32>(_arrValues_Int32));

        [Benchmark]
        public Vector<UInt64> ConstructorBenchmark_UInt64() => new Vector<ulong>(new Span<UInt64>(_arrValues_UInt64));

        [Benchmark]
        public Vector<Int64> ConstructorBenchmark_Int64() => new Vector<long>(new Span<Int64>(_arrValues_Int64));

        [Benchmark]
        public Vector<Single> ConstructorBenchmark_Single() => new Vector<float>(new Span<Single>(_arrValues_Single));

        [Benchmark]
        public Vector<Double> ConstructorBenchmark_Double() => new Vector<double>(new Span<Double>(_arrValues_Double));
    }
}