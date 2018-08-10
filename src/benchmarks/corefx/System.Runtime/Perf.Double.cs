// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Double
    {
        decimal decimalNum = new decimal(1.23456789E+5);
        
        [Benchmark]
        [Arguments(104234.343, 1_000_000)]
        [Arguments(double.MaxValue, 100_000)]
        [Arguments(double.MinValue, 100_000)]
        [Arguments(double.MinValue / 2, 100_000)]
        [Arguments(double.NaN, 10_000_000)]
        [Arguments(double.PositiveInfinity, 10_000_000)]
        [Arguments(2.2250738585072009E-308, 100_000)]
        public string DefaultToString(double number, int innerIterations) // innerIterations argument is not used anymore but kept to preserve benchmark ID, do NOT remove it 
            => number.ToString(); 

        public static IEnumerable<object[]> ToStringWithCultureInfoArguments()
        {
            yield return new object[] {new CultureInfo("zh"), 104234.343, 1_000_000};
            yield return new object[] {new CultureInfo("zh"), double.MaxValue, 100_000};
            yield return new object[] {new CultureInfo("zh"), double.MinValue, 100_000};
            yield return new object[] {new CultureInfo("zh"), double.NaN, 20_000_000};
        }

        [Benchmark]
        [ArgumentsSource(nameof(ToStringWithCultureInfoArguments))]
        public string ToStringWithCultureInfo(CultureInfo cultureName, double number, int innerIterations) // the argument is called "cultureName" instead of "culture" to keep benchmark ID in BenchView, do NOT rename it
            => number.ToString(cultureName);

        public static IEnumerable<object[]> ToStringWithFormat_TestData()
        {
            string[] formats =
            {
                "R",
                "G",
                "G17",
                "E",
                "F50"
            };

            double[] normalTestValues =
            {
                0.0,
                250.0,
            };

            double[] edgeTestValues =
            {
                double.MaxValue,
                double.MinValue,
                double.Epsilon,
            };

            foreach (string format in formats)
            {
                foreach (double testValue in normalTestValues)
                {
                    yield return new object[] {format, testValue, 2_000_000};
                }

                foreach (double testValue in edgeTestValues)
                {
                    yield return new object[] {format, testValue, 100_000};
                }
            }

            yield return new object[] {"G", double.PositiveInfinity, 20_000_000};
            yield return new object[] {"G", double.NaN, 20_000_000};
        }

        [Benchmark]
        [ArgumentsSource(nameof(ToStringWithFormat_TestData))]
        public string ToStringWithFormat(string format, double number, int innerIterations) // innerIterations argument is not used anymore but kept to preserve benchmark ID, do NOT remove it  
            => number.ToString(format);

        [Benchmark]
        public string Decimal_ToString() => decimalNum.ToString();
    }
}