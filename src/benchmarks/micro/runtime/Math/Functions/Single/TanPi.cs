// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.TanPi(float) over 5000 iterations for the domain -PI/2, +PI/2

        private const float tanPiDelta = 0.0004f;
        private const float tanPiExpectedResult = -52762.5156f;

        [Benchmark]
        public void TanPi() => TanPiTest();

        public static void TanPiTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += tanPiDelta;
                result += float.TanPi(value);
            }

            float diff = MathF.Abs(tanPiExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {tanPiExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
