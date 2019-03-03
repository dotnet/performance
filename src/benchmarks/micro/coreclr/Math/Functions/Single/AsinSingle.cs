// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class SinglePrecisionTests
    {
        // Tests MathF.Asin(float) over 5000 iterations for the domain -1, +1

        private const float asinDelta = 0.0004f;
        private const float asinExpectedResult = 1.57079590f;

        [Benchmark]
        public void Asin() => AsinTest();

        public static void AsinTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += asinDelta;
                result += MathF.Asin(value);
            }

            var diff = MathF.Abs(asinExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {asinExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
