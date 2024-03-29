﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.AcosPi(float) over 5000 iterations for the domain -1, +1

        private const float acosPiDelta = 0.0004f;
        private const float acosPiExpectedResult = 2500.46362f;

        [Benchmark]
        public void AcosPi() => AcosPiTest();

        public static void AcosPiTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += float.AcosPi(value);
                value += acosPiDelta;
            }

            float diff = MathF.Abs(acosPiExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {acosPiExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
