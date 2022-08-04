// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.LogP1(double) over 5000 iterations for the domain 0, +2

        private const double logP1Delta = 0.0004;
        private const double logP1ExpectedResult = 3240.1414489328322;

        [Benchmark]
        public void LogP1() => LogP1Test();

        public static void LogP1Test()
        {
            double result = 0.0, value = 0.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += logP1Delta;
                result += double.LogP1(value);
            }

            double diff = Math.Abs(logP1ExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {logP1ExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }

}
