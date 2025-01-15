// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Consumer = BenchmarkDotNet.Engines.Consumer;

namespace System.Memory
{
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.Span)]
    public class ReadOnlySpan
    {
        private readonly string _sampeString = "this is a very nice sample string";

        [Benchmark]
        public ReadOnlySpan<char> StringAsSpan() => _sampeString.AsSpan();
        
        [Benchmark(OperationsPerInvoke = 16 * 2)]
        public char GetPinnableReference()
        {
            ReadOnlySpan<char> span = _sampeString.AsSpan();
            char c;

            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            c = span.GetPinnableReference(); c = span.GetPinnableReference();
            return c;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        [ArgumentsSource(nameof(IndexOfStringArguments))]
        [MemoryRandomization]
        public int IndexOfString(string input, string value, StringComparison comparisonType)
        {
            ReadOnlySpan<char> inputSpan = input.AsSpan();
            ReadOnlySpan<char> valueSpan = value.AsSpan();

            int result = 0;
            
            result ^= inputSpan.IndexOf(valueSpan, comparisonType); result ^= inputSpan.IndexOf(valueSpan, comparisonType);
            result ^= inputSpan.IndexOf(valueSpan, comparisonType); result ^= inputSpan.IndexOf(valueSpan, comparisonType);
            result ^= inputSpan.IndexOf(valueSpan, comparisonType); result ^= inputSpan.IndexOf(valueSpan, comparisonType);
            result ^= inputSpan.IndexOf(valueSpan, comparisonType); result ^= inputSpan.IndexOf(valueSpan, comparisonType);
            result ^= inputSpan.IndexOf(valueSpan, comparisonType); result ^= inputSpan.IndexOf(valueSpan, comparisonType);
            result ^= inputSpan.IndexOf(valueSpan, comparisonType); result ^= inputSpan.IndexOf(valueSpan, comparisonType);
            result ^= inputSpan.IndexOf(valueSpan, comparisonType); result ^= inputSpan.IndexOf(valueSpan, comparisonType);
            result ^= inputSpan.IndexOf(valueSpan, comparisonType); result ^= inputSpan.IndexOf(valueSpan, comparisonType);

            return result;
        }
        
        public static IEnumerable<object[]> IndexOfStringArguments => new List<object[]>
        {
            new object[] { "string1", "string2", StringComparison.InvariantCulture },
            new object[] { "foobardzsdzs", "rddzs", StringComparison.InvariantCulture },
            new object[] { "StrIng", "string", StringComparison.OrdinalIgnoreCase },
            new object[] { "\u3060", "\u305F", StringComparison.InvariantCulture },
            new object[] { "ABCDE", "c", StringComparison.InvariantCultureIgnoreCase },
            new object[] { "More Test's", "Tests", StringComparison.OrdinalIgnoreCase },
            new object[] { "Hello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello Worldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylong!xyz", "~", StringComparison.Ordinal },
            new object[] { "Hello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello Worldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylong!xyz", "w", StringComparison.OrdinalIgnoreCase },
            new object[] { "Hello Worldbbbbbbbbbbbbbbcbbbbbbbbbbbbbbbbbbba!", "y", StringComparison.Ordinal },
            new object[] { GenerateInputString('A', 10, '5', 5), "5", StringComparison.InvariantCulture },
            new object[] { GenerateInputString('A', 100, 'X', 70), "x", StringComparison.InvariantCultureIgnoreCase },
            new object[] { GenerateInputString('A', 100, 'X', 70), "x", StringComparison.OrdinalIgnoreCase },
            new object[] { GenerateInputString('A', 1000, 'X', 500), "X", StringComparison.Ordinal },
            new object[] { GenerateInputString('\u3060', 1000, 'x', 500), "x", StringComparison.Ordinal },
            new object[] { GenerateInputString('\u3060', 100, '\u3059', 50), "\u3059", StringComparison.Ordinal }
        };

        [Benchmark]
        [ArgumentsSource(nameof(TrimArguments))]
        [MemoryRandomization]
        public ReadOnlySpan<char> Trim(string input) => input.AsSpan().Trim();

        public static IEnumerable<object> TrimArguments()
        {
            yield return "";
            yield return " abcdefg ";
            yield return "abcdefg";
        }

        [Benchmark]
        [ArgumentsSource(nameof(IsWhiteSpaceArguments))]
        [MemoryRandomization]
        public bool IsWhiteSpace(string input) => input.AsSpan().IsWhiteSpace();

        public static IEnumerable<object> IsWhiteSpaceArguments()
        {
            yield return "";
            yield return "0abcdefg";
            yield return " 1abcdefg";
            yield return "  2abcdefg";
            yield return "    4abcdefg";
            yield return "     5abcdefg";
            yield return "      6abcdefg";
            yield return "       7abcdefg";
            yield return "        8abcdefg";
            yield return "         9abcdefg";
            yield return "          10abcdefg";
            yield return "           11abcdefg";
            yield return "            12abcdefg";
            yield return "                16abcdefg";
            yield return "                                32abcdefg";
        }

        private static string GenerateInputString(char source, int count, char replaceChar, int replacePos)
        {
            char[] str = new char[count];
            for (int i = 0; i < count; i++)
            {
                str[i] = source;
            }
            str[replacePos] = replaceChar;

            return new string(str);
        }
    }
}
