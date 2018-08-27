// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.Globalization.Tests
{
    public class Perf_CompareInfo
    {
        private static string GenerateInputString(char source, int count, char replaceChar, int replacePos)
        {
            char[] str = new char[count];
            for (int i = 0; i < count; i++)
            {
                str[i] = replaceChar;
            }
            str[replacePos] = replaceChar;

            return new string(str);
        }

        public static IEnumerable<object[]> s_compareTestData => new List<object[]>
        {
            new object[] { new CultureInfo(""), "string1", "string2", CompareOptions.None },
            new object[] { new CultureInfo("tr-TR"), "StrIng", "string", CompareOptions.IgnoreCase },
            new object[] { new CultureInfo("en-US"), "StrIng", "string", CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo(""), "\u3060", "\u305F", CompareOptions.None },
            new object[] { new CultureInfo("ja-JP"), "ABCDE", "c", CompareOptions.None },
            new object[] { new CultureInfo("es-ES"), "$", "&", CompareOptions.IgnoreSymbols },
            new object[] { new CultureInfo(""), GenerateInputString('A', 10, '5', 5), GenerateInputString('A', 10, '5', 6), CompareOptions.Ordinal },
            new object[] { new CultureInfo(""), GenerateInputString('A', 100, 'X', 70), GenerateInputString('A', 100, 'X', 70), CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo("ja-JP"), GenerateInputString('A', 100, 'X', 70), GenerateInputString('A', 100, 'x', 70), CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo("en-US"), GenerateInputString('A', 1000, 'X', 500), GenerateInputString('A', 1000, 'X', 500), CompareOptions.None },
            new object[] { new CultureInfo("en-US"), GenerateInputString('\u3060', 1000, 'x', 500), GenerateInputString('\u3060', 1000, 'x', 10), CompareOptions.None },
            new object[] { new CultureInfo("es-ES"), GenerateInputString('\u3060', 100, '\u3059', 50), GenerateInputString('\u3060', 100, '\u3059', 50), CompareOptions.Ordinal },
            new object[] { new CultureInfo("tr-TR"), GenerateInputString('\u3060', 5000, '\u3059', 2501), GenerateInputString('\u3060', 5000, '\u3059', 2500), CompareOptions.Ordinal }
        };

        [Benchmark]
        [ArgumentsSource(nameof(s_compareTestData))]
        public void Compare(CultureInfo culture, string string1, string string2, CompareOptions options)
            => culture.CompareInfo.Compare(string1, string2, options);

        public static IEnumerable<object[]> s_indexTestData => new List<object[]>
        {
            new object[] { new CultureInfo(""), "string1", "string2", CompareOptions.None },
            new object[] { new CultureInfo(""), "foobardzsdzs", "rddzs", CompareOptions.IgnoreCase },
            new object[] { new CultureInfo("en-US"), "StrIng", "string", CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo(""), "\u3060", "\u305F", CompareOptions.None },
            new object[] { new CultureInfo("ja-JP"), "ABCDE", "c", CompareOptions.None },
            new object[] { new CultureInfo(""), "$", "&", CompareOptions.IgnoreSymbols },
            new object[] { new CultureInfo(""), "More Test's", "Tests", CompareOptions.IgnoreSymbols },
            new object[] { new CultureInfo("es-ES"), "TestFooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace },
            new object[] { new CultureInfo("en-US"), "Hello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello Worldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylong!xyz", "~", CompareOptions.Ordinal },
            new object[] { new CultureInfo("en-US"), "Hello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello Worldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylong!xyz", "w", CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo("es-ES"), "Hello Worldbbbbbbbbbbbbbbcbbbbbbbbbbbbbbbbbbba!", "y", CompareOptions.Ordinal },
            new object[] { new CultureInfo(""), GenerateInputString('A', 10, '5', 5), "5", CompareOptions.Ordinal },
            new object[] { new CultureInfo(""), GenerateInputString('A', 100, 'X', 70), "x", CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo("ja-JP"), GenerateInputString('A', 100, 'X', 70), "x", CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo("en-US"), GenerateInputString('A', 1000, 'X', 500), "X", CompareOptions.None },
            new object[] { new CultureInfo("en-US"), GenerateInputString('\u3060', 1000, 'x', 500), "x", CompareOptions.None },
            new object[] { new CultureInfo("es-ES"), GenerateInputString('\u3060', 100, '\u3059', 50), "\u3059", CompareOptions.Ordinal }
        };

        [Benchmark]
        [ArgumentsSource(nameof(s_indexTestData))]
        public int IndexOf(CultureInfo culture, string source, string value, CompareOptions options)
            => culture.CompareInfo.IndexOf(source, value, options);

        [Benchmark]
        [ArgumentsSource(nameof(s_indexTestData))]
        public int LastIndexOf(CultureInfo culture, string source, string value, CompareOptions options)
            => culture.CompareInfo.LastIndexOf(source, value, options);

        public static IEnumerable<object[]> s_prefixTestData => new List<object[]>
        {
            new object[] { new CultureInfo(""), "string1", "str", CompareOptions.None },
            new object[] { new CultureInfo(""), "foobardzsdzs", "FooBarDZ", CompareOptions.IgnoreCase },
            new object[] { new CultureInfo("en-US"), "StrIng", "str", CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo(""), "\u3060", "\u305F", CompareOptions.None },
            new object[] { new CultureInfo("ja-JP"), "ABCDE", "cd", CompareOptions.None },
            new object[] { new CultureInfo(""), "$", "&", CompareOptions.IgnoreSymbols },
            new object[] { new CultureInfo(""), "More's Test's", "More", CompareOptions.IgnoreSymbols },
            new object[] { new CultureInfo("es-ES"), "TestFooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace },
            new object[] { new CultureInfo("es-ES"), "Hello Worldbbbbbbbbbbbbbbcbbbbbbbbbbbbbbbbbbba!", "Hello World", CompareOptions.Ordinal },
            new object[] { new CultureInfo(""), GenerateInputString('A', 10, '5', 5), "AAAAA", CompareOptions.Ordinal },
            new object[] { new CultureInfo(""), GenerateInputString('A', 100, 'X', 70), new string('a', 30), CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo("ja-JP"), GenerateInputString('A', 100, 'X', 70), new string('a', 70), CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo("en-US"), GenerateInputString('A', 1000, 'X', 500), new string('A', 500), CompareOptions.None },
            new object[] { new CultureInfo("en-US"), GenerateInputString('\u3060', 1000, 'x', 500), new string('\u3060', 30), CompareOptions.None },
            new object[] { new CultureInfo("es-ES"), GenerateInputString('\u3060', 100, '\u3059', 50), "\u3060text", CompareOptions.Ordinal }
        };

        [Benchmark]
        [ArgumentsSource(nameof(s_prefixTestData))]
        public bool IsPrefix(CultureInfo culture, string source, string prefix, CompareOptions options)
            => culture.CompareInfo.IsPrefix(source, prefix, options);

        public static IEnumerable<object[]> s_suffixTestData => new List<object[]>
        {
            new object[] { new CultureInfo(""), "string1", "ing1", CompareOptions.None },
            new object[] { new CultureInfo(""), "foobardzsdzs", "DZsDzS", CompareOptions.IgnoreCase },
            new object[] { new CultureInfo("en-US"), "StrIng", "str", CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo(""), "\u3060", "\u305F", CompareOptions.IgnoreSymbols },
            new object[] { new CultureInfo("ja-JP"), "ABCDE", "E", CompareOptions.None },
            new object[] { new CultureInfo(""), "$", "&", CompareOptions.IgnoreSymbols },
            new object[] { new CultureInfo(""), "More's Test's", "Test", CompareOptions.IgnoreSymbols },
            new object[] { new CultureInfo("es-ES"), "TestFooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace },
            new object[] { new CultureInfo(""), GenerateInputString('A', 10, '5', 5), "5AAAA", CompareOptions.Ordinal },
            new object[] { new CultureInfo(""), GenerateInputString('A', 100, 'X', 70), new string('a', 30), CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo("ja-JP"), GenerateInputString('A', 100, 'X', 70), "x" + new string('a', 29), CompareOptions.OrdinalIgnoreCase },
            new object[] { new CultureInfo("en-US"), GenerateInputString('A', 1000, 'X', 100), new string('A', 900), CompareOptions.None },
            new object[] { new CultureInfo("en-US"), GenerateInputString('\u3060', 1000, 'x', 500), new string('\u3060', 30), CompareOptions.None },
            new object[] { new CultureInfo("es-ES"), GenerateInputString('\u3060', 100, '\u3059', 50), "\u3060text", CompareOptions.Ordinal }
        };

        [Benchmark]
        [ArgumentsSource(nameof(s_suffixTestData))]
        public bool IsSuffix(CultureInfo culture, string source, string suffix, CompareOptions options)
            => culture.CompareInfo.IsSuffix(source, suffix, options);

        [Benchmark]
        [Arguments("foo")]
        [Arguments("Exhibit \u00C0")]
        [Arguments("TestFooBA\u0300RnotsolongTELLme")]
        [Arguments("More Test's")]
        [Arguments("$")]
        [Arguments("\u3060")]
        [Arguments("Hello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello Worldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylong!xyz")]
        public bool IsSortable(string text)
            => CompareInfo.IsSortable(text);
    }
}
