// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.SinPi(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double sinPiDelta = 0.0006283185307180;
        private const double sinPiExpectedResult = -0.97536797261875063;

        [Benchmark]
        public void SinPi() => SinPiTest();

        public static void SinPiTest()
        {
            double result = 0.0, value = -1.5707963267948966;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sinPiDelta;
                result += double.SinPi(value);
            }

            double diff = Math.Abs(sinPiExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {sinPiExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
