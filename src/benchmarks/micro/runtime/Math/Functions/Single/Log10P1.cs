// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.Log10P1(float) over 5000 iterations for the domain 0, +2

        private const float log10P1Delta = 0.0004f;
        private const float log10P1ExpectedResult = 1407.15637f;

        /// <summary>
        /// this benchmark is dependent on loop alignment
        /// </summary>
        [Benchmark]
        public void Log10P1() => Log10P1Test();

        public static void Log10P1Test()
        {
            float result = 0.0f, value = 0.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += log10P1Delta;
                result += float.Log10P1(value);
            }

            float diff = MathF.Abs(log10P1ExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {log10P1ExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
