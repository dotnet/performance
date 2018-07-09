// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.SIMD)]
    public class Perf_Vector2
    {
        [Benchmark]
        public Vector2 AddFunctionBenchmark()
        {
            var result = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result = Vector2.Add(result, VectorTests.Vector2Delta);
            }

            return result;
        }

        [Benchmark]
        public Vector2 SubtractOperatorBenchmark()
        {
            var result = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result -= VectorTests.Vector2Delta;
            }

            return result;
        }

        [Benchmark]
        public Vector2 SubtractFunctionBenchmark()
        {
            var result = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result = Vector2.Subtract(result, VectorTests.Vector2Delta);
            }

            return result;
        }

        [Benchmark]
        public Vector2 MultiplyOperatorBenchmark()
        {
            var result = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result *= VectorTests.Vector2Delta;
            }

            return result;
        }

        [Benchmark]
        public float DistanceSquaredBenchmark()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector2.DistanceSquared(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float DistanceSquaredJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector2Delta;
                result += Vector2.DistanceSquared(value, VectorTests.Vector2ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public Vector2 MultiplyFunctionBenchmark()
        {
            var result = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result = Vector2.Multiply(result, VectorTests.Vector2Delta);
            }

            return result;
        }

        [Benchmark]
        public float LengthSquaredBenchmark()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = VectorTests.Vector2Value.LengthSquared();
            }

            return result;
        }

        [Benchmark]
        public float LengthSquaredJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector2Delta;
                result += value.LengthSquared();
            }

            return result;
        }

        [Benchmark]
        public int GetHashCodeBenchmark()
        {
            var result = 0;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = VectorTests.Vector2Value.GetHashCode();
            }

            return result;
        }

        [Benchmark]
        public int GetHashCodeJitOptimizeCanaryBenchmark()
        {
            var result = 0;
            var value = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector2Delta;
                result += value.GetHashCode();
            }

            return result;
        }

        [Benchmark]
        public Vector2 AddOperatorBenchmark()
        {
            var result = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result += VectorTests.Vector2Delta;
            }

            return result;
        }

        [Benchmark]
        public float DistanceBenchmark()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector2.Distance(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float DistanceJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector2Delta;
                result += Vector2.Distance(value, VectorTests.Vector2ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float DotBenchmark()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector2.Dot(VectorTests.Vector2Value, VectorTests.Vector2ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float DotJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector2Delta;
                result += Vector2.Dot(value, VectorTests.Vector2ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float LengthBenchmark()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = VectorTests.Vector2Value.Length();
            }

            return result;
        }

        [Benchmark]
        public float LengthJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector2Delta;
                result += value.Length();
            }

            return result;
        }

        [Benchmark]
        public Vector2 SquareRootBenchmark()
        {
            var result = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector2.SquareRoot(result);
            }

            return result;
        }

        [Benchmark]
        public Vector2 SquareRootJitOptimizeCanaryBenchmark()
        {
            var result = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result += Vector2.SquareRoot(result);
            }

            return result;
        }

        [Benchmark]
        public Vector2 NormalizeBenchmark()
        {
            var result = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector2.Normalize(result);
            }

            return result;
        }

        [Benchmark]
        public Vector2 NormalizeJitOptimizeCanaryBenchmark()
        {
            var result = VectorTests.Vector2Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result += Vector2.Normalize(result);
            }

            return result;
        }
    }
}
