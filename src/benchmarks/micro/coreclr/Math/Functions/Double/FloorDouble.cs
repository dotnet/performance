// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class DoublePrecisionTests
    {
        // Tests Math.Floor(double) over 5000 iterations for the domain -1, +1

        private const double floorDelta = 0.0004;
        private const double floorExpectedResult = -2500;

        [Benchmark]
        public void Floor() => FloorTest();

        public static void FloorTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += floorDelta;
                result += Math.Floor(value);
            }

            var diff = Math.Abs(floorExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {floorExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
