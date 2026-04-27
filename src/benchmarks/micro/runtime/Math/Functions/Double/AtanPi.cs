// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.AtanPi(double) over 5000 iterations for the domain -1, +1

        private const double atanPiDelta = 0.0004;
        private const double atanPiExpectedResult = 0.24999999994323507;

        [Benchmark]
        public void AtanPi() => AtanPiTest();

        public static void AtanPiTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += atanPiDelta;
                result += double.AtanPi(value);
            }

            double diff = Math.Abs(atanPiExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {atanPiExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
