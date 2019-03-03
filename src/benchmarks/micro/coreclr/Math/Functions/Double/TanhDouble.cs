// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Transcendental
{
    public partial class Double
    {
        // Tests Math.Tanh(double) over 5000 iterations for the domain -1, +1

        private const double tanhDelta = 0.0004;
        private const double tanhExpectedResult = 0.76159415578341827;

        [Benchmark]
        public void Tanh() => TanhTest();

        public static void TanhTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += tanhDelta;
                result += Math.Tanh(value);
            }

            var diff = Math.Abs(tanhExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {tanhExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
