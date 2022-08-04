// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.CosPi(float) over 5000 iterations for the domain 0, PI

        private const float cosPiDelta = 0.000628318531f;
        private const float cosPiExpectedResult = -218.823822f;

        [Benchmark]
        public void CosPi() => CosPiTest();

        public static void CosPiTest()
        {
            float result = 0.0f, value = 0.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += cosPiDelta;
                result += float.CosPi(value);
            }

            float diff = MathF.Abs(cosPiExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {cosPiExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
