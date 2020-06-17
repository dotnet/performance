﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD)]
    public class Perf_Vector3
    {
        [Benchmark]
        public Vector3 AddOperatorBenchmark() => VectorTests.Vector3Value + VectorTests.Vector3Delta;
        
        [Benchmark]
        public Vector3 AddFunctionBenchmark() => Vector3.Add(VectorTests.Vector3Value, VectorTests.Vector3Delta);

        [Benchmark]
        public Vector3 MultiplyOperatorBenchmark() => VectorTests.Vector3Value * VectorTests.Vector3Delta;

        [Benchmark]
        public Vector3 MultiplyFunctionBenchmark() => Vector3.Multiply(VectorTests.Vector3Value, VectorTests.Vector3Delta);

        [Benchmark]
        public Vector3 SubtractOperatorBenchmark() => VectorTests.Vector3Value - VectorTests.Vector3Delta;

        [Benchmark]
        public Vector3 SubtractFunctionBenchmark() => Vector3.Subtract(VectorTests.Vector3Value, VectorTests.Vector3Delta);

        [Benchmark]
        public float DistanceSquaredBenchmark() => Vector3.DistanceSquared(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public float LengthSquaredBenchmark() => VectorTests.Vector3Value.LengthSquared();

        [Benchmark]
        public int GetHashCodeBenchmark() => VectorTests.Vector3Value.GetHashCode();

        [Benchmark]
        public Vector3 SquareRootBenchmark() => Vector3.SquareRoot(VectorTests.Vector3Value);

        [Benchmark]
        public Vector3 NormalizeBenchmark() => Vector3.Normalize(VectorTests.Vector3Value);

        [Benchmark]
        public float DotBenchmark() => Vector3.Dot(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public float DistanceBenchmark() => Vector3.Distance(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public float LengthBenchmark() => VectorTests.Vector3Value.Length();

        [Benchmark]
        public Vector3 CrossBenchmark() => Vector3.Cross(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public Vector3 ClampBenchmark() => Vector3.Clamp(VectorTests.Vector3Value, VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public Vector3 MinBenchmark() => Vector3.Min(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public Vector3 MaxBenchmark() => Vector3.Max(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);
    }
}
