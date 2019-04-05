// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Log2(double) over 5000 iterations for the domain +1, +3

        private const double log2Delta = 0.0004;
        private const double log2ExpectedResult = 4672.9510376532398;

        [Benchmark]
        public void Log2() => Log2Test();

        public static void Log2Test()
        {
            var result = 0.0; var value = 1.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += Math.Log2(value);
                value += log2Delta;
            }

            var diff = Math.Abs(log2ExpectedResult - result);

            if (double.IsNaN(result) || (diff > MathTests.DoubleEpsilon))
            {
                throw new Exception($"Expected Result {log2ExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}