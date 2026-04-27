// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.Exp10(float) over 5000 iterations for the domain -1, +1

        private const float exp10Delta = 0.0004f;
        private const float exp10ExpectedResult = 10754.291f;

        [Benchmark]
        public void Exp10() => Exp10Test();

        public static void Exp10Test()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += exp10Delta;
                result += float.Exp10(value);
            }

            float diff = Math.Abs(exp10ExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {exp10ExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
