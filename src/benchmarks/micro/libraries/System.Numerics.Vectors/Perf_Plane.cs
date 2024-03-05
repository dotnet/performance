// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Plane
    {
        [Benchmark]
        public Plane CreateFromVector4Benchmark() => new Plane(Vector4.UnitX);

        [Benchmark]
        [MemoryRandomization]
        public Plane CreateFromVector3WithScalarDBenchmark() => new Plane(Vector3.UnitX, 4.0f);

        [Benchmark]
        [MemoryRandomization]
        public Plane CreateFromScalarXYZDBenchmark() => new Plane(1.0f, 2.0f, 3.0f, 4.0f);

        [Benchmark]
        public bool EqualityOperatorBenchmark() => default == VectorTests.PlaneValue;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => default != VectorTests.PlaneValue;

        [Benchmark]
        public Plane CreateFromVerticesBenchmark() => Plane.CreateFromVertices(Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);

        [Benchmark]
        public float DotBenchmark() => Plane.Dot(VectorTests.PlaneValue, Vector4.UnitX);

        [Benchmark]
        public float DotCoordinateBenchmark() => Plane.DotCoordinate(VectorTests.PlaneValue, Vector3.UnitX);

        [Benchmark]
        public float DotNormalBenchmark() => Plane.DotNormal(VectorTests.PlaneValue, Vector3.UnitX);

        [Benchmark]
        public bool EqualsBenchmark() => VectorTests.PlaneValue.Equals(VectorTests.PlaneValue);

        [Benchmark]
        public Plane NormalizeBenchmark() => Plane.Normalize(VectorTests.PlaneValue);

        [Benchmark]
        public Plane TransformByMatrix4x4Benchmark() => Plane.Transform(VectorTests.PlaneValue, Matrix4x4.Identity);

        [Benchmark]
        public Plane TransformByQuaternionBenchmark() => Plane.Transform(VectorTests.PlaneValue, Quaternion.Identity);
    }
}
