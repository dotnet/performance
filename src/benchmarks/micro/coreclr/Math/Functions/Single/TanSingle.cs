// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class Single
    {
        // Tests MathF.Tan(float) over 5000 iterations for the domain -PI/2, +PI/2

        private const float tanDelta = 0.0004f;
        private const float tanExpectedResult = 1.66717815f;

        [Benchmark]
        public void Tan() => TanTest();

        public static void TanTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += tanDelta;
                result += MathF.Tan(value);
            }

            var diff = MathF.Abs(tanExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {tanExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
