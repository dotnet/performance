// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Vector2
    {
        [Benchmark]
        public Vector2 AddOperatorBenchmark() => VectorTests.Vector2Value + VectorTests.Vector2Delta;
        
        [Benchmark]
        public Vector2 AddFunctionBenchmark() => Vector2.Add(VectorTests.Vector2Value, VectorTests.Vector2Delta);

        [Benchmark]
        public Vector2 SubtractOperatorBenchmark() => VectorTests.Vector2Value - VectorTests.Vector2Delta;

        [Benchmark]
        public Vector2 SubtractFunctionBenchmark() => Vector2.Subtract(VectorTests.Vector2Value, VectorTests.Vector2Delta);

        [Benchmark]
        public Vector2 MultiplyOperatorBenchmark() => VectorTests.Vector2Value * VectorTests.Vector2Delta;

        [Benchmark]
        public Vector2 MultiplyFunctionBenchmark() => Vector2.Multiply(VectorTests.Vector2Value, VectorTests.Vector2Delta);
        
        [Benchmark]
        public float DistanceSquaredBenchmark() => Vector2.DistanceSquared(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public float LengthSquaredBenchmark() => VectorTests.Vector2Value.LengthSquared();

        [Benchmark]
        public int GetHashCodeBenchmark() => VectorTests.Vector2Value.GetHashCode();

        [Benchmark]
        public float DistanceBenchmark() => Vector2.Distance(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public float DotBenchmark() => Vector2.Dot(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public float LengthBenchmark() => VectorTests.Vector2Value.Length();

        [Benchmark]
        public Vector2 SquareRootBenchmark() => Vector2.SquareRoot(VectorTests.Vector2Value);

        [Benchmark]
        public Vector2 NormalizeBenchmark() => Vector2.Normalize(VectorTests.Vector2Value);

        [Benchmark]
        public Vector2 ClampBenchmark() => Vector2.Clamp(VectorTests.Vector2Value, VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public Vector2 MinBenchmark() => Vector2.Min(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public Vector2 MaxBenchmark() => Vector2.Max(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);
    }
}
