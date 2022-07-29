// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.AcosPi(double) over 5000 iterations for the domain -1, +1

        private const double acosPiDelta = 0.0004;
        private const double acosPiExpectedResult = 2500.5000000001137;

        [Benchmark]
        public void AcosPi() => AcosPiTest();

        public static void AcosPiTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += double.AcosPi(value);
                value += acosPiDelta;
            }

            double diff = Math.Abs(acosPiExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {acosPiExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
