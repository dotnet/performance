// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Matrix4x4
    {
        public const float PI = 3.14159265f;

        [Benchmark]
        public Matrix4x4 CreateFromMatrix3x2() => new Matrix4x4(Matrix3x2.Identity);

        [Benchmark]
        public Matrix4x4 CreateFromScalars() => new Matrix4x4(1.1f, 1.2f, 1.3f, 1.4f,
                                                              2.1f, 2.2f, 2.3f, 2.4f,
                                                              3.1f, 3.2f, 3.3f, 3.4f,
                                                              4.1f, 4.2f, 4.3f, 4.4f);

        [Benchmark]
        public Matrix4x4 IdentityBenchmark() => Matrix4x4.Identity;

        [Benchmark]
        public bool IsIdentityBenchmark() => Matrix4x4.Identity.IsIdentity;

        [Benchmark]
        public Vector3 TranslationBenchmark() => Matrix4x4.Identity.Translation;

        [Benchmark]
        public Matrix4x4 AddOperatorBenchmark() => Matrix4x4.Identity + Matrix4x4.Identity;

        [Benchmark]
        public bool EqualityOperatorBenchmark() => Matrix4x4.Identity == Matrix4x4.Identity;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => Matrix4x4.Identity != Matrix4x4.Identity;

        [Benchmark]
        public Matrix4x4 MultiplyByMatrixOperatorBenchmark() => Matrix4x4.Identity * Matrix4x4.Identity;

        [Benchmark]
        public Matrix4x4 MultiplyByScalarOperatorBenchmark() => Matrix4x4.Identity * 0.5f;

        [Benchmark]
        public Matrix4x4 SubtractOperatorBenchmark() => Matrix4x4.Identity - Matrix4x4.Identity;

        [Benchmark]
        public Matrix4x4 NegationOperatorBenchmark() => -Matrix4x4.Identity;

        [Benchmark]
        public Matrix4x4 AddBenchmark() => Matrix4x4.Add(Matrix4x4.Identity, Matrix4x4.Identity);

        [Benchmark]
        public Matrix4x4 CreateBillboardBenchmark() => Matrix4x4.CreateBillboard(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY, Vector3.UnitZ);

        [Benchmark]
        public Matrix4x4 CreateConstrainedBillboardBenchmark() => Matrix4x4.CreateConstrainedBillboard(Vector3.Zero, Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);

        [Benchmark]
        public Matrix4x4 CreateFromAxisAngleBenchmark() => Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, PI / 2.0f);

        [Benchmark]
        public Matrix4x4 CreateFromQuaternionBenchmark() => Matrix4x4.CreateFromQuaternion(Quaternion.Identity);

        [Benchmark]
        public Matrix4x4 CreateFromYawPitchRollBenchmarkBenchmark() => Matrix4x4.CreateFromYawPitchRoll(PI, PI / 2.0f, PI / 4.0f);

        [Benchmark]
        public Matrix4x4 CreateLookAtBenchmark() => Matrix4x4.CreateLookAt(Vector3.UnitZ, Vector3.Zero, Vector3.UnitY);

        [Benchmark]
        public Matrix4x4 CreateOrthographicBenchmark() => Matrix4x4.CreateOrthographic(1920.0f, 1080.0f, 0.001f, 100.0f);

        [Benchmark]
        public Matrix4x4 CreateOrthographicOffCenterBenchmark() => Matrix4x4.CreateOrthographicOffCenter(640.0f, 1920.0f, 1080.0f, 360.0f, 0.001f, 100.0f);

        [Benchmark]
        public Matrix4x4 CreatePerspectiveBenchmark() => Matrix4x4.CreatePerspective(1920.0f, 1080.0f, 0.001f, 100.0f);

        [Benchmark]
        public Matrix4x4 CreatePerspectiveFieldOfViewBenchmark() => Matrix4x4.CreatePerspectiveFieldOfView(PI / 2.0f, 1920.0f / 1080.0f, 0.001f, 100.0f);

        [Benchmark]
        public Matrix4x4 CreatePerspectiveOffCenterBenchmark() => Matrix4x4.CreatePerspectiveOffCenter(640.0f, 1920.0f, 1080.0f, 360.0f, 0.001f, 100.0f);

        [Benchmark]
        public Matrix4x4 CreateReflectionBenchmark() => Matrix4x4.CreateReflection(default);

        [Benchmark]
        public Matrix4x4 CreateRotationXBenchmark() => Matrix4x4.CreateRotationX(PI / 2.0f);

        [Benchmark]
        public Matrix4x4 CreateRotationXWithCenterBenchmark() => Matrix4x4.CreateRotationX(PI / 2.0f, Vector3.Zero);

        [Benchmark]
        public Matrix4x4 CreateRotationYBenchmark() => Matrix4x4.CreateRotationY(PI / 2.0f);

        [Benchmark]
        public Matrix4x4 CreateRotationYWithCenterBenchmark() => Matrix4x4.CreateRotationY(PI / 2.0f, Vector3.Zero);

        [Benchmark]
        public Matrix4x4 CreateRotationZBenchmark() => Matrix4x4.CreateRotationY(PI / 2.0f);

        [Benchmark]
        public Matrix4x4 CreateRotationZWithCenterBenchmark() => Matrix4x4.CreateRotationY(PI / 2.0f, Vector3.Zero);

        [Benchmark]
        public Matrix4x4 CreateScaleFromVectorBenchmark() => Matrix4x4.CreateScale(Vector3.UnitX);

        [Benchmark]
        public Matrix4x4 CreateScaleFromVectorWithCenterBenchmark() => Matrix4x4.CreateScale(Vector3.UnitX, Vector3.Zero);

        [Benchmark]
        public Matrix4x4 CreateScaleFromScalarBenchmark() => Matrix4x4.CreateScale(1.0f);

        [Benchmark]
        public Matrix4x4 CreateScaleFromScalarWithCenterBenchmark() => Matrix4x4.CreateScale(1.0f, Vector3.Zero);

        [Benchmark]
        public Matrix4x4 CreateScaleFromScalarXYZBenchmark() => Matrix4x4.CreateScale(1.0f, 2.0f, 3.0f, Vector3.Zero);

        [Benchmark]
        public Matrix4x4 CreateScaleFromScalarXYZWithCenterBenchmark() => Matrix4x4.CreateScale(1.0f, 2.0f, 3.0f, Vector3.Zero);

        [Benchmark]
        public Matrix4x4 CreateShadowBenchmark() => Matrix4x4.CreateShadow(Vector3.UnitY, default);

        [Benchmark]
        public Matrix4x4 CreateTranslationFromVectorBenchmark() => Matrix4x4.CreateTranslation(Vector3.UnitX);

        [Benchmark]
        public Matrix4x4 CreateTranslationFromScalarXYZ() => Matrix4x4.CreateTranslation(1.0f, 2.0f, 3.0f);

        [Benchmark]
        public Matrix4x4 CreateWorldBenchmark() => Matrix4x4.CreateWorld(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);

        [Benchmark]
        public bool DecomposeBenchmark() => Matrix4x4.Decompose(Matrix4x4.Identity, out Vector3 scale, out Quaternion rotation, out Vector3 translation);

        [Benchmark]
        public bool EqualsBenchmark() => Matrix4x4.Identity.Equals(Matrix4x4.Identity);

        [Benchmark]
        public float GetDeterminantBenchmark() => Matrix4x4.Identity.GetDeterminant();

        [Benchmark]
        public bool InvertBenchmark() => Matrix4x4.Invert(Matrix4x4.Identity, out Matrix4x4 result);

        [Benchmark]
        public Matrix4x4 LerpBenchmark() => Matrix4x4.Lerp(default, Matrix4x4.Identity, 0.5f);

        [Benchmark]
        public Matrix4x4 MultiplyByMatrixBenchmark() => Matrix4x4.Multiply(Matrix4x4.Identity, Matrix4x4.Identity);

        [Benchmark]
        public Matrix4x4 MultiplyByScalarBenchmark() => Matrix4x4.Multiply(Matrix4x4.Identity, 0.5f);

        [Benchmark]
        public Matrix4x4 NegateBenchmark() => Matrix4x4.Negate(Matrix4x4.Identity);

        [Benchmark]
        [MemoryRandomization]
        public Matrix4x4 SubtractBenchmark() => Matrix4x4.Subtract(Matrix4x4.Identity, Matrix4x4.Identity);

        [Benchmark]
        public Matrix4x4 TransformBenchmark() => Matrix4x4.Transform(Matrix4x4.Identity, Quaternion.Identity);

        [Benchmark]
        public Matrix4x4 Transpose() => Matrix4x4.Transpose(Matrix4x4.Identity);
    }
}
