// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        private static readonly int[] s_testStringSizes = { 10, 100, 1000 };

        public static IEnumerable<object[]> ContainsStringComparisonArgs()
        {
            foreach (var compareOption in s_compareOptions)
                foreach (var size in s_testStringSizes)
                    yield return new object[] { compareOption, new StringArguments(size) };
        }

        [Benchmark]
        [ArgumentsSource(nameof(ContainsStringComparisonArgs))]
        public bool Contains(StringComparison comparisonType, StringArguments size) // the argument is called "size" to keep the old benchmark ID, do NOT rename it
            => size.TestString1.Contains(size.Q3, comparisonType);
    }
}
