// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.SinCos(float) over 5000 iterations for the domain -PI/2, +PI/2

        private const float sinCosDelta = 0.000628318531f;
        private const float sinCosExpectedResultX = 1.03592682f;
        private const float sinCosExpectedResultY = 3183.11548f;

        [Benchmark]
        public void SinCos() => SinCosTest();

        public static void SinCosTest()
        {
            float resultX = 0.0f, resultY = 0.0f, value = -1.57079633f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sinCosDelta;
                (float sinResult, float cosResult) = MathF.SinCos(value);
                resultX += sinResult; resultY += cosResult;
            }

            float diffX = Math.Abs(sinCosExpectedResultX - resultX);
            float diffY = Math.Abs(sinCosExpectedResultY - resultY);

            if ((diffX > MathTests.SingleEpsilon) || (diffY > MathTests.SingleEpsilon))
            {
                throw new Exception($"Expected Result ({sinCosExpectedResultX,10:g9}, {sinCosExpectedResultY,10:g9}); Actual Result ({resultX,10:g9}, {resultY,10:g9})");
            }
        }
    }
}
