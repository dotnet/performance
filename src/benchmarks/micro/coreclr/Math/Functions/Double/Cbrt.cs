// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Cbrt(double) over 5000 iterations for the domain +0, +PI

        private const double cbrtDelta = 0.0006283185307180;
        private const double cbrtExpectedResult = 5491.4635361574383;

        [Benchmark]
        public void Cbrt() => CbrtTest();

        public static void CbrtTest()
        {
            var result = 0.0; var value = 0.0;

            for (var iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += Math.Cbrt(value);
                value += cbrtDelta;
            }

            var diff = Math.Abs(cbrtExpectedResult - result);

            if (double.IsNaN(result) || (diff > MathTests.DoubleEpsilon))
            {
                throw new Exception($"Expected Result {cbrtExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}