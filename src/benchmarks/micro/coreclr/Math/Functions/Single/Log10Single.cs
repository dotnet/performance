﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Functions
{
    public partial class MathTests
    {
        // Tests MathF.Log10(float) over 5000 iterations for the domain -1, +1

        private const float log10SingleDelta = 0.0004f;
        private const float log10SingleExpectedResult = -664.094971f;

        /// <summary>
        /// this benchmark is dependent on loop alignment
        /// </summary>
        [Benchmark]
        public void Log10SingleBenchmark() => Log10SingleTest();

        public static void Log10SingleTest()
        {
            var result = 0.0f; var value = 0.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += log10SingleDelta;
                result += MathF.Log10(value);
            }

            var diff = MathF.Abs(log10SingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {log10SingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
