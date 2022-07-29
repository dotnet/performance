// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.Exp2(float) over 5000 iterations for the domain -1, +1

        private const float exp2Delta = 0.0004f;
        private const float exp2ExpectedResult = 5410.92969f;

        [Benchmark]
        public void Exp2() => Exp2Test();

        public static void Exp2Test()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += exp2Delta;
                result += float.Exp2(value);
            }

            float diff = Math.Abs(exp2ExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {exp2ExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
