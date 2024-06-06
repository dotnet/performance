// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Vector4
    {
        [Benchmark]
        public Vector4 CreateFromScalar() => new Vector4(1.0f);

        [Benchmark]
        public Vector4 CreateFromVector3WithScalarBenchmark() => new Vector4(VectorTests.Vector3Value, 4.0f);

        [Benchmark]
        public Vector4 CreateFromVector2WithScalarBenchmark() => new Vector4(VectorTests.Vector2Value, 3.0f, 4.0f);

        [Benchmark]
        public Vector4 CreateFromScalarXYZWBenchmark() => new Vector4(1.0f, 2.0f, 3.0f, 4.0f);

        [Benchmark]
        [MemoryRandomization]
        public Vector4 OneBenchmark() => Vector4.One;

        [Benchmark]
        public Vector4 UnitXBenchmark() => Vector4.UnitX;

        [Benchmark]
        public Vector4 UnitYBenchmark() => Vector4.UnitY;

        [Benchmark]
        public Vector4 UnitZBenchmark() => Vector4.UnitZ;

        [Benchmark]
        public Vector4 UnitWBenchmark() => Vector4.UnitW;

        [Benchmark]
        public Vector4 ZeroBenchmark() => Vector4.Zero;

        [Benchmark]
        public Vector4 AddOperatorBenchmark() => VectorTests.Vector4Value + VectorTests.Vector4Delta;

        [Benchmark]
        public Vector4 DivideOperatorBenchmark() => VectorTests.Vector4Value / VectorTests.Vector4Delta;

        [Benchmark]
        public Vector4 DivideByScalarOperatorBenchmark() => VectorTests.Vector4Value / 0.5f;

        [Benchmark]
        public bool EqualityOperatorBenchmark() => VectorTests.Vector4Value == VectorTests.Vector4ValueInverted;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => VectorTests.Vector4Value != VectorTests.Vector4ValueInverted;

        [Benchmark]
        public Vector4 MultiplyOperatorBenchmark() => VectorTests.Vector4Value * VectorTests.Vector4Delta;

        [Benchmark]
        public Vector4 MultiplyByScalarOperatorBenchmark() => VectorTests.Vector4Value * 0.5f;

        [Benchmark]
        public Vector4 SubtractOperatorBenchmark() => VectorTests.Vector4Value - VectorTests.Vector4Delta;

        [Benchmark]
        public Vector4 NegateOperatorBenchmark() => -VectorTests.Vector4Value;

        [Benchmark]
        public Vector4 AbsBenchmark() => Vector4.Abs(VectorTests.Vector4Value);

        [Benchmark]
        public Vector4 AddFunctionBenchmark() => Vector4.Add(VectorTests.Vector4Value, VectorTests.Vector4Delta);

        [Benchmark]
        [MemoryRandomization]
        public Vector4 ClampBenchmark() => Vector4.Clamp(VectorTests.Vector4Value, VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);

        [Benchmark]
        public float DistanceBenchmark() => Vector4.Distance(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);

        [Benchmark]
        public float DistanceSquaredBenchmark() => Vector4.DistanceSquared(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);

        [Benchmark]
        public float DistanceSquaredJitOptimizeCanaryBenchmark() => Vector4.DistanceSquared(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);

        [Benchmark]
        public Vector4 DivideBenchmark() => Vector4.Divide(VectorTests.Vector4Value, VectorTests.Vector4Delta);

        [Benchmark]
        public Vector4 DivideByScalarBenchmark() => Vector4.Divide(VectorTests.Vector4Value, 0.5f);

        [Benchmark]
        public float DotBenchmark() => Vector4.Dot(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);

        [Benchmark]
        public bool EqualsBenchmark() => VectorTests.Vector4Value.Equals(VectorTests.Vector4ValueInverted);

        [Benchmark]
        public int GetHashCodeBenchmark() => VectorTests.Vector4Value.GetHashCode();

        [Benchmark]
        public float LengthBenchmark() => VectorTests.Vector4Value.Length();

        [Benchmark]
        public float LengthSquaredBenchmark() => VectorTests.Vector4Value.LengthSquared();

        [Benchmark]
        public Vector4 LerpBenchmark() => Vector4.Lerp(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted, 0.5f);

        [Benchmark]
        public Vector4 MaxBenchmark() => Vector4.Max(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);

        [Benchmark]
        public Vector4 MinBenchmark() => Vector4.Min(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);

        [Benchmark]
        public Vector4 MultiplyFunctionBenchmark() => Vector4.Multiply(VectorTests.Vector4Value, VectorTests.Vector4Delta);

        [Benchmark]
        public Vector4 MultiplyByScalarBenchmark() => Vector4.Multiply(VectorTests.Vector4Value, 0.5f);

        [Benchmark]
        public Vector4 NegateBenchmark() => Vector4.Negate(VectorTests.Vector4Value);

        [Benchmark]
        public Vector4 NormalizeBenchmark() => Vector4.Normalize(VectorTests.Vector4Value);

        [Benchmark]
        public Vector4 SquareRootBenchmark() => Vector4.SquareRoot(VectorTests.Vector4Value);

        [Benchmark]
        public Vector4 SubtractFunctionBenchmark() => Vector4.Subtract(VectorTests.Vector4Value, VectorTests.Vector4Delta);

        [Benchmark]
        public Vector4 TransformByQuaternionBenchmark() => Vector4.Transform(VectorTests.Vector3Value, Quaternion.Identity);

        [Benchmark]
        public Vector4 TransformByMatrix4x4Benchmark() => Vector4.Transform(VectorTests.Vector3Value, Matrix4x4.Identity);

        [Benchmark]
        public Vector4 TransformVector3ByQuaternionBenchmark() => Vector4.Transform(VectorTests.Vector3Value, Quaternion.Identity);

        [Benchmark]
        public Vector4 TransformVector3ByMatrix4x4Benchmark() => Vector4.Transform(VectorTests.Vector3Value, Matrix4x4.Identity);

        [Benchmark]
        public Vector4 TransformVector2ByQuaternionBenchmark() => Vector4.Transform(VectorTests.Vector2Value, Quaternion.Identity);

        [Benchmark]
        public Vector4 TransformVector2ByMatrix4x4Benchmark() => Vector4.Transform(VectorTests.Vector2Value, Matrix4x4.Identity);
    }
}
