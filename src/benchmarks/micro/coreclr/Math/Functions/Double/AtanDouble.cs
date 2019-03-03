// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class DoublePrecisionTests
    {
        // Tests Math.Atan(double) over 5000 iterations for the domain -1, +1

        private const double atanDelta = 0.0004;
        private const double atanExpectedResult = 0.78539816322061329;

        [Benchmark]
        public void AtanBenchmark() => AtanTest();

        public static void AtanTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += atanDelta;
                result += Math.Atan(value);
            }

            var diff = Math.Abs(atanExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {atanExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
