using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        private static readonly string[] s_caseStrings = new string[]
        {
            "",
            " ",
            "TeSt",
            "TEST",
            "test",
            "I think Turkish i \u0131s TROUBL\u0130NG",
            "dzsdzsDDZSDZSDZSddsz",
            "a\u0300\u00C0A\u0300",
            "Foo\u0400Bar",
            "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a",
            "\u4e33\u4e65 Testing... \u4EE8",
        };
        
        public static IEnumerable<object> CaseArgs => s_caseStrings;

        [Benchmark]
        [ArgumentsSource(nameof(CaseArgs))]
        public string ToUpper(string s)
            => s.ToUpper();

        [Benchmark]
        [ArgumentsSource(nameof(CaseArgs))]
        public string ToUpperInvariant(string s)
            => s.ToUpperInvariant();
        
        [Benchmark]
        [ArgumentsSource(nameof(CaseArgs))]
        public string ToLower(string s)
            => s.ToLower();

        [Benchmark]
        [ArgumentsSource(nameof(CaseArgs))]
        public string ToLowerInvariant(string s)
            => s.ToLowerInvariant();
    }
}