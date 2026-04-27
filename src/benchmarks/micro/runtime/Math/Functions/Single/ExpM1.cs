// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.ExpM1(float) over 5000 iterations for the domain -1, +1

        private const float expM1Delta = 0.0004f;
        private const float expM1ExpectedResult = 877.289917f;

        [Benchmark]
        public void ExpM1() => ExpM1Test();

        public static void ExpM1Test()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += expM1Delta;
                result += float.ExpM1(value);
            }

            float diff = Math.Abs(expM1ExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {expM1ExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
