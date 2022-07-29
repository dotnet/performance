// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.Log10(double) over 5000 iterations for the domain 0, +2

        private const double log10P1Delta = 0.0004;
        private const double log10P1ExpectedResult = 1407.1755518575349;

        /// <summary>
        /// this benchmark is dependent on loop alignment
        /// </summary>
        [Benchmark]
        public void Log10P1() => Log10P1Test();

        public static void Log10P1Test()
        {
            double result = 0.0, value = 0.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += log10P1Delta;
                result += double.Log10P1(value);
            }

            double diff = Math.Abs(log10P1ExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {log10P1ExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
