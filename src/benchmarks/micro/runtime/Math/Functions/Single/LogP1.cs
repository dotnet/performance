// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.LogP1(float) over 5000 iterations for the domain 0, +2

        private const float logP1Delta = 0.0004f;
        private const float logP1ExpectedResult = 3240.10205f;

        [Benchmark]
        public void LogP1() => LogP1Test();

        public static void LogP1Test()
        {
            float result = 0.0f, value = 0.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += logP1Delta;
                result += float.LogP1(value);
            }

            float diff = MathF.Abs(logP1ExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {logP1ExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }

}
