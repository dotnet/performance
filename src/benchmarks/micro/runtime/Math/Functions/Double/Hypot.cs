// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Hypot(double, double) over 5000 iterations for the domain x: +2, +1; y: -2, -1

        private const double hypotDeltaX = -0.0004;
        private const double hypotDeltaY = 0.0004;
        private const double hypotExpectedResult = 7069.653598303822;

        [Benchmark]
        public void Hypot() => HypotTest();

        public static void HypotTest()
        {
            double result = 0.0, valueX = 2.0, valueY = -2.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                valueX += hypotDeltaX; valueY += hypotDeltaY;
                result += double.Hypot(valueX, valueY);
            }

            double diff = Math.Abs(hypotExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {hypotExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
