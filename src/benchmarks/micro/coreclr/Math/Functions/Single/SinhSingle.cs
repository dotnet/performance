// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class Single
    {
        // Tests MathF.Sinh(float) over 5000 iterations for the domain -1, +1

        private const float sinhDelta = 0.0004f;
        private const float sinhExpectedResult = 1.26028216f;

        /// <summary>
        /// this benchmark is dependent on loop alignment
        /// </summary>
        [Benchmark]
        public void Sinh() => SinhTest();

        public static void SinhTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sinhDelta;
                result += MathF.Sinh(value);
            }

            var diff = MathF.Abs(sinhExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {sinhExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
