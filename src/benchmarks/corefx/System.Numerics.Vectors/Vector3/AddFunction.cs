// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BdnDtos;
using BenchmarkDotNet.Attributes;
using Microsoft.Xunit.Performance;

namespace System.Numerics.Tests
{
    public static class Perf_Vector3
    {
        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void AddFunctionBenchmark()
        {
            var expectedResult = VectorTests.Vector3Value;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = AddFunctionTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 AddFunctionTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result = Vector3.Add(result, VectorTests.Vector3Delta);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void MultiplyOperatorBenchmark()
        {
            var expectedResult = Vector3.Zero;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = MultiplyOperatorTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 MultiplyOperatorTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result *= VectorTests.Vector3Delta;
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void SubtractOperatorBenchmark()
        {
            var expectedResult = VectorTests.Vector3Value;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = SubtractOperatorTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 SubtractOperatorTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result -= VectorTests.Vector3Delta;
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void SubtractFunctionBenchmark()
        {
            var expectedResult = VectorTests.Vector3Value;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = SubtractFunctionTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 SubtractFunctionTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result = Vector3.Subtract(result, VectorTests.Vector3Delta);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void MultiplyFunctionBenchmark()
        {
            var expectedResult = Vector3.Zero;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = MultiplyFunctionTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 MultiplyFunctionTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result = Vector3.Multiply(result, VectorTests.Vector3Delta);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DistanceSquaredBenchmark()
        {
            const float expectedResult = 12.0f;

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
                result = Vector3.DistanceSquared(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);
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
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += Vector3.DistanceSquared(value, VectorTests.Vector3ValueInverted);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void LengthSquaredBenchmark()
        {
            const float expectedResult = 3.0f;

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
                result = VectorTests.Vector3Value.LengthSquared();
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
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += value.LengthSquared();
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void GetHashCodeBenchmark()
        {
            const int expectedResult = 796918537;

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
                result = VectorTests.Vector3Value.GetHashCode();
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void GetHashCodeJitOptimizeCanaryBenchmark()
        {
            const int expectedResult = -1756894976;

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
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += value.GetHashCode();
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void AddOperatorBenchmark()
        {
            var expectedResult = VectorTests.Vector3Value;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = AddOperatorTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 AddOperatorTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result += VectorTests.Vector3Delta;
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void SquareRootBenchmark()
        {
            var expectedResult = new Vector3(float.NaN, 1.0f, float.NaN);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = SquareRootTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 SquareRootTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector3.SquareRoot(result);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void SquareRootJitOptimizeCanaryBenchmark()
        {
            var expectedResult = new Vector3(float.NaN, 2.81474977e+14f, float.NaN);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = SquareRootJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 SquareRootJitOptimizeCanaryTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result += Vector3.SquareRoot(result);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void NormalizeBenchmark()
        {
            var expectedResult = new Vector3(-0.577350318f, 0.577350318f, -0.577350318f);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = NormalizeTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 NormalizeTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                // The inputs aren't being changed and the output is being reset with each iteration, so a future
                // optimization could potentially throw away everything except for the final call. This would break
                // the perf test. The JitOptimizeCanary code below does modify the inputs and consume each output.
                result = Vector3.Normalize(result);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void NormalizeJitOptimizeCanaryBenchmark()
        {
            var expectedResult = new Vector3(-16777216.0f, 16777216.0f, -16777216.0f);

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 actualResult;

                using (iteration.StartMeasurement())
                {
                    actualResult = NormalizeJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, actualResult);
            }
        }

        public static Vector3 NormalizeJitOptimizeCanaryTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result += Vector3.Normalize(result);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DotBenchmark()
        {
            const float expectedResult = -3.0f;

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
                result = Vector3.Dot(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DotJitOptimizeCanaryBenchmark()
        {
            const float expectedResult = -67108864.0f;

            foreach (var iteration in Benchmark.Iterations)
            {
                float result;

                using (iteration.StartMeasurement())
                {
                    result = DotJitOptimizeCanaryTest();
                }

                VectorTests.AssertEqual(expectedResult, result);
            }
        }

        public static float DotJitOptimizeCanaryTest()
        {
            var result = 0.0f;
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += Vector3.Dot(value, VectorTests.Vector3ValueInverted);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void DistanceBenchmark()
        {
            const float expectedResult = 3.46410155f;

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
                result = Vector3.Distance(VectorTests.Vector3Value, VectorTests.Vector3ValueInverted);
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
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += Vector3.Distance(value, VectorTests.Vector3ValueInverted);
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void LengthBenchmark()
        {
            const float expectedResult = 1.73205078f;

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
                result = VectorTests.Vector3Value.Length();
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
            var value = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                value += VectorTests.Vector3Delta;
                result += value.Length();
            }

            return result;
        }

        [Benchmark(InnerIterationCount = VectorTests.DefaultInnerIterationsCount)]
        public static void CrossBenchmark()
        {
            var expectedResult = Vector3.Zero;

            foreach (var iteration in Benchmark.Iterations)
            {
                Vector3 result;

                using (iteration.StartMeasurement())
                {
                    result = CrossTest();
                }

                VectorTests.AssertEqual(expectedResult, result);
            }
        }

        public static Vector3 CrossTest()
        {
            var result = VectorTests.Vector3Value;

            for (var iteration = 0; iteration < Benchmark.InnerIterationCount; iteration++)
            {
                result = Vector3.Cross(result, VectorTests.Vector3ValueInverted);
            }

            return result;
        }
    }
}
