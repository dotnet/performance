// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests double.Log2P1(double) over 5000 iterations for the domain +1, +3

        private const double log2P1Delta = 0.0004;
        private const double log2P1ExpectedResult = 7786.0247835324726;

        [Benchmark]
        public void Log2P1() => Log2P1Test();

        public static void Log2P1Test()
        {
            double result = 0.0, value = 1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += double.Log2P1(value);
                value += log2P1Delta;
            }

            double diff = Math.Abs(log2P1ExpectedResult - result);

            if (double.IsNaN(result) || (diff > MathTests.DoubleEpsilon))
            {
                throw new Exception($"Expected Result {log2P1ExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
