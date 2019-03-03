// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class SinglePrecisionTests
    {
        // Tests MathF.Exp(float) over 5000 iterations for the domain -1, +1

        private const float expDelta = 0.0004f;
        private const float expExpectedResult = 5877.28564f;

        [Benchmark]
        public void ExpBenchmark() => ExpTest();

        public static void ExpTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += expDelta;
                result += MathF.Exp(value);
            }

            var diff = MathF.Abs(expExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {expExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
