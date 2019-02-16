// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace MathTests.FloatingPointTests
{
    [BenchmarkCategory(Categories.CoreCLR)]
    public partial class SinglePrecisionTests
    {
        // Tests Math.Abs(single) over 5000 iterations for the domain -1, +1

        private const float absDelta = 0.0004f;
        private const float absExpectedResult = 2500.03125f;

        [Benchmark]
        public void AbsBenchmark() => AbsTest();

        public static void AbsTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += absDelta;
                result += Math.Abs(value);
            }

            var diff = Math.Abs(absExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {absExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
