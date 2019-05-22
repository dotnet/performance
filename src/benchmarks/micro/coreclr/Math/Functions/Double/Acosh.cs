// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Acosh(double) over 5000 iterations for the domain +1, +3

        private const double acoshDelta = 0.0004;
        private const double acoshExpectedResult = 6148.648751739127;

        [Benchmark]
        public void Acosh() => AcoshTest();

        public static void AcoshTest()
        {
            var result = 0.0; var value = 1.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += Math.Acosh(value);
                value += acoshDelta;
            }

            var diff = Math.Abs(acoshExpectedResult - result);

            if (double.IsNaN(result) || (diff > MathTests.DoubleEpsilon))
            {
                throw new Exception($"Expected Result {acoshExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}