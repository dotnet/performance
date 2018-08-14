using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        private static readonly char[][] s_replaceCharPairs = new char[][]
        {
            new char[] {' ', 'T'},
            new char[] {'T', 'd'},
            new char[] {'d', 'a'},
            new char[] {'a', (char) 192},
            new char[] {(char) 192, (char) 8197},
            new char[] {(char) 8197, "\u0400"[0]},
            new char[] {"\u0400"[0], '\t'},
            new char[] {'\t', (char) 768},
            new char[] {(char) 768, ' '},
        };

        public static IEnumerable<object[]> ReplaceCharArgs => Permutations(s_trimStrings, s_replaceCharPairs);

        [Benchmark]
        [ArgumentsSource(nameof(ReplaceCharArgs))]
        public string Replace_Char(string s, char[] chars)
            => s.Replace(chars[0], chars[1]);

        private static readonly string[][] s_replaceStringPairs = new string[][]
        {
            new string[] {"  ", "T"},
            new string[] {"T", "dd"},
            new string[] {"dd", "a"},
            new string[] {"a", "\u00C0"},
            new string[] {"\u00C0", "a\u0300"},
            new string[] {"a\u0300", "\u0400"},
            new string[] {"\u0400", "\u0300"},
            new string[] {"\u0300", "\u00A0\u2000"},
            new string[] {"\u00A0\u2000", "  "},
        };

        public static IEnumerable<object[]> ReplaceStringArgs => Permutations(s_trimStrings, s_replaceStringPairs);

        [Benchmark]
        [ArgumentsSource(nameof(ReplaceStringArgs))]
        public string Replace_String(string s0, string[] strings)
            => s0.Replace(strings[0], strings[1]);
    }
}