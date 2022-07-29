// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.Log2P1(float) over 5000 iterations for the domain +1, +3

        private const float log2P1Delta = 0.0004f;
        private const float log2P1ExpectedResult = 7785.8833f;

        [Benchmark]
        public void Log2P1() => Log2P1Test();

        public static void Log2P1Test()
        {
            float result = 0.0f, value = 1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += float.Log2P1(value);
                value += log2P1Delta;
            }

            float diff = MathF.Abs(log2P1ExpectedResult - result);

            if (float.IsNaN(result) || (diff > MathTests.SingleEpsilon))
            {
                throw new Exception($"Expected Result {log2P1ExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
