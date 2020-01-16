// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.SIMD)]
    public class Perf_Vector4
    {
        [Benchmark]
        public Vector4 AddOperatorBenchmark() => VectorTests.Vector4Value + VectorTests.Vector4Delta;

        [Benchmark]
        public Vector4 AddFunctionBenchmark() => Vector4.Add(VectorTests.Vector4Value, VectorTests.Vector4Delta);

        [Benchmark]
        public Vector4 SubtractOperatorBenchmark() => VectorTests.Vector4Value - VectorTests.Vector4Delta;

        [Benchmark]
        public float DistanceSquaredBenchmark() => Vector4.DistanceSquared(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);

        [Benchmark]
        public float DistanceSquaredJitOptimizeCanaryBenchmark() => Vector4.DistanceSquared(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);

        [Benchmark]
        public Vector4 MultiplyOperatorBenchmark() => VectorTests.Vector4Value * VectorTests.Vector4Delta;

        [Benchmark]
        public Vector4 SubtractFunctionBenchmark() => Vector4.Subtract(VectorTests.Vector4Value, VectorTests.Vector4Delta);

        [Benchmark]
        public Vector4 MultiplyFunctionBenchmark() => Vector4.Multiply(VectorTests.Vector4Value, VectorTests.Vector4Delta);

        [Benchmark]
        public float LengthSquaredBenchmark() => VectorTests.Vector4Value.LengthSquared();

        [Benchmark]
        public int GetHashCodeBenchmark() => VectorTests.Vector4Value.GetHashCode();

        [Benchmark]
        public Vector4 SquareRootBenchmark() => Vector4.SquareRoot(VectorTests.Vector4Value);

        [Benchmark]
        public Vector4 NormalizeBenchmark() => Vector4.Normalize(VectorTests.Vector4Value);

        [Benchmark]
        public float DistanceBenchmark() => Vector4.Distance(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);

        [Benchmark]
        public float LengthBenchmark() => VectorTests.Vector4Value.Length();

        [Benchmark]
        public float DotBenchmark() => Vector4.Dot(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);
    }
}
