// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace MathTests.FloatingPointTests
{
    public partial class DoublePrecisionTests
    {
        // Tests Math.Sqrt(double) over 5000 iterations for the domain 0, PI

        private const double sqrtDelta = 0.0006283185307180;
        private const double sqrtExpectedResult = 5909.0605337797215;

        [Benchmark]
        public void SqrtBenchmark() => SqrtTest();

        public static void SqrtTest()
        {
            var result = 0.0; var value = 0.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sqrtDelta;
                result += Math.Sqrt(value);
            }

            var diff = Math.Abs(sqrtExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {sqrtExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
