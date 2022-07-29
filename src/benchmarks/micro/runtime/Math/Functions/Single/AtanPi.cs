// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests float.AtanPi(float) over 5000 iterations for the domain -1, +1

        private const float atanPiDelta = 0.0004f;
        private const float atanPiExpectedResult = 0.267916471f;

        [Benchmark]
        public void AtanPi() => AtanPiTest();

        public static void AtanPiTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += atanPiDelta;
                result += float.AtanPi(value);
            }

            float diff = MathF.Abs(atanPiExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {atanPiExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
