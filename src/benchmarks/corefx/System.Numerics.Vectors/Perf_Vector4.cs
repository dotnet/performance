// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.Numerics.Tests
{
    public class Perf_Vector4
    {
        [Benchmark]
        public Vector4 AddFunctionBenchmark()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result = Vector4.Add(result, VectorTests.Vector4Delta);
            }

            return result;
        }

        [Benchmark]
        public Vector4 SubtractOperatorBenchmark()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result -= VectorTests.Vector4Delta;
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
                result = Vector4.DistanceSquared(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float DistanceSquaredJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += Vector4.DistanceSquared(value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public Vector4 MultiplyOperatorBenchmark()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result *= VectorTests.Vector4Delta;
            }

            return result;
        }

        [Benchmark]
        public Vector4 SubtractFunctionBenchmark()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result = Vector4.Subtract(result, VectorTests.Vector4Delta);
            }

            return result;
        }

        [Benchmark]
        public Vector4 MultiplyFunctionBenchmark()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result = Vector4.Multiply(result, VectorTests.Vector4Delta);
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
                result = VectorTests.Vector4Value.LengthSquared();
            }

            return result;
        }

        [Benchmark]
        public float LengthSquaredJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
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
                result = VectorTests.Vector4Value.GetHashCode();
            }

            return result;
        }

        [Benchmark]
        public int GetHashCodeJitOptimizeCanaryBenchmark()
        {
            var result = 0;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += value.GetHashCode();
            }

            return result;
        }

        [Benchmark]
        public Vector4 AddOperatorBenchmark()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result += VectorTests.Vector4Delta;
            }

            return result;
        }

        [Benchmark]
        public Vector4 SquareRootBenchmark()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector4.SquareRoot(result);
            }

            return result;
        }

        [Benchmark]
        public Vector4 SquareRootJitOptimizeCanaryBenchmark()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result += Vector4.SquareRoot(result);
            }

            return result;
        }

        [Benchmark]
        public Vector4 NormalizeBenchmark()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector4.Normalize(result);
            }

            return result;
        }

        [Benchmark]
        public Vector4 NormalizeJitOptimizeCanaryBenchmark()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result += Vector4.Normalize(result);
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
                result = Vector4.Distance(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float DistanceJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += Vector4.Distance(value, VectorTests.Vector4ValueInverted);
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
                result = VectorTests.Vector4Value.Length();
            }

            return result;
        }

        [Benchmark]
        public float LengthJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += value.Length();
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
                result = Vector4.Dot(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float DotJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += Vector4.Dot(value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }
    }
}
