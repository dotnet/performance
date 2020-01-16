// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Log(double) over 5000 iterations for the domain -1, +1

        private const double logDelta = 0.0004;
        private const double logExpectedResult = -1529.0865454048721;

        [Benchmark]
        public void Log() => LogTest();

        public static void LogTest()
        {
            var result = 0.0; var value = 0.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += logDelta;
                result += Math.Log(value);
            }

            var diff = Math.Abs(logExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {logExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }

}
