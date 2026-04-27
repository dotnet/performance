// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Quaternion
    {
        public const float PI = 3.14159265f;

        [Benchmark]
        public Quaternion CreateFromVector3WithScalarBenchmark() => new Quaternion(Vector3.UnitX, 4.0f);

        [Benchmark]
        public Quaternion CreateFromScalarXYZWBenchmark() => new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);

        [Benchmark]
        public Quaternion IdentityBenchmark() => Quaternion.Identity;

        [Benchmark]
        public bool IsIdentityBenchmark() => Quaternion.Identity.IsIdentity;

        [Benchmark]
        public Quaternion AddOperatorBenchmark() => Quaternion.Identity + Quaternion.Identity;

        [Benchmark]
        public Quaternion DivisionOperatorBenchmark() => Quaternion.Identity / Quaternion.Identity;

        [Benchmark]
        public bool EqualityOperatorBenchmark() => Quaternion.Identity == Quaternion.Identity;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => Quaternion.Identity != Quaternion.Identity;

        [Benchmark]
        public Quaternion MultiplyByQuaternionOperatorBenchmark() => Quaternion.Identity * Quaternion.Identity;

        [Benchmark]
        public Quaternion MultiplyByScalarOperatorBenchmark() => Quaternion.Identity * 0.5f;

        [Benchmark]
        public Quaternion SubtractionOperatorBenchmark() => Quaternion.Identity - Quaternion.Identity;

        [Benchmark]
        public Quaternion NegationOperatorBenchmark() => -Quaternion.Identity;

        [Benchmark]
        public Quaternion AddBenchmark() => Quaternion.Add(Quaternion.Identity, Quaternion.Identity);

        [Benchmark]
        public Quaternion ConcatenateBenchmark() => Quaternion.Concatenate(Quaternion.Identity, Quaternion.Identity);

        [Benchmark]
        public Quaternion ConjugateBenchmark() => Quaternion.Conjugate(Quaternion.Identity);

        [Benchmark]
        public Quaternion CreateFromAxisAngleBenchmark() => Quaternion.CreateFromAxisAngle(Vector3.UnitX, PI / 2.0f);

        [Benchmark]
        public Quaternion CreateFromRotationMatrixBenchmark() => Quaternion.CreateFromRotationMatrix(Matrix4x4.Identity);

        [Benchmark]
        public Quaternion CreateFromYawPitchRollBenchmark() => Quaternion.CreateFromYawPitchRoll(PI, PI / 2.0f, PI / 4.0f);

        [Benchmark]
        [MemoryRandomization]
        public Quaternion DivideBenchmark() => Quaternion.Add(Quaternion.Identity, Quaternion.Identity);

        [Benchmark]
        public float DotBenchmark() => Quaternion.Dot(Quaternion.Identity, Quaternion.Identity);

        [Benchmark]
        public bool EqualsBenchmark() => Quaternion.Identity.Equals(Quaternion.Identity);

        [Benchmark]
        public Quaternion InverseBenchmark() => Quaternion.Inverse(Quaternion.Identity);

        [Benchmark]
        public float LengthBenchmark() => Quaternion.Identity.Length();

        [Benchmark]
        public float LengthSquaredBenchmark() => Quaternion.Identity.LengthSquared();

        [Benchmark]
        public Quaternion LerpBenchmark() => Quaternion.Lerp(default, Quaternion.Identity, 0.5f);

        [Benchmark]
        public Quaternion MultiplyByQuaternionBenchmark() => Quaternion.Multiply(Quaternion.Identity, Quaternion.Identity);

        [Benchmark]
        public Quaternion MultiplyByScalarBenchmark() => Quaternion.Multiply(Quaternion.Identity, 0.5f);

        [Benchmark]
        public Quaternion NegateBenchmark() => Quaternion.Negate(Quaternion.Identity);

        [Benchmark]
        public Quaternion NormalizeBenchmark() => Quaternion.Normalize(Quaternion.Identity);

        [Benchmark]
        public Quaternion SlerpBenchmark() => Quaternion.Lerp(default, Quaternion.Identity, 0.5f);

        [Benchmark]
        public Quaternion SubtractBenchmark() => Quaternion.Subtract(Quaternion.Identity, Quaternion.Identity);
    }
}
