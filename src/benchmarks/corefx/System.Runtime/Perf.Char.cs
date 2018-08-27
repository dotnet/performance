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
    public class Perf_Char
    {
        public static IEnumerable<object[]> Char_ChangeCase_MemberData()
        {
            yield return new object[] { 'A', new CultureInfo("en-US") }; // ASCII upper case
            yield return new object[] { 'a', new CultureInfo("en-US") }; // ASCII lower case
            yield return new object[] { '\u0130', new CultureInfo("en-US") }; // non-ASCII, English
            yield return new object[] { '\u4F60', new CultureInfo("zh-Hans") }; // non-ASCII, Chinese
        }

        [Benchmark]
        [ArgumentsSource(nameof(Char_ChangeCase_MemberData))]
        public char Char_ToLower(char c, CultureInfo cultureName) => char.ToLower(c, cultureName); // the argument is called "cultureName" instead of "culture" to keep benchmark ID in BenchView, do NOT rename it

        [Benchmark]
        [ArgumentsSource(nameof(Char_ChangeCase_MemberData))]
        public char Char_ToUpper(char c, CultureInfo cultureName)=> char.ToUpper(c, cultureName);
    }
}
