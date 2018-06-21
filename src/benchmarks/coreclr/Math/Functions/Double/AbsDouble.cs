// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Functions
{
    public partial class MathTests
    {
        // Tests Math.Abs(double) over 5000 iterations for the domain -1, +1

        private const double absDoubleDelta = 0.0004;
        private const double absDoubleExpectedResult = 2499.9999999999659;

        [Benchmark]
        public void AbsDoubleBenchmark() => AbsDoubleTest();

        public static void AbsDoubleTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += absDoubleDelta;
                result += Math.Abs(value);
            }

            var diff = Math.Abs(absDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {absDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }

}
