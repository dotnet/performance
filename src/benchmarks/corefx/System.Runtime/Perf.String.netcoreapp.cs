// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        [Benchmark]
        [Arguments("This is a very nice sentence", "bad", StringComparison.CurrentCultureIgnoreCase)]
        [Arguments("This is a very nice sentence", "bad", StringComparison.Ordinal)]
        [Arguments("This is a very nice sentence", "bad", StringComparison.OrdinalIgnoreCase)]
        [Arguments("This is a very nice sentence", "nice", StringComparison.CurrentCultureIgnoreCase)]
        [Arguments("This is a very nice sentence", "nice", StringComparison.Ordinal)]
        [Arguments("This is a very nice sentence", "nice", StringComparison.OrdinalIgnoreCase)]
        public bool Contains(String text, String value, StringComparison comparisonType)
            => text.Contains(value, comparisonType);
    }
}
