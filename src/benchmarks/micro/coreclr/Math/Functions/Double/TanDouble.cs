// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class DoublePrecisionTests
    {
        // Tests Math.Tan(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double tanDelta = 0.0004;
        private const double tanExpectedResult = 1.5574077243051505;

        [Benchmark]
        public void TanBenchmark() => TanTest();

        public static void TanTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += tanDelta;
                result += Math.Tan(value);
            }

            var diff = Math.Abs(tanExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {tanExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
