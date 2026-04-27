// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.SinPi(float) over 5000 iterations for the domain -PI/2, +PI/2

        private const float sinPiDelta = 0.000628318531f;
        private const float sinPiExpectedResult = -1.01186228f;

        [Benchmark]
        public void SinPi() => SinPiTest();

        public static void SinPiTest()
        {
            float result = 0.0f, value = -1.57079633f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sinPiDelta;
                result += float.SinPi(value);
            }

            float diff = MathF.Abs(sinPiExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {sinPiExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
