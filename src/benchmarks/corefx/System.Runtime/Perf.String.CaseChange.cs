using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        public IEnumerable<object> CaseArgs()
        {
            yield return "TeSt";
            yield return "TEST";
            yield return "test";
        }

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