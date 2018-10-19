// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Functions
{
    public partial class MathTests
    {
        // Tests Math.Pow(double, double) over 5000 iterations for the domain x: +2, +1; y: -2, -1

        private const double powDoubleDeltaX = -0.0004;
        private const double powDoubleDeltaY = 0.0004;
        private const double powDoubleExpectedResult = 4659.4627376138733;

        [Benchmark]
        public void PowDoubleBenchmark() => PowDoubleTest();

        public static void PowDoubleTest()
        {
            var result = 0.0; var valueX = 2.0; var valueY = -2.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                valueX += powDoubleDeltaX; valueY += powDoubleDeltaY;
                result += Math.Pow(valueX, valueY);
            }

            var diff = Math.Abs(powDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {powDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
