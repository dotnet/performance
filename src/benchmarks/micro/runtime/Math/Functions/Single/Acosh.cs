// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Acosh(float) over 5000 iterations for the domain +1, +3

        private const float acoshDelta = 0.0004f;
        private const float acoshExpectedResult = 6148.45459f;

        [Benchmark]
        public void Acosh() => AcoshTest();

        public static void AcoshTest()
        {
            var result = 0.0f; var value = 1.0f;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += MathF.Acosh(value);
                value += acoshDelta;
            }

            var diff = MathF.Abs(acoshExpectedResult - result);

            if (float.IsNaN(result) || (diff > MathTests.SingleEpsilon))
            {
                throw new Exception($"Expected Result {acoshExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}