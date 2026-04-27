// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.SinCosPi(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double sinCosPiDelta = 0.0006283185307180;
        private const double sinCosPiExpectedResultX = -0.97536797261875063;
        private const double sinCosPiExpectedResultY = -988.25405329984926;

        [Benchmark]
        public void SinCosPi() => SinCosPiTest();

        public static void SinCosPiTest()
        {
            double resultX = 0.0, resultY = 0.0, value = -1.5707963267948966;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sinCosPiDelta;
                // (double sinResult, double cosResult) = double.SinCosPi(value);
                double sinResult = double.SinPi(value);
                double cosResult = double.CosPi(value);
                resultX += sinResult; resultY += cosResult;
            }

            double diffX = Math.Abs(sinCosPiExpectedResultX - resultX);
            double diffY = Math.Abs(sinCosPiExpectedResultY - resultY);

            if ((diffX > MathTests.DoubleEpsilon) || (diffY > MathTests.DoubleEpsilon))
            {
                throw new Exception($"Expected Result ({sinCosPiExpectedResultX,20:g17}, {sinCosPiExpectedResultY,20:g17}); Actual Result ({resultX,20:g17}, {resultY,20:g17})");
            }
        }
    }
}
