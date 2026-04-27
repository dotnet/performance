// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
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
        [Arguments("Good afternoon, Constable!")]
        public int Char_IsLower(string input) // counts the number of lowercase chars in a string
        {
            int count = 0;
            foreach (char ch in input)
            {
                if (char.IsLower(ch))
                {
                    count++;
                }
            }
            return count;
        }

        [Benchmark]
        [Arguments("Good afternoon, Constable!")]
        public int Char_IsUpper(string input) // counts the number of uppercase chars in a string
        {
            int count = 0;
            foreach (char ch in input)
            {
                if (char.IsUpper(ch))
                {
                    count++;
                }
            }
            return count;
        }

        [Benchmark]
        [ArgumentsSource(nameof(Char_ChangeCase_MemberData))]
        [MemoryRandomization]
        public char Char_ToLower(char c, CultureInfo cultureName) => char.ToLower(c, cultureName);

        [Benchmark]
        [ArgumentsSource(nameof(Char_ChangeCase_MemberData))]
        [MemoryRandomization]
        public char Char_ToUpper(char c, CultureInfo cultureName)=> char.ToUpper(c, cultureName);

        [Benchmark]
        [Arguments("Hello World!")]
        public int Char_ToUpperInvariant(string input)
        {
            int accum = 0;
            for (int i = 0; i < input.Length; i++)
            {
                accum += char.ToUpperInvariant(input[i]);
            }
            return accum;
        }

        [Benchmark]
        [Arguments("Hello World!")]
        public int Char_ToLowerInvariant(string input)
        {
            int accum = 0;
            for (int i = 0; i < input.Length; i++)
            {
                accum += char.ToLowerInvariant(input[i]);
            }
            return accum;
        }

        [Benchmark]
        [Arguments('.')]
        [Arguments('a')]
        [Arguments('\x05D0')]
        public UnicodeCategory GetUnicodeCategory(char c) => char.GetUnicodeCategory(c);
    }
}
