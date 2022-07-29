// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.Exp2(double) over 5000 iterations for the domain -1, +1

        private const double exp2Delta = 0.0004;
        private const double exp2ExpectedResult = 5410.8564379907702;

        [Benchmark]
        public void Exp2() => Exp2Test();

        public static void Exp2Test()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += exp2Delta;
                result += double.Exp2(value);
            }

            double diff = Math.Abs(exp2ExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {exp2ExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
