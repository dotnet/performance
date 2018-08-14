using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        private CultureInfo _cultureInfo;
        
        [Benchmark]
        [Arguments("The quick brown fox", "The quick brown fox")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x jumped")]
        [Arguments("a\u0300a\u0300a\u0300", "\u00e0\u00e0\u00e0")]
        [Arguments("a\u0300a\u0300a\u0300", "\u00c0\u00c0\u00c0")]
        public int Compare_Culture_invariant(string s1, string s2)
            => CultureInfo.InvariantCulture.CompareInfo.Compare(s1, s2, CompareOptions.None);

        [GlobalSetup(Target = nameof(Compare_Culture_en_us))]
        public void SetupCompare_Culture_en_us() => _cultureInfo = new CultureInfo("en-us");

        [Benchmark]
        [Arguments("The quick brown fox", "The quick brown fox")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x jumped")]
        [Arguments("a\u0300a\u0300a\u0300", "\u00e0\u00e0\u00e0")]
        [Arguments("a\u0300a\u0300a\u0300", "\u00c0\u00c0\u00c0")]
        public int Compare_Culture_en_us(string s1, string s2)
            => _cultureInfo.CompareInfo.Compare(s1, s2, CompareOptions.None);

        [GlobalSetup(Target = nameof(Compare_Culture_ja_jp))]
        public void SetupCompare_Culture_ja_jp() => _cultureInfo = new CultureInfo("ja-jp");

        [Benchmark]
        [Arguments("The quick brown fox", "The quick brown fox")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x jumped")]
        [Arguments("a\u0300a\u0300a\u0300", "\u00e0\u00e0\u00e0")]
        [Arguments("a\u0300a\u0300a\u0300", "\u00c0\u00c0\u00c0")]
        public int Compare_Culture_ja_jp(string s1, string s2)
            => _cultureInfo.CompareInfo.Compare(s1, s2, CompareOptions.None);
        
        
        private static readonly object[] s_compareOptions = new object[]
        {
            StringComparison.CurrentCultureIgnoreCase,
            StringComparison.Ordinal,
            StringComparison.OrdinalIgnoreCase,
        };
        
        private static readonly string[][] s_comparePairs = new string[][]
        {
            new string[] {"The quick brown fox", "THE QUICK BROWN FOX"},
            new string[] {"The quick brown fox", "THE QUICK BROWN FOX J"},
            new string[] {"Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick BROWN f\u00f2x"},
            new string[] {"Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick BROWN f\u00f2x jumped"},
            new string[] {"A\u0300A\u0300A\u0300", "\u00e0\u00e0\u00e0"},
            new string[] {"A\u0300A\u0300A\u0300", "\u00e0\u00e0 Z"},
        };
        
        public static IEnumerable<object[]> CompareArgs => Permutations(s_comparePairs, s_compareOptions);
        
        [Benchmark]
        [ArgumentsSource(nameof(CompareArgs))]
        public int Compare(string[] strings, StringComparison comparison)
            => string.Compare(strings[0], strings[1], comparison);
        
        private static readonly object[] s_equalityStrings = new object[]
        {
            "a",
            "  ",
            "TeSt!",
            "I think Turkish i \u0131s TROUBL\u0130NG",
            "dzsdzsDDZSDZSDZSddsz",
            "a\u0300\u00C0A\u0300A",
            "Foo\u0400Bar!",
            "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a",
            "\u4e33\u4e65 Testing... \u4EE8",
        };
        
        public static IEnumerable<object[]> EqualityArgs => Permutations(s_equalityStrings, s_equalityStrings);

        [Benchmark]
        [ArgumentsSource(nameof(EqualityArgs))]
        public bool Equality(string s1, string s2)
            => s1 == s2;

        [Benchmark]
        [ArgumentsSource(nameof(EqualityArgs))]
        public bool Equals(string s1, string s2)
            => s1.Equals(s2);
    }
}