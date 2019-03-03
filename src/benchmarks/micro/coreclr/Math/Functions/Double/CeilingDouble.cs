// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class DoublePrecisionTests
    {
        // Tests Math.Ceiling(double) over 5000 iterations for the domain -1, +1

        private const double ceilingDelta = 0.0004;
        private const double ceilingExpectedResult = 2500;

        [Benchmark]
        public void CeilingBenchmark() => CeilingTest();

        public static void CeilingTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += ceilingDelta;
                result += Math.Ceiling(value);
            }

            var diff = Math.Abs(ceilingExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {ceilingExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}