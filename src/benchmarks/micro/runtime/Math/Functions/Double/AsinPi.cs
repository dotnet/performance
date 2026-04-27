// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.AsinPi(double) over 5000 iterations for the domain -1, +1

        private const double asinPiDelta = 0.0004;
        private const double asinPiExpectedResult = -0.50000000010965517;

        [Benchmark]
        public void AsinPi() => AsinPiTest();

        public static void AsinPiTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += double.AsinPi(value);
                value += asinPiDelta;
            }

            double diff = Math.Abs(asinPiExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {asinPiExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
