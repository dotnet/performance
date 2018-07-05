// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.Numerics.Tests
{
    public class Perf_Vector3
    {
        [Benchmark]
        public Vector3 AddFunctionBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result = Vector3.Add(result, VectorTests.Vector3Delta);
            }

            return result;
        }

        [Benchmark]
        public Vector3 MultiplyOperatorBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result *= VectorTests.Vector3Delta;
            }

            return result;
        }

        [Benchmark]
        public Vector3 SubtractOperatorBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result -= VectorTests.Vector3Delta;
            }

            return result;
        }

        [Benchmark]
        public Vector3 SubtractFunctionBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result = Vector3.Subtract(result, VectorTests.Vector3Delta);
            }

            return result;
        }

        [Benchmark]
        public Vector3 MultiplyFunctionBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result = Vector3.Multiply(result, VectorTests.Vector3Delta);
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
                result = Vector3.DistanceSquared(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float DistanceSquaredJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += Vector3.DistanceSquared(value, VectorTests.Vector3ValueInverted);
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
                result = VectorTests.Vector3Value.LengthSquared();
            }

            return result;
        }

        [Benchmark]
        public float LengthSquaredJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
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
                result = VectorTests.Vector3Value.GetHashCode();
            }

            return result;
        }

        [Benchmark]
        public int GetHashCodeJitOptimizeCanaryBenchmark()
        {
            var result = 0;
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += value.GetHashCode();
            }

            return result;
        }

        [Benchmark]
        public Vector3 AddOperatorBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result += VectorTests.Vector3Delta;
            }

            return result;
        }

        [Benchmark]
        public Vector3 SquareRootBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector3.SquareRoot(result);
            }

            return result;
        }

        [Benchmark]
        public Vector3 SquareRootJitOptimizeCanaryBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result += Vector3.SquareRoot(result);
            }

            return result;
        }

        [Benchmark]
        public Vector3 NormalizeBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector3.Normalize(result);
            }

            return result;
        }

        [Benchmark]
        public Vector3 NormalizeJitOptimizeCanaryBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result += Vector3.Normalize(result);
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
                result = Vector3.Dot(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float DotJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += Vector3.Dot(value, VectorTests.Vector3ValueInverted);
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
                result = Vector3.Distance(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);
            }

            return result;
        }

        [Benchmark]
        public float DistanceJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += Vector3.Distance(value, VectorTests.Vector3ValueInverted);
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
                result = VectorTests.Vector3Value.Length();
            }

            return result;
        }

        [Benchmark]
        public float LengthJitOptimizeCanaryBenchmark()
        {
            var result = 0.0f;
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += value.Length();
            }

            return result;
        }

        [Benchmark]
        public Vector3 CrossBenchmark()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < VectorTests.DefaultInnerIterationsCount; iteration++)
            {
                result = Vector3.Cross(result, VectorTests.Vector3ValueInverted);
            }

            return result;
        }
    }
}
