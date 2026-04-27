// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.SinCos(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double sinCosDelta = 0.0006283185307180;
        private const double sinCosExpectedResultX = 1.0000000005446217;
        private const double sinCosExpectedResultY = 3183.0987571179171;

        [Benchmark]
        public void SinCos() => SinCosTest();

        public static void SinCosTest()
        {
            double resultX = 0.0, resultY = 0.0, value = -1.5707963267948966;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sinCosDelta;
                (double sinResult, double cosResult) = Math.SinCos(value);
                resultX += sinResult; resultY += cosResult;
            }

            double diffX = Math.Abs(sinCosExpectedResultX - resultX);
            double diffY = Math.Abs(sinCosExpectedResultY - resultY);

            if ((diffX > MathTests.DoubleEpsilon) || (diffY > MathTests.DoubleEpsilon))
            {
                throw new Exception($"Expected Result ({sinCosExpectedResultX,20:g17}, {sinCosExpectedResultY,20:g17}); Actual Result ({resultX,20:g17}, {resultY,20:g17})");
            }
        }
    }
}
