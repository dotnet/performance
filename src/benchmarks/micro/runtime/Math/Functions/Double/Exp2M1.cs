// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.Exp2M1(double) over 5000 iterations for the domain -1, +1

        private const double exp2M1Delta = 0.0004;
        private const double exp2M1ExpectedResult = 410.85643799078713;

        [Benchmark]
        public void Exp2M1() => Exp2M1Test();

        public static void Exp2M1Test()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += exp2M1Delta;
                result += double.Exp2M1(value);
            }

            double diff = Math.Abs(exp2M1ExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {exp2M1ExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
