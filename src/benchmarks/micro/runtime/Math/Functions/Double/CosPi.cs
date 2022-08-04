// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.CosPi(double) over 5000 iterations for the domain 0, PI

        private const double cosPiDoubleDelta = 0.0006283185307180;
        private const double cosPiDoubleExpectedResult = -218.94441504588787;

        [Benchmark]
        public void CosPi() => CosPiDoubleTest();

        public static void CosPiDoubleTest()
        {
            double result = 0.0, value = 0.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += cosPiDoubleDelta;
                result += double.CosPi(value);
            }

            double diff = Math.Abs(cosPiDoubleExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {cosPiDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
