// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.ExpM1(double) over 5000 iterations for the domain -1, +1

        private const double expM1Delta = 0.0004;
        private const double expM1ExpectedResult = 877.18124775909428;

        [Benchmark]
        public void ExpM1() => ExpM1Test();

        public static void ExpM1Test()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += expM1Delta;
                result += double.ExpM1(value);
            }

            double diff = Math.Abs(expM1ExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {expM1ExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
