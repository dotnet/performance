// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Transcendental
{
    public partial class Single
    {
        // Tests MathF.Cos(float) over 5000 iterations for the domain 0, PI

        private const float cosDelta = 0.000628318531f;
        private const float cosExpectedResult = -0.993487537f;

        [Benchmark]
        public void Cos() => CosTest();

        public static void CosTest()
        {
            var result = 0.0f; var value = 0.0f;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += cosDelta;
                result += MathF.Cos(value);
            }

            var diff = MathF.Abs(cosExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {cosExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
