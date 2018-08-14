using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        private static readonly object[] s_trimStrings = new object[]
        {
            "",
            " ",
            "   ",
            "Test",
            " Test",
            "Test ",
            " Te st  ",
            "\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\t Te\t \tst \t\r\n\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005",
            " \u0400Te \u0400st",
            " a\u0300\u00C0",
            " a \u0300 \u00C0 ",
            "     ddsz dszdsz \t  dszdsz  \t        ",
        };
        
        public static IEnumerable<object> TrimArgs => s_trimStrings;

        [Benchmark]
        [ArgumentsSource(nameof(TrimArgs))]
        public string Trim(string s)
            => s.Trim();

        [Benchmark]
        [ArgumentsSource(nameof(TrimArgs))]
        public string TrimStart(string s)
            => s.TrimStart();

        [Benchmark]
        [ArgumentsSource(nameof(TrimArgs))]
        public string TrimEnd(string s)
            => s.TrimEnd();
        
        private static readonly object[] s_trimCharArrays = new object[]
        {
            null,
            new char[] {'T'},
            new char[] {'T', 'T', 'T', 'T', 'T'},
            new char[] {'a'},
            new char[] {'T', (char) 192},
            new char[] {' ', (char) 8197},
            new char[] {"\u0400"[0]},
            new char[] {'1', 'a', ' ', '0', 'd', 'e', 's', 't', "\u0400"[0]},
        };
        
        public static IEnumerable<object[]> TrimCharArrayArgs => Permutations(s_trimStrings, s_trimCharArrays);
        
        [Benchmark]
        [ArgumentsSource(nameof(TrimCharArrayArgs))]
        public string Trim_CharArr(string s, char[] c)
            => s.Trim(c);

        [Benchmark]
        [ArgumentsSource(nameof(TrimCharArrayArgs))]
        public string TrimStart_CharArr(string s, char[] c)
            => s.TrimStart(c);

        [Benchmark]
        [ArgumentsSource(nameof(TrimCharArrayArgs))]
        public string TrimEnd_CharArr(string s, char[] c)
            => s.TrimEnd(c);
    }
}