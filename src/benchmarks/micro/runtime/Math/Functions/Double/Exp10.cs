// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.Exp10(double) over 5000 iterations for the domain -1, +1

        private const double exp10Delta = 0.0004;
        private const double exp10ExpectedResult = 10753.739186957077;

        [Benchmark]
        public void Exp10() => Exp10Test();

        public static void Exp10Test()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += exp10Delta;
                result += double.Exp10(value);
            }

            double diff = Math.Abs(exp10ExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {exp10ExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
