// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.TanPi(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double tanPiDelta = 0.0004;
        private const double tanPiExpectedResult = 14955062196972.66;

        [Benchmark]
        public void TanPi() => TanPiTest();

        public static void TanPiTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += tanPiDelta;
                result += double.TanPi(value);
            }

            double diff = Math.Abs(tanPiExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {tanPiExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
