// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Matrix3x2
    {
        public const float PI = 3.14159265f;

        [Benchmark]
        public Matrix3x2 CreateFromScalars() => new Matrix3x2(1.1f, 1.2f,
                                                              2.1f, 2.2f,
                                                              3.1f, 3.2f);

        [Benchmark]
        public Matrix3x2 IdentityBenchmark() => Matrix3x2.Identity;

        [Benchmark]
        [MemoryRandomization]
        public bool IsIdentityBenchmark() => Matrix3x2.Identity.IsIdentity;

        [Benchmark]
        public Matrix3x2 AddOperatorBenchmark() => Matrix3x2.Identity + Matrix3x2.Identity;

        [Benchmark]
        public bool EqualityOperatorBenchmark() => Matrix3x2.Identity == Matrix3x2.Identity;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => Matrix3x2.Identity != Matrix3x2.Identity;

        [Benchmark]
        public Matrix3x2 MultiplyByMatrixOperatorBenchmark() => Matrix3x2.Identity * Matrix3x2.Identity;

        [Benchmark]
        public Matrix3x2 MultiplyByScalarOperatorBenchmark() => Matrix3x2.Identity * 0.5f;

        [Benchmark]
        public Matrix3x2 SubtractOperatorBenchmark() => Matrix3x2.Identity - Matrix3x2.Identity;

        [Benchmark]
        public Matrix3x2 NegationOperatorBenchmark() => -Matrix3x2.Identity;

        [Benchmark]
        public Matrix3x2 AddBenchmark() => Matrix3x2.Add(Matrix3x2.Identity, Matrix3x2.Identity);

        [Benchmark]
        public Matrix3x2 CreateRotationBenchmark() => Matrix3x2.CreateRotation(PI / 2.0f);

        [Benchmark]
        public Matrix3x2 CreateRotationWithCenterBenchmark() => Matrix3x2.CreateRotation(PI / 2.0f, Vector2.Zero);

        [Benchmark]
        public Matrix3x2 CreateScaleFromScalarXYBenchmark() => Matrix3x2.CreateScale(1.0f, 2.0f);

        [Benchmark]
        public Matrix3x2 CreateScaleFromScalarWithCenterBenchmark() => Matrix3x2.CreateScale(1.0f, Vector2.Zero);

        [Benchmark]
        public Matrix3x2 CreateScaleFromScalarXYWithCenterBenchmark() => Matrix3x2.CreateScale(1.0f, 2.0f, Vector2.Zero);

        [Benchmark]
        public Matrix3x2 CreateScaleFromScalarBenchmark() => Matrix3x2.CreateScale(1.0f);

        [Benchmark]
        public Matrix3x2 CreateScaleFromVectorBenchmark() => Matrix3x2.CreateScale(Vector2.UnitX);

        [Benchmark]
        public Matrix3x2 CreateScaleFromVectorWithCenterBenchmark() => Matrix3x2.CreateScale(Vector2.UnitX, Vector2.Zero);

        [Benchmark]
        public Matrix3x2 CreateSkewFromScalarXYBenchmark() => Matrix3x2.CreateSkew(1.0f, 2.0f);

        [Benchmark]
        public Matrix3x2 CreateSkewFromScalarXYWithCenterBenchmark() => Matrix3x2.CreateSkew(1.0f, 2.0f, Vector2.Zero);

        [Benchmark]
        public Matrix3x2 CreateTranslationFromVectorBenchmark() => Matrix3x2.CreateTranslation(Vector2.UnitX);

        [Benchmark]
        public Matrix3x2 CreateTranslationFromScalarXY() => Matrix3x2.CreateTranslation(1.0f, 2.0f);

        [Benchmark]
        [MemoryRandomization]
        public bool EqualsBenchmark() => Matrix3x2.Identity.Equals(Matrix3x2.Identity);

        [Benchmark]
        public float GetDeterminantBenchmark() => Matrix3x2.Identity.GetDeterminant();

        [Benchmark]
        public bool InvertBenchmark() => Matrix3x2.Invert(Matrix3x2.Identity, out Matrix3x2 result);

        [Benchmark]
        public Matrix3x2 LerpBenchmark() => Matrix3x2.Lerp(default, Matrix3x2.Identity, 0.5f);

        [Benchmark]
        public Matrix3x2 MultiplyByMatrixBenchmark() => Matrix3x2.Multiply(Matrix3x2.Identity, Matrix3x2.Identity);

        [Benchmark]
        public Matrix3x2 MultiplyByScalarBenchmark() => Matrix3x2.Multiply(Matrix3x2.Identity, 0.5f);

        [Benchmark]
        public Matrix3x2 NegateBenchmark() => Matrix3x2.Negate(Matrix3x2.Identity);

        [Benchmark]
        public Matrix3x2 SubtractBenchmark() => Matrix3x2.Subtract(Matrix3x2.Identity, Matrix3x2.Identity);
    }
}
