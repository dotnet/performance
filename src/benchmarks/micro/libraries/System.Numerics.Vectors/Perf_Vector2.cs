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
        public Vector2 CreateFromScalar() => new Vector2(1.0f);

        [Benchmark]
        public Vector2 CreateFromScalarXYBenchmark() => new Vector2(1.0f, 2.0f);

        [Benchmark]
        public Vector2 OneBenchmark() => Vector2.One;

        [Benchmark]
        [MemoryRandomization]
        public Vector2 UnitXBenchmark() => Vector2.UnitX;

        [Benchmark]
        public Vector2 UnitYBenchmark() => Vector2.UnitY;

        [Benchmark]
        public Vector2 ZeroBenchmark() => Vector2.Zero;

        [Benchmark]
        public Vector2 AddOperatorBenchmark() => VectorTests.Vector2Value + VectorTests.Vector2Delta;

        [Benchmark]
        public Vector2 DivideByVector2OperatorBenchmark() => VectorTests.Vector2Value / VectorTests.Vector2Delta;

        [Benchmark]
        public Vector2 DivideByScalarOperatorBenchmark() => VectorTests.Vector2Value / 0.5f;

        [Benchmark]
        public bool EqualityOperatorBenchmark() => VectorTests.Vector2Value == VectorTests.Vector2ValueInverted;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => VectorTests.Vector2Value != VectorTests.Vector2ValueInverted;

        [Benchmark]
        public Vector2 MultiplyOperatorBenchmark() => VectorTests.Vector2Value * VectorTests.Vector2Delta;

        [Benchmark]
        public Vector2 MultiplyByScalarOperatorBenchmark() => VectorTests.Vector2Value * 0.5f;

        [Benchmark]
        public Vector2 SubtractOperatorBenchmark() => VectorTests.Vector2Value - VectorTests.Vector2Delta;

        [Benchmark]
        public Vector2 NegateOperatorBenchmark() => -VectorTests.Vector2Value;

        [Benchmark]
        public Vector2 AbsBenchmark() => Vector2.Abs(VectorTests.Vector2Value);

        [Benchmark]
        public Vector2 AddFunctionBenchmark() => Vector2.Add(VectorTests.Vector2Value, VectorTests.Vector2Delta);

        [Benchmark]
        public Vector2 ClampBenchmark() => Vector2.Clamp(VectorTests.Vector2Value, VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public float DistanceBenchmark() => Vector2.Distance(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public float DistanceSquaredBenchmark() => Vector2.DistanceSquared(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public Vector2 DivideByVector2Benchmark() => Vector2.Divide(VectorTests.Vector2Value, VectorTests.Vector2Delta);

        [Benchmark]
        public Vector2 DivideByScalarBenchmark() => Vector2.Divide(VectorTests.Vector2Value, 0.5f);

        [Benchmark]
        public float DotBenchmark() => Vector2.Dot(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public bool EqualsBenchmark() => VectorTests.Vector2Value.Equals(VectorTests.Vector2Value);

        [Benchmark]
        public int GetHashCodeBenchmark() => VectorTests.Vector2Value.GetHashCode();

        [Benchmark]
        public float LengthBenchmark() => VectorTests.Vector2Value.Length();

        [Benchmark]
        public float LengthSquaredBenchmark() => VectorTests.Vector2Value.LengthSquared();

        [Benchmark]
        public Vector2 LerpBenchmark() => Vector2.Lerp(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted, 0.5f);

        [Benchmark]
        public Vector2 MaxBenchmark() => Vector2.Max(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public Vector2 MinBenchmark() => Vector2.Min(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public Vector2 MultiplyFunctionBenchmark() => Vector2.Multiply(VectorTests.Vector2Value, VectorTests.Vector2Delta);

        [Benchmark]
        public Vector2 NegateBenchmark() => Vector2.Negate(VectorTests.Vector2Value);

        [Benchmark]
        [MemoryRandomization]
        public Vector2 NormalizeBenchmark() => Vector2.Normalize(VectorTests.Vector2Value);

        [Benchmark]
        public Vector2 ReflectBenchmark() => Vector2.Reflect(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);

        [Benchmark]
        public Vector2 SquareRootBenchmark() => Vector2.SquareRoot(VectorTests.Vector2Value);

        [Benchmark]
        public Vector2 SubtractFunctionBenchmark() => Vector2.Subtract(VectorTests.Vector2Value, VectorTests.Vector2Delta);

        [Benchmark]
        public Vector2 TransformByMatrix3x2Benchmark() => Vector2.Transform(VectorTests.Vector2Value, Matrix3x2.Identity);

        [Benchmark]
        public Vector2 TransformByMatrix4x4Benchmark() => Vector2.Transform(VectorTests.Vector2Value, Matrix4x4.Identity);

        [Benchmark]
        [MemoryRandomization]
        public Vector2 TransformByQuaternionBenchmark() => Vector2.Transform(VectorTests.Vector2Value, Quaternion.Identity);

        [Benchmark]
        public Vector2 TransformNormalByMatrix3x2Benchmark() => Vector2.TransformNormal(VectorTests.Vector2Value, Matrix3x2.Identity);

        [Benchmark]
        public Vector2 TransformNormalByMatrix4x4Benchmark() => Vector2.TransformNormal(VectorTests.Vector2Value, Matrix4x4.Identity);
    }
}
