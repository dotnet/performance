// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace MathTests.FloatingPointTests
{
    public partial class DoublePrecisionTests
    {
        // Tests Math.Asin(double) over 5000 iterations for the domain -1, +1

        private const double asinDelta = 0.0004;
        private const double asinExpectedResult = 1.5707959028763392;

        [Benchmark]
        public void AsinBenchmark() => AsinTest();

        public static void AsinTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += asinDelta;
                result += Math.Asin(value);
            }

            var diff = Math.Abs(asinExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {asinExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
