// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.Hypot(float, float) over 5000 iterations for the domain x: +2, +1; y: -2, -1

        private const float hypotDeltaX = -0.0004f;
        private const float hypotDeltaY = 0.0004f;
        private const float hypotExpectedResult = 7070.32422f;

        [Benchmark]
        public void Hypot() => HypotTest();

        public static void HypotTest()
        {
            float result = 0.0f, valueX = 2.0f, valueY = -2.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                valueX += hypotDeltaX; valueY += hypotDeltaY;
                result += float.Hypot(valueX, valueY);
            }

            float diff = MathF.Abs(hypotExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {hypotExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
