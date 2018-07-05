// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BdnDtos;
using BenchmarkDotNet.Attributes;
using Microsoft.Xunit.Performance;

namespace System.Numerics.Tests
{
    public static class Perf_Vector4
    {
        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void AddFunctionBenchmark()
        {
            var expectedResult = VectorTests.Vector4Value;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector4 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = AddFunctionTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector4 AddFunctionTest()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result = Vector4.Add(result, VectorTests.Vector4Delta);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void SubtractOperatorBenchmark()
        {
            var expectedResult = VectorTests.Vector4Value;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector4 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = SubtractOperatorTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector4 SubtractOperatorTest()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result -= VectorTests.Vector4Delta;
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DistanceSquaredBenchmark()
        {
            const float expectedResult = 16.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = DistanceSquaredTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float DistanceSquaredTest()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector4.DistanceSquared(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DistanceSquaredJitOptimizeCanaryBenchmark()
        {
            const float expectedResult = 268435456.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = DistanceSquaredJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float DistanceSquaredJitOptimizeCanaryTest()
        {
            var result = 0.0f;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += Vector4.DistanceSquared(value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void MultiplyOperatorBenchmark()
        {
            var expectedResult = Vector4.Zero;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector4 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = MultiplyOperatorTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector4 MultiplyOperatorTest()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result *= VectorTests.Vector4Delta;
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void SubtractFunctionBenchmark()
        {
            var expectedResult = VectorTests.Vector4Value;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector4 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = SubtractFunctionTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector4 SubtractFunctionTest()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result = Vector4.Subtract(result, VectorTests.Vector4Delta);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void MultiplyFunctionBenchmark()
        {
            var expectedResult = Vector4.Zero;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector4 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = MultiplyFunctionTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector4 MultiplyFunctionTest()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result = Vector4.Multiply(result, VectorTests.Vector4Delta);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void LengthSquaredBenchmark()
        {
            const float expectedResult = 4.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = LengthSquaredTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float LengthSquaredTest()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = VectorTests.Vector4Value.LengthSquared();
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void LengthSquaredJitOptimizeCanaryBenchmark()
        {
            const float expectedResult = 67108864.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = LengthSquaredJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float LengthSquaredJitOptimizeCanaryTest()
        {
            var result = 0.0f;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += value.LengthSquared();
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void GetHashCodeBenchmark()
        {
            const int expectedResult = 536896558;

            foreach (var iteration in Benchmark.Iterations)
            {
                int actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = GetHashCodeTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static int GetHashCodeTest()
        {
            var result = 0;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = VectorTests.Vector4Value.GetHashCode();
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void GetHashCodeJitOptimizeCanaryBenchmark()
        {
            const int expectedResult = 504524288;

            foreach (var iteration in Benchmark.Iterations)
            {
                int actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = GetHashCodeJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static int GetHashCodeJitOptimizeCanaryTest()
        {
            var result = 0;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += value.GetHashCode();
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void AddOperatorBenchmark()
        {
            var expectedResult = VectorTests.Vector4Value;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector4 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = AddOperatorTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector4 AddOperatorTest()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result += VectorTests.Vector4Delta;
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void SquareRootBenchmark()
        {
            var expectedResult = new Vector4(float.NaN, 1.0f, float.NaN, 1.0f);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector4 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = SquareRootTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector4 SquareRootTest()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector4.SquareRoot(result);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void SquareRootJitOptimizeCanaryBenchmark()
        {
            var expectedResult = new Vector4(float.NaN, 2.81474977e+14f, float.NaN, 2.81474977e+14f);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector4 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = SquareRootJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector4 SquareRootJitOptimizeCanaryTest()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result += Vector4.SquareRoot(result);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void NormalizeBenchmark()
        {
            var expectedResult = new Vector4(-0.5f, 0.5f, -0.5f, 0.5f);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector4 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = NormalizeTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector4 NormalizeTest()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector4.Normalize(result);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void NormalizeJitOptimizeCanaryBenchmark()
        {
            var expectedResult = new Vector4(-8388608.0f, 8388608.0f, -8388608.0f, 8388608.0f);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector4 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = NormalizeJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector4 NormalizeJitOptimizeCanaryTest()
        {
            var result = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result += Vector4.Normalize(result);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DistanceBenchmark()
        {
            const float expectedResult = 4.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = DistanceTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float DistanceTest()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector4.Distance(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DistanceJitOptimizeCanaryBenchmark()
        {
            const float expectedResult = 67108864.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = DistanceJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float DistanceJitOptimizeCanaryTest()
        {
            var result = 0.0f;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += Vector4.Distance(value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void LengthBenchmark()
        {
            const float expectedResult = 2.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = LengthTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float LengthTest()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = VectorTests.Vector4Value.Length();
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void LengthJitOptimizeCanaryBenchmark()
        {
            const float expectedResult = 33554432.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = LengthJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float LengthJitOptimizeCanaryTest()
        {
            var result = 0.0f;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += value.Length();
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DotBenchmark()
        {
            const float expectedResult = -4.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = DotTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float DotTest()
        {
            var result = 0.0f;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector4.Dot(VectorTests.Vector4Value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DotJitOptimizeCanaryBenchmark()
        {
            const float expectedResult = -67108864.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = DotJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static float DotJitOptimizeCanaryTest()
        {
            var result = 0.0f;
            var value = VectorTests.Vector4Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector4Delta;
                result += Vector4.Dot(value, VectorTests.Vector4ValueInverted);
            }

            return result;
        }
    }
}
