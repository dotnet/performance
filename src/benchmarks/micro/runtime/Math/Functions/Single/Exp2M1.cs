﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.Exp2M1(float) over 5000 iterations for the domain -1, +1

        private const float exp2M1Delta = 0.0004f;
        private const float exp2M1ExpectedResult = 410.921875f;

        [Benchmark]
        public void Exp2M1() => Exp2M1Test();

        public static void Exp2M1Test()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += exp2M1Delta;
                result += float.Exp2M1(value);
            }

            float diff = Math.Abs(exp2M1ExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {exp2M1ExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
