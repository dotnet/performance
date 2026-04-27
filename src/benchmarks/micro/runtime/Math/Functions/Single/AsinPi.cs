// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.AsinPi(float) over 5000 iterations for the domain -1, +1

        private const float asinPiDelta = 0.0004f;
        private const float asinPiExpectedResult = -0.463512957f;

        [Benchmark]
        public void AsinPi() => AsinPiTest();

        public static void AsinPiTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += float.AsinPi(value);
                value += asinPiDelta;
            }

            float diff = MathF.Abs(asinPiExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {asinPiExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
