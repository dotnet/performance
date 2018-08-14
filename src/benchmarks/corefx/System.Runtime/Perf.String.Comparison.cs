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
        public int Compare_Culture_invariant(string s1, string s2)
            => CultureInfo.InvariantCulture.CompareInfo.Compare(s1, s2, CompareOptions.None);

        [GlobalSetup(Target = nameof(Compare_Culture_en_us))]
        public void SetupCompare_Culture_en_us() => _cultureInfo = new CultureInfo("en-us");

        [Benchmark]
        [Arguments("The quick brown fox", "The quick brown fox")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x")]
        public int Compare_Culture_en_us(string s1, string s2)
            => _cultureInfo.CompareInfo.Compare(s1, s2, CompareOptions.None);

        [GlobalSetup(Target = nameof(Compare_Culture_ja_jp))]
        public void SetupCompare_Culture_ja_jp() => _cultureInfo = new CultureInfo("ja-jp");

        [Benchmark]
        [Arguments("The quick brown fox", "The quick brown fox")]
        [Arguments("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x")]
        public int Compare_Culture_ja_jp(string s1, string s2)
            => _cultureInfo.CompareInfo.Compare(s1, s2, CompareOptions.None);

        [Benchmark]
        [Arguments(new [] {"The quick brown fox", "THE QUICK BROWN FOX"}, StringComparison.CurrentCultureIgnoreCase)]
        [Arguments(new [] {"The quick brown fox", "THE QUICK BROWN FOX"}, StringComparison.Ordinal)]
        [Arguments(new [] {"The quick brown fox", "THE QUICK BROWN FOX"}, StringComparison.OrdinalIgnoreCase)]
        [Arguments(new [] {"Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick BROWN f\u00f2x"}, StringComparison.CurrentCultureIgnoreCase)]
        [Arguments(new [] {"Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick BROWN f\u00f2x"}, StringComparison.Ordinal)]
        [Arguments(new [] {"Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick BROWN f\u00f2x"}, StringComparison.OrdinalIgnoreCase)]
        public int Compare(string[] strings, StringComparison comparison) // we should have two separate string arguments but we keep it that way to don't change the ID of the benchmark
            => string.Compare(strings[0], strings[1], comparison);
        
        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", "dzsdzsDDZSDZSDZSddsz")]
        public bool Equality(string s1, string s2)
            => s1 == s2;

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", "dzsdzsDDZSDZSDZSddsz")]
        public bool Equals(string s1, string s2)
            => s1.Equals(s2);
    }
}