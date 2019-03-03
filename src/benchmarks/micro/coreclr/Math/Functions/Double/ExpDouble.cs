// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class DoublePrecisionTests
    {
        // Tests Math.Exp(double) over 5000 iterations for the domain -1, +1

        private const double expDelta = 0.0004;
        private const double expExpectedResult = 5877.1812477590884;

        [Benchmark]
        public void ExpBenchmark() => ExpTest();

        public static void ExpTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += expDelta;
                result += Math.Exp(value);
            }

            var diff = Math.Abs(expExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {expExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
