// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Transcendental
{
    public partial class DoublePrecisionTests
    {
        // Tests Math.Pow(double, double) over 5000 iterations for the domain x: +2, +1; y: -2, -1

        private const double powDeltaX = -0.0004;
        private const double powDeltaY = 0.0004;
        private const double powExpectedResult = 4659.4627376138733;

        [Benchmark]
        public void PowBenchmark() => PowTest();

        public static void PowTest()
        {
            var result = 0.0; var valueX = 2.0; var valueY = -2.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                valueX += powDeltaX; valueY += powDeltaY;
                result += Math.Pow(valueX, valueY);
            }

            var diff = Math.Abs(powExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {powExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
