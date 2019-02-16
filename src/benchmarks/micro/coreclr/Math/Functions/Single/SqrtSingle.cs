// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace MathTests.FloatingPointTests
{
    public partial class SinglePrecisionTests
    {
        // Tests MathF.Sqrt(float) over 5000 iterations for the domain 0, PI

        private const float sqrtDelta = 0.000628318531f;
        private const float sqrtExpectedResult = 5909.03027f;

        [Benchmark]
        public void SqrtBenchmark() => SqrtTest();

        public static void SqrtTest()
        {
            var result = 0.0f; var value = 0.0f;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sqrtDelta;
                result += MathF.Sqrt(value);
            }

            var diff = MathF.Abs(sqrtExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {sqrtExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
