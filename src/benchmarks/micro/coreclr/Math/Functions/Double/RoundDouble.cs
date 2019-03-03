// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class DoublePrecisionTests
    {
        // Tests Math.Round(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double roundDelta = 0.0006283185307180;
        private const double roundExpectedResult = 2;

        [Benchmark]
        public void Round() => RoundTest();

        public static void RoundTest()
        {
            var result = 0.0; var value = -1.5707963267948966;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += roundDelta;
                result += Math.Round(value);
            }

            var diff = Math.Abs(roundExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {roundExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
