// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.MathBenchmarks
{
    [BenchmarkCategory(Categories.CoreCLR)]
    public partial class Double
    {
        // Tests Math.Abs(double) over 5000 iterations for the domain -1, +1

        private const double absDelta = 0.0004;
        private const double absExpectedResult = 2499.9999999999659;

        [Benchmark]
        public void Abs() => AbsTest();

        public static void AbsTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += absDelta;
                result += Math.Abs(value);
            }

            var diff = Math.Abs(absExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {absExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }

}
