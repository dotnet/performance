// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Vector3
    {
        [Benchmark]
        public Vector3 CreateFromScalar() => new Vector3(1.0f);

        [Benchmark]
        public Vector3 CreateFromVector2WithScalarBenchmark() => new Vector3(VectorTests.Vector2Value, 3.0f);

        [Benchmark]
        public Vector3 CreateFromScalarXYZBenchmark() => new Vector3(1.0f, 2.0f, 3.0f);

        [Benchmark]
        public Vector3 OneBenchmark() => Vector3.One;

        [Benchmark]
        public Vector3 UnitXBenchmark() => Vector3.UnitX;

        [Benchmark]
        [MemoryRandomization]
        public Vector3 UnitYBenchmark() => Vector3.UnitY;

        [Benchmark]
        [MemoryRandomization]
        public Vector3 UnitZBenchmark() => Vector3.UnitZ;

        [Benchmark]
        public Vector3 ZeroBenchmark() => Vector3.Zero;

        [Benchmark]
        public Vector3 AddOperatorBenchmark() => VectorTests.Vector3Value + VectorTests.Vector3Delta;

        [Benchmark]
        public Vector3 DivideByVector3OperatorBenchmark() => VectorTests.Vector3Value / VectorTests.Vector3Delta;

        [Benchmark]
        public Vector3 DivideByScalarOperatorBenchmark() => VectorTests.Vector3Value / 0.5f;

        [Benchmark]
        public bool EqualityOperatorBenchmark() => VectorTests.Vector3Value == VectorTests.Vector3ValueInverted;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => VectorTests.Vector3Value != VectorTests.Vector3ValueInverted;

        [Benchmark]
        public Vector3 MultiplyOperatorBenchmark() => VectorTests.Vector3Value * VectorTests.Vector3Delta;

        [Benchmark]
        public Vector3 MultiplyByScalarOperatorBenchmark() => VectorTests.Vector3Value * 0.5f;

        [Benchmark]
        public Vector3 SubtractOperatorBenchmark() => VectorTests.Vector3Value - VectorTests.Vector3Delta;

        [Benchmark]
        public Vector3 NegateOperatorBenchmark() => -VectorTests.Vector3Value;

        [Benchmark]
        public Vector3 AbsBenchmark() => Vector3.Abs(VectorTests.Vector3Value);

        [Benchmark]
        public Vector3 AddFunctionBenchmark() => Vector3.Add(VectorTests.Vector3Value, VectorTests.Vector3Delta);

        [Benchmark]
        public Vector3 ClampBenchmark() => Vector3.Clamp(VectorTests.Vector3Value, VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public Vector3 CrossBenchmark() => Vector3.Cross(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public float DistanceBenchmark() => Vector3.Distance(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public float DistanceSquaredBenchmark() => Vector3.DistanceSquared(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public Vector3 DivideByVector3Benchmark() => Vector3.Divide(VectorTests.Vector3Value, VectorTests.Vector3Delta);

        [Benchmark]
        public Vector3 DivideByScalarBenchmark() => Vector3.Divide(VectorTests.Vector3Value, 0.5f);

        [Benchmark]
        public float DotBenchmark() => Vector3.Dot(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public bool EqualsBenchmark() => VectorTests.Vector3Value.Equals(VectorTests.Vector3ValueInverted);

        [Benchmark]
        public int GetHashCodeBenchmark() => VectorTests.Vector3Value.GetHashCode();

        [Benchmark]
        public float LengthBenchmark() => VectorTests.Vector3Value.Length();

        [Benchmark]
        public float LengthSquaredBenchmark() => VectorTests.Vector3Value.LengthSquared();

        [Benchmark]
        public Vector3 LerpBenchmark() => Vector3.Lerp(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted, 0.5f);

        [Benchmark]
        public Vector3 MaxBenchmark() => Vector3.Max(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public Vector3 MinBenchmark() => Vector3.Min(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public Vector3 MultiplyFunctionBenchmark() => Vector3.Multiply(VectorTests.Vector3Value, VectorTests.Vector3Delta);

        [Benchmark]
        public Vector3 MultiplyByScalarBenchmark() => Vector3.Multiply(VectorTests.Vector3Value, 0.5f);
        
        [Benchmark]
        public Vector3 NegateBenchmark() => Vector3.Negate(VectorTests.Vector3Value);

        [Benchmark]
        public Vector3 NormalizeBenchmark() => Vector3.Normalize(VectorTests.Vector3Value);

        [Benchmark]
        public Vector3 ReflectBenchmark() => Vector3.Reflect(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);

        [Benchmark]
        public Vector3 SquareRootBenchmark() => Vector3.SquareRoot(VectorTests.Vector3Value);

        [Benchmark]
        public Vector3 SubtractFunctionBenchmark() => Vector3.Subtract(VectorTests.Vector3Value, VectorTests.Vector3Delta);

        [Benchmark]
        public Vector3 TransformByMatrix4x4Benchmark() => Vector3.Transform(VectorTests.Vector3Value, Matrix4x4.Identity);

        [Benchmark]
        public Vector3 TransformByQuaternionBenchmark() => Vector3.Transform(VectorTests.Vector3Value, Quaternion.Identity);

        [Benchmark]
        public Vector3 TransformNormalByMatrix4x4Benchmark() => Vector3.TransformNormal(VectorTests.Vector3Value, Matrix4x4.Identity);
    }
}
