// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.SinCosPi(float) over 5000 iterations for the domain -PI/2, +PI/2

        private const float sinCosPiDelta = 0.000628318531f;
        private const float sinCosPiExpectedResultX = -1.01186228f;
        private const float sinCosPiExpectedResultY = -988.237793f;

        [Benchmark]
        public void SinCosPi() => SinCosPiTest();

        public static void SinCosPiTest()
        {
            float resultX = 0.0f, resultY = 0.0f, value = -1.57079633f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sinCosPiDelta;
                // (float sinResult, float cosResult) = float.SinCosPi(value);
                float sinResult = float.SinPi(value);
                float cosResult = float.CosPi(value);
                resultX += sinResult; resultY += cosResult;
            }

            float diffX = Math.Abs(sinCosPiExpectedResultX - resultX);
            float diffY = Math.Abs(sinCosPiExpectedResultY - resultY);

            if ((diffX > MathTests.SingleEpsilon) || (diffY > MathTests.SingleEpsilon))
            {
                throw new Exception($"Expected Result ({sinCosPiExpectedResultX,10:g9}, {sinCosPiExpectedResultY,10:g9}); Actual Result ({resultX,10:g9}, {resultY,10:g9})");
            }
        }
    }
}
